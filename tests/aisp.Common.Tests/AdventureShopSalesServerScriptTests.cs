using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class AdventureShopSalesServerScriptTests
{
    [Fact]
    public async Task Start_ShowsNoPayoutMessageInsideEventBracket()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var dispatcher = CreateDispatcher(db);
            var session = new CapturingPlayerSession { CharacterId = 1, MapId = 10030200 };

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.AdventureShopSales,
                CreateContext(),
                EventCompletionPolicy.Replayable,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(ServerEvents.Keys.AdventureShopSales, session.ActiveEventKey);
            // The client only renders event messages between event_start and event_end.
            Assert.Equal(PacketType.EventStartNotify, session.Sent[0].Type);
            var message = Assert.Single(session.Sent, p => p.Type == PacketType.EventMessageNotify);
            var reader = new PacketReader(message.Payload);
            Assert.Equal(1342177330u, reader.ReadUInt());
            Assert.Equal("Happy・Story Payout", reader.ReadString());
            Assert.Contains("sales", reader.ReadString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventMessageCloseNotify);
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventSyncNotify);
            Assert.DoesNotContain(
                session.Sent,
                p => p.Type == PacketType.AdventureShopStartedNotify
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Sync_EndsTheEvent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var dispatcher = CreateDispatcher(db);
            var session = new CapturingPlayerSession { CharacterId = 1, MapId = 10030200 };
            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.AdventureShopSales,
                CreateContext(),
                EventCompletionPolicy.Replayable,
                TestContext.Current.CancellationToken
            );

            var syncHandler = new AreaEventSyncRHandler(
                dispatcher,
                NullLogger<AreaEventSyncRHandler>.Instance
            );
            var writer = new PacketWriter();
            writer.Write(0u);
            await syncHandler.HandleAsync(
                writer.ToBytes(),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Null(session.ActiveEventKey);
            Assert.Equal(PacketType.EventEndNotify, session.Sent[^1].Type);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static ServerScriptDispatcher CreateDispatcher(MainContext db)
    {
        var serverScriptSession = new ServerScriptSession(
            new CharacterEventRepository(db),
            NullLogger<ServerScriptSession>.Instance
        );
        var script = new AdventureShopSalesServerScript(
            serverScriptSession,
            TestTextLocaliser.English
        );
        return new ServerScriptDispatcher(
            [script],
            serverScriptSession,
            NullLogger<ServerScriptDispatcher>.Instance
        );
    }

    private static ServerScriptContext CreateContext() =>
        new()
        {
            Npc = new Npc { NpcObjectId = 1342177330, Name = "はっぴぃ・すとぉりぃ売上" },
        };
}
