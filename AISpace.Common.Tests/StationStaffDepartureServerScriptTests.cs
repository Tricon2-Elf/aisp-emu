using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

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
            ServerScriptState = new ServerScriptState { EventKey = ServerEvents.Keys.StationStaffDeparture, Step = string.Empty },
        };

        await runner.BeginAsync(session, ScriptedEvents.Keys.IntroductionMyRoomShuffle, TestContext.Current.CancellationToken);

        Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
        Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
        var scriptPlay = Assert.Single(session.Sent);
        Assert.Equal(PacketType.EventScriptPlayNotify, scriptPlay.Type);
        Assert.Equal("./script/event/introdution_myroom_sh.csv", new PacketReader(scriptPlay.Payload).ReadString("utf-8"));
        Assert.DoesNotContain(session.Sent, packet => packet.Type is PacketType.EventStartNotify or PacketType.EventEndNotify);

        var scriptResult = await runner.TryHandleAsync(PacketType.EventScriptPlayRequest, BuildUIntPayload(0), session, TestContext.Current.CancellationToken);

        Assert.Equal(ClientScriptSegmentStatus.InProgress, scriptResult.Status);
        Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventFadeInNotify);
        Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);

        var fadeResult = await runner.TryHandleAsync(PacketType.EventFadeInRequest, ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

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
            ServerScriptState = new ServerScriptState { EventKey = ServerEvents.Keys.StationStaffDeparture, Step = string.Empty },
        };
        await runner.BeginAsync(session, ScriptedEvents.Keys.IntroductionMyRoomDaCapo, TestContext.Current.CancellationToken);

        var result = await runner.TryHandleAsync(PacketType.EventScriptPlayRequest, BuildUIntPayload(7), session, TestContext.Current.CancellationToken);

        Assert.Equal(ClientScriptSegmentStatus.Failed, result.Status);
        Assert.Equal(7u, result.Result);
        Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
        Assert.DoesNotContain(session.Sent, packet => packet.Type is PacketType.EventFadeInNotify or PacketType.EventEndNotify);
    }

    [Theory]
    [InlineData(1u, "./script/event/introdution_myroom_dc.csv")]
    [InlineData(2u, "./script/event/introdution_myroom_cl.csv")]
    [InlineData(3u, "./script/event/introdution_myroom_sh.csv")]
    public async Task StationStaffDeparture_SelectsClientScriptForHomeIsland(uint homeIslandId, string expectedLabel)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var character = await SeedCharacterAsync(db, homeIslandId);
            var dispatcher = CreateDispatcher(db, new SharedState());
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                MapId = 10990100,
                ChannelId = 1,
            };

            await dispatcher.StartAsync(session, ServerEvents.Keys.StationStaffDeparture, CreateContext(), EventCompletionPolicy.Once, TestContext.Current.CancellationToken);

            Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
            Assert.Equal(NpcEventKind.ServerScript, session.ActiveEventKind);
            Assert.Equal(1, session.Sent.Count(packet => packet.Type == PacketType.EventStartNotify));
            var scriptPlay = Assert.Single(session.Sent, packet => packet.Type == PacketType.EventScriptPlayNotify);
            Assert.Equal(expectedLabel, new PacketReader(scriptPlay.Payload).ReadString("utf-8"));
        }
        finally
        {
            await connection.DisposeAsync();
        }
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

            await dispatcher.StartAsync(session, ServerEvents.Keys.StationStaffDeparture, CreateContext(), EventCompletionPolicy.Once, TestContext.Current.CancellationToken);

            Assert.Equal(ServerEvents.Keys.StationStaffDeparture, session.ActiveEventKey);
            var message = Assert.Single(session.Sent, packet => packet.Type == PacketType.EventMessageNotify);
            var messageReader = new PacketReader(message.Payload);
            Assert.Equal(1342177293u, messageReader.ReadUInt());
            Assert.Equal("駅員 (Station Staff)", messageReader.ReadString("utf-8"));
            Assert.Equal("Please register at the Sotokanda Building first.", messageReader.ReadString("utf-8"));
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventMessageCloseNotify);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventSyncNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.EventScriptPlayNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.NotifyChangeMap);

            var syncHandler = new AreaEventSyncRHandler(dispatcher, NullLogger<AreaEventSyncRHandler>.Instance);
            await syncHandler.HandleAsync(BuildUIntPayload(0), session, TestContext.Current.CancellationToken);

            Assert.Null(session.ActiveEventKey);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.NotifyChangeMap);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task StationStaffDeparture_TeleportsAfterFadeAcknowledgement()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var character = await SeedCharacterAsync(db, 3);
            db.Maps.Add(
                new Map
                {
                    MapId = 10030200,
                    Name = "Shuffle Shopping Street",
                    SpawnX = 10,
                    SpawnY = 0.1f,
                    SpawnZ = 20,
                    SpawnRotation = 0,
                }
            );
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

            await dispatcher.StartAsync(session, ServerEvents.Keys.StationStaffDeparture, CreateContext(), EventCompletionPolicy.Once, TestContext.Current.CancellationToken);
            var scriptPlayHandler = new AreaEventScriptPlayHandler(NullLogger<AreaEventScriptPlayHandler>.Instance, dispatcher);
            await scriptPlayHandler.HandleAsync(BuildUIntPayload(0), session, TestContext.Current.CancellationToken);

            Assert.Equal(10990100u, session.MapId);
            Assert.DoesNotContain(session.Sent, packet => packet.Type is PacketType.EventEndNotify or PacketType.NotifyChangeMap);

            var fadeInHandler = new AreaEventFadeInHandler(eventRepository, NullLogger<AreaEventFadeInHandler>.Instance, dispatcher);
            await fadeInHandler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(10030200u, session.MapId);
            Assert.Equal(1, session.Sent.Count(packet => packet.Type == PacketType.EventEndNotify));
            Assert.Equal(1, session.Sent.Count(packet => packet.Type == PacketType.NotifyChangeMap));
            Assert.True(session.Sent.FindIndex(packet => packet.Type == PacketType.EventEndNotify) < session.Sent.FindIndex(packet => packet.Type == PacketType.NotifyChangeMap));
            Assert.False(await eventRepository.HasCompletedAsync(character.Id, ServerEvents.Keys.StationStaffDeparture, TestContext.Current.CancellationToken));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task StationStaffDeparture_ClientScriptFailureAbortsWithoutTeleport()
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
            await dispatcher.StartAsync(session, ServerEvents.Keys.StationStaffDeparture, CreateContext(), EventCompletionPolicy.Once, TestContext.Current.CancellationToken);

            var handler = new AreaEventScriptPlayHandler(NullLogger<AreaEventScriptPlayHandler>.Instance, dispatcher);
            await handler.HandleAsync(BuildUIntPayload(9), session, TestContext.Current.CancellationToken);

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(10990100u, session.MapId);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type is PacketType.EventFadeInNotify or PacketType.NotifyChangeMap);
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

    private static ServerScriptDispatcher CreateDispatcher(MainContext db, SharedState state)
    {
        var eventRepository = new CharacterEventRepository(db);
        var serverScriptSession = new ServerScriptSession(eventRepository, NullLogger<ServerScriptSession>.Instance);
        var script = new StationStaffDepartureServerScript(new CharacterRepository(db, NullLogger<CharacterRepository>.Instance), new ClientScriptSegmentRunner(), serverScriptSession, CreateDirectMapLinkTransitionService(db, state), NullLogger<StationStaffDepartureServerScript>.Instance);
        return new ServerScriptDispatcher([script], serverScriptSession, NullLogger<ServerScriptDispatcher>.Instance);
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(MainContext db, SharedState state) =>
        new(
            new MapRepository(db),
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
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
}
