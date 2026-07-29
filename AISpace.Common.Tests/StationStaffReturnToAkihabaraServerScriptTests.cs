using AISpace.Common.Config;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Tests;

public sealed class StationStaffReturnToAkihabaraServerScriptTests
{
    [Fact]
    public async Task Start_ShowsAkihabaraTravelMessage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var dispatcher = CreateDispatcher(db, new SharedState());
            var session = new CapturingPlayerSession
            {
                CharacterId = 1,
                MapId = 10030200,
                ChannelId = 1,
            };

            await dispatcher.StartAsync(session, ServerEvents.Keys.StationStaffReturnToAkihabara, CreateContext(), EventCompletionPolicy.Replayable, TestContext.Current.CancellationToken);

            Assert.Equal(ServerEvents.Keys.StationStaffReturnToAkihabara, session.ActiveEventKey);
            var message = Assert.Single(session.Sent, packet => packet.Type == PacketType.EventMessageNotify);
            var reader = new PacketReader(message.Payload);
            Assert.Equal(1342177294u, reader.ReadUInt());
            Assert.Equal("駅員 (Station Staff)", reader.ReadString("utf-8"));
            Assert.Equal("I'll take you to Akihabara", reader.ReadString("utf-8"));
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventMessageCloseNotify);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventSyncNotify);
            Assert.DoesNotContain(session.Sent, packet => packet.Type == PacketType.NotifyChangeMap);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Sync_TeleportsToAkihabara()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User { Username = "return-staff-user" };
            var character = new Character
            {
                Name = "Return Traveller",
                User = user,
                ModelId = 1,
                Birthdate = DateTime.UnixEpoch,
                CurrentMapId = 10030200,
                HomeIslandId = 3,
            };
            db.Users.Add(user);
            db.Characters.Add(character);
            db.Maps.Add(
                new Map
                {
                    MapId = StationStaffReturnToAkihabaraServerScript.AkihabaraMapId,
                    Name = "Akihabara",
                    SpawnX = 1,
                    SpawnY = 0,
                    SpawnZ = 2,
                    SpawnRotation = 0,
                }
            );
            db.Channels.Add(
                new GameChannel
                {
                    ChannelNum = 1,
                    IP = "localhost",
                    Port = 50054,
                    MapId = 10030200,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var state = new SharedState();
            var dispatcher = CreateDispatcher(db, state);
            var session = new CapturingPlayerSession
            {
                CharacterId = (uint)character.Id,
                Character = character,
                MapId = 10030200,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);

            await dispatcher.StartAsync(session, ServerEvents.Keys.StationStaffReturnToAkihabara, CreateContext(), EventCompletionPolicy.Replayable, TestContext.Current.CancellationToken);

            var syncHandler = new AreaEventSyncRHandler(dispatcher, NullLogger<AreaEventSyncRHandler>.Instance);
            var writer = new PacketWriter();
            writer.Write(0u);
            await syncHandler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(StationStaffReturnToAkihabaraServerScript.AkihabaraMapId, session.MapId);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.EventEndNotify);
            Assert.Contains(session.Sent, packet => packet.Type == PacketType.NotifyChangeMap);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static ServerScriptDispatcher CreateDispatcher(MainContext db, SharedState state)
    {
        var serverScriptSession = new ServerScriptSession(new CharacterEventRepository(db), NullLogger<ServerScriptSession>.Instance);
        var script = new StationStaffReturnToAkihabaraServerScript(serverScriptSession, CreateDirectMapLinkTransitionService(db, state), NullLogger<StationStaffReturnToAkihabaraServerScript>.Instance);
        return new ServerScriptDispatcher([script], serverScriptSession, NullLogger<ServerScriptDispatcher>.Instance);
    }

    private static DirectMapLinkTransitionService CreateDirectMapLinkTransitionService(MainContext db, SharedState state) =>
        new(
            new MapRepository(db),
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            new MyRoomRepository(db),
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
            Npc = new Npc { NpcObjectId = 1342177294, Name = "駅員 (Station Staff)" },
        };
}
