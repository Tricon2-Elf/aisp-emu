using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class AreaUserStatusUpdateHandlerTests
{
    private static byte[] Request(uint objectId, string text, uint icon)
    {
        var writer = new PacketWriter();
        writer.Write(objectId);
        writer.Write(new UserStatusData { StatusText = text, StatusIconId = icon }.ToBytes());
        return writer.ToBytes();
    }

    [Fact]
    public async Task OwnAvatar_IsStored_Acknowledged_AndBroadcast()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1);
            await using var db = new MainContext(options);
            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                UserId = 1,
                CharacterId = 1,
                MapId = 10990100,
                ChannelId = 1,
            };
            var peer = new CapturingPlayerSession
            {
                UserId = 2,
                CharacterId = 2,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, peer);
            var handler = new AreaUserStatusUpdateHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                state,
                NullLogger<AreaUserStatusUpdateHandler>.Instance
            );

            await handler.HandleAsync(
                Request(1, "Testing 12345", 4),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(
                [PacketType.UserStatusUpdateResponse, PacketType.NotifyUserStatusUpdate],
                session.Sent.Select(p => p.Type)
            );
            var ack = new PacketReader(session.Sent[0].Payload);
            Assert.Equal(0u, ack.ReadUInt());
            Assert.Equal(1u, ack.ReadUInt());
            var notify = Assert.Single(peer.Sent);
            Assert.Equal(PacketType.NotifyUserStatusUpdate, notify.Type);
            Assert.Equal(4 + UserStatusData.WireSize, notify.Payload.Length);
            var reader = new PacketReader(notify.Payload);
            Assert.Equal(1u, reader.ReadUInt());
            var status = UserStatusData.FromBytes(reader.ReadBytes(UserStatusData.WireSize));
            Assert.Equal("Testing 12345", status.StatusText);
            Assert.Equal(4u, status.StatusIconId);

            db.ChangeTracker.Clear();
            var stored = await db.Characters.FindAsync([1], TestContext.Current.CancellationToken);
            Assert.Equal("Testing 12345", stored!.UserStatusText);
            Assert.Equal(4u, stored.UserStatusIconId);

            // Someone else's avatar is refused without a broadcast.
            session.Sent.Clear();
            peer.Sent.Clear();
            await handler.HandleAsync(
                Request(2, "nope", 1),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());
            Assert.Empty(peer.Sent);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
