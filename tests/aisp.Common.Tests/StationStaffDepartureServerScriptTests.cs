using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace aisp.Common.Tests;

public sealed class StationStaffDepartureServerScriptTests
{
    [Fact]
    public async Task ClientScriptSegmentRunner_PreservesParentEventUntilCompletion()
    {
        var runner = new ClientScriptSegmentRunner();
        var session = new CapturingPlayerSession
        {
            ActiveEventKey = ServerEvents.Keys.StationStaffDeparture,
            ActiveEventKind = NpcEventKind.ServerScript,
            ServerScriptState = new ServerScriptState
            {
                EventKey = ServerEvents.Keys.StationStaffDeparture,
                Step = string.Empty,
            },
        };

        await runner.BeginAsync(
            session,
            ScriptedEvents.Keys.IntroductionMyRoomShuffle,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
        Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
        var scriptPlay = Assert.Single(session.Sent);
        Assert.Equal(PacketType.EventScriptPlayNotify, scriptPlay.Type);
        Assert.Equal(
            "./script/event/introdution_myroom_sh.csv",
            new PacketReader(scriptPlay.Payload).ReadString("utf-8")
        );
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type is PacketType.EventStartNotify or PacketType.EventEndNotify
        );

        var scriptResult = await runner.TryHandleAsync(
            PacketType.EventScriptPlayRequest,
            BuildUIntPayload(0),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(ClientScriptSegmentStatus.InProgress, scriptResult.Status);
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventFadeInNotify);
        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);

        var fadeResult = await runner.TryHandleAsync(
            PacketType.EventFadeInRequest,
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(ClientScriptSegmentStatus.Completed, fadeResult.Status);
        Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
        Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
    }

    [Fact]
    public async Task ClientScriptSegmentRunner_ReturnsFailureWithoutEndingParentEvent()
    {
        var runner = new ClientScriptSegmentRunner();
        var session = new CapturingPlayerSession
        {
            ActiveEventKey = ServerEvents.Keys.StationStaffDeparture,
            ActiveEventKind = NpcEventKind.ServerScript,
            ServerScriptState = new ServerScriptState
            {
                EventKey = ServerEvents.Keys.StationStaffDeparture,
                Step = string.Empty,
            },
        };
        await runner.BeginAsync(
            session,
            ScriptedEvents.Keys.IntroductionMyRoomDaCapo,
            TestContext.Current.CancellationToken
        );

        var result = await runner.TryHandleAsync(
            PacketType.EventScriptPlayRequest,
            BuildUIntPayload(7),
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(ClientScriptSegmentStatus.Failed, result.Status);
        Assert.Equal(7u, result.Result);
        Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type is PacketType.EventFadeInNotify or PacketType.EventEndNotify
        );
    }

    [Fact]
    public async Task StationStaffDeparture_ShowsRegistrationMessageWithoutTeleport_WhenHomeIslandIsUnset()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var character = await SeedCharacterAsync(db, 0);
            var dispatcher = CreateDispatcher(db, new SharedState());
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                MapId = 10990100,
                ChannelId = 1,
            };

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.StationStaffDeparture,
                CreateContext(),
                EventCompletionPolicy.Replayable,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
            var message = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.EventMessageNotify
            );
            var messageReader = new PacketReader(message.Payload);
            Assert.Equal(1342177293u, messageReader.ReadUInt());
            Assert.Equal("駅員 (Station Staff)", messageReader.ReadString("utf-8"));
            Assert.Equal(
                "Please register at the Sotokanda Building first.",
                messageReader.ReadString("utf-8")
            );
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.EventMessageCloseNotify
            );
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventSyncNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.EventSelectInitNotify
            );
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );

            var syncHandler = new AreaEventSyncRHandler(
                dispatcher,
                NullLogger<AreaEventSyncRHandler>.Instance
            );
            await syncHandler.HandleAsync(
                BuildUIntPayload(0),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveEventKey);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task StationStaffDeparture_ShowsIslandChoiceDialog()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var character = await SeedCharacterAsync(db, 1);
            var dispatcher = CreateDispatcher(db, new SharedState());
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                MapId = 10990100,
                ChannelId = 1,
            };

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.StationStaffDeparture,
                CreateContext(),
                EventCompletionPolicy.Replayable,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
            var selectInit = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.EventSelectInitNotify
            );
            Assert.Equal(
                (uint)EventSelectType.Dialogue,
                new PacketReader(selectInit.Payload).ReadUInt()
            );
            var islandLabels = session
                .Sent.Where(packet => packet.Type == PacketType.EventSelectPushNotify)
                .Select(packet => new PacketReader(packet.Payload).ReadString("utf-8"))
                .ToArray();
            Assert.Equal(["Da Capo", "SHUFFLE!", "CLANNAD"], islandLabels);
            var selectExec = Assert.Single(
                session.Sent,
                packet => packet.Type == PacketType.EventSelectExecNotify
            );
            Assert.Equal(
                "Which island would you like to visit?",
                new PacketReader(selectExec.Payload).ReadString("utf-8")
            );
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(0, StationStaffDepartureServerScript.DaCapoShoppingStreetMapId)]
    [InlineData(1, StationStaffDepartureServerScript.ShuffleShoppingStreetMapId)]
    [InlineData(2, StationStaffDepartureServerScript.ClannadShoppingStreetMapId)]
    public async Task StationStaffDeparture_TeleportsToSelectedShoppingDistrict(
        byte selection,
        uint expectedMapId
    )
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var character = await SeedCharacterAsync(db, 1);
            SeedShoppingDistrictMaps(db);
            db.Channels.Add(
                new GameChannel
                {
                    ChannelNum = 1,
                    IP = "localhost",
                    Port = 50054,
                    MapId = 10990100,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var state = new SharedState();
            var dispatcher = CreateDispatcher(db, state);
            var eventRepository = new CharacterEventRepository(db);
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                Character = character,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.StationStaffDeparture,
                CreateContext(),
                EventCompletionPolicy.Replayable,
                TestContext.Current.CancellationToken
            );

            var selectHandler = new AreaEventSelectExecRHandler(
                dispatcher,
                NullLogger<AreaEventSelectExecRHandler>.Instance
            );
            await selectHandler.HandleAsync(
                BuildEventSelectionPayload(selection),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(expectedMapId, session.MapId);
            Assert.Equal(1, session.Sent.Count(packet => packet.Type == PacketType.EventEndNotify));
            Assert.Equal(
                1,
                session.Sent.Count(packet => packet.Type == PacketType.NotifyChangeMap)
            );
            Assert.True(
                session.Sent.FindIndex(packet => packet.Type == PacketType.EventEndNotify)
                    < session.Sent.FindIndex(packet => packet.Type == PacketType.NotifyChangeMap)
            );
            Assert.False(
                await eventRepository.HasCompletedAsync(
                    character.Id,
                    ServerEvents.Keys.StationStaffDeparture,
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task StationStaffDeparture_CancelSelectionAbortsWithoutTeleport()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var character = await SeedCharacterAsync(db, 1);
            var dispatcher = CreateDispatcher(db, new SharedState());
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                MapId = 10990100,
                ChannelId = 1,
            };
            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.StationStaffDeparture,
                CreateContext(),
                EventCompletionPolicy.Replayable,
                TestContext.Current.CancellationToken
            );

            var handler = new AreaEventSelectExecRHandler(
                dispatcher,
                NullLogger<AreaEventSelectExecRHandler>.Instance
            );
            var writer = new PacketWriter();
            writer.Write(1u);
            writer.Write((byte)0);
            await handler.HandleAsync(
                writer.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(10990100u, session.MapId);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(
                session.Sent,
                packet => packet.Type == PacketType.NotifyChangeMap
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task<Character> SeedCharacterAsync(MainContext db, uint homeIslandId)
    {
        var user = new User { Username = $"station-user-{homeIslandId}" };
        var character = new Character
        {
            Name = "Station Traveller",
            User = user,
            ModelId = 1,
            Birthdate = DateTime.UnixEpoch,
            CurrentMapId = 10990100,
            HomeIslandId = homeIslandId,
        };
        db.Users.Add(user);
        db.Characters.Add(character);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return character;
    }

    private static void SeedShoppingDistrictMaps(MainContext db)
    {
        db.Maps.AddRange(
            new Map
            {
                MapId = StationStaffDepartureServerScript.DaCapoShoppingStreetMapId,
                Name = "Da Capo Shopping Street",
                SpawnX = 10,
                SpawnY = 0.1f,
                SpawnZ = 20,
                SpawnRotation = 0,
            },
            new Map
            {
                MapId = StationStaffDepartureServerScript.ShuffleShoppingStreetMapId,
                Name = "Shuffle Shopping Street",
                SpawnX = 10,
                SpawnY = 0.1f,
                SpawnZ = 20,
                SpawnRotation = 0,
            },
            new Map
            {
                MapId = StationStaffDepartureServerScript.ClannadShoppingStreetMapId,
                Name = "Clannad Shopping Street",
                SpawnX = 10,
                SpawnY = 0.1f,
                SpawnZ = 20,
                SpawnRotation = 0,
            }
        );
    }

    private static ServerScriptDispatcher CreateDispatcher(MainContext db, SharedState state)
    {
        var eventRepository = new CharacterEventRepository(db);
        var serverScriptSession = new ServerScriptSession(
            eventRepository,
            NullLogger<ServerScriptSession>.Instance
        );
        var script = new StationStaffDepartureServerScript(
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            serverScriptSession,
            CreateDirectMapLinkTransitionService(db, state),
            TestTextLocaliser.English,
            NullLogger<StationStaffDepartureServerScript>.Instance
        );
        return new ServerScriptDispatcher(
            [script],
            serverScriptSession,
            NullLogger<ServerScriptDispatcher>.Instance
        );
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(
        MainContext db,
        SharedState state
    ) =>
        new(
            new MapRepository(db),
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            new MyRoomRepository(db),
            new CircleRepository(db),
            new MapLinkRepository(db),
            new ChannelRepository(db),
            Options.Create(
                new ServerOptions
                {
                    NetworkOptions = new NetworkOptions(),
                    DbOptions = new DbOptions(),
                    IPOverride = "localhost",
                }
            ),
            state,
            TestTextLocaliser.English,
            NullLogger<DirectMapLinkTransitionService>.Instance
        );

    private static ServerScriptContext CreateContext() =>
        new()
        {
            Npc = new Npc { NpcObjectId = 1342177293, Name = "駅員 (Station Staff)" },
        };

    private static byte[] BuildUIntPayload(uint value)
    {
        var writer = new PacketWriter();
        writer.Write(value);
        return writer.ToBytes();
    }

    private static byte[] BuildEventSelectionPayload(byte selection)
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(selection);
        return writer.ToBytes();
    }
}
