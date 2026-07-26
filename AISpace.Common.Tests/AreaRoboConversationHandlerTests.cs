using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboConversationHandlerTests
{
    [Fact]
    public async Task OwnedRobo_AttachTalkAndDetach_CompletesConversationPacketSequence()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(1, 1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Conversation Robo")) { OwnerAvatarId = 1 };
                await new RoboRepository(seedDb).UpsertAsync(1, robo, TestContext.Current.CancellationToken);
            }

            await using var handlerDb = new MainContext(options);
            var repository = new RoboRepository(handlerDb);
            var attachHandler = new AreaRoboAttachHandler(repository, NullLogger<AreaRoboAttachHandler>.Instance);
            var attachReplyHandler = new AreaRoboAttachRequestRHandler(repository, NullLogger<AreaRoboAttachRequestRHandler>.Instance);
            var talkHandler = new AreaRoboTalkPostHandler(repository, NullLogger<AreaRoboTalkPostHandler>.Instance);
            var detachHandler = new AreaRoboDetachFromAvatarHandler(repository, NullLogger<AreaRoboDetachFromAvatarHandler>.Instance);
            var session = new CapturingPlayerSession { CharacterId = 1 };
            const string message = "ご一緒にお出かけでもしませんか？";

            await attachHandler.HandleAsync(BuildPayload(1), session, TestContext.Current.CancellationToken);
            await attachReplyHandler.HandleAsync(BuildPayload(1, 0), session, TestContext.Current.CancellationToken);
            await talkHandler.HandleAsync(BuildTalkPayload(1, message), session, TestContext.Current.CancellationToken);
            await detachHandler.HandleAsync(BuildPayload(1), session, TestContext.Current.CancellationToken);

            Assert.Collection(
                session.Sent,
                packet =>
                {
                    Assert.Equal(PacketType.RoboAttachRequestNotify, packet.Type);
                    AssertUInts(packet.Payload, 1, 1);
                },
                packet =>
                {
                    Assert.Equal(PacketType.RoboAttachResponse, packet.Type);
                    AssertUInts(packet.Payload, 1, 0);
                },
                packet =>
                {
                    Assert.Equal(PacketType.RoboTalkForwardNotify, packet.Type);
                    var reader = new PacketReader(packet.Payload);
                    Assert.Equal(1u, reader.ReadUInt());
                    Assert.Equal(message, reader.ReadString("utf-8"));
                },
                packet =>
                {
                    Assert.Equal(PacketType.RoboGrantNextMessageNoticeNotify, packet.Type);
                    AssertUInts(packet.Payload, 1);
                },
                packet =>
                {
                    Assert.Equal(PacketType.RoboDetachNoticeFromAvatarNotify, packet.Type);
                    AssertUInts(packet.Payload, 1, 1);
                }
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UnownedRobo_DoesNotForwardConversationOrDetach()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            var talkHandler = new AreaRoboTalkPostHandler(repository, NullLogger<AreaRoboTalkPostHandler>.Instance);
            var detachHandler = new AreaRoboDetachFromAvatarHandler(repository, NullLogger<AreaRoboDetachFromAvatarHandler>.Instance);
            var roboSideDetachHandler = new AreaRoboDetachFromRoboHandler(repository, NullLogger<AreaRoboDetachFromRoboHandler>.Instance);
            var session = new CapturingPlayerSession { CharacterId = 1 };

            await talkHandler.HandleAsync(BuildTalkPayload(99, "unowned"), session, TestContext.Current.CancellationToken);
            await detachHandler.HandleAsync(BuildPayload(99), session, TestContext.Current.CancellationToken);
            await roboSideDetachHandler.HandleAsync(BuildPayload(99), session, TestContext.Current.CancellationToken);

            Assert.Empty(session.Sent);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RoboSideAttachRejection_ClearsRelationshipAndAllowsRetry()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(42, 1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Retry Robo")) { OwnerAvatarId = 42 };
                await new RoboRepository(seedDb).UpsertAsync(42, robo, TestContext.Current.CancellationToken);
            }

            await using var handlerDb = new MainContext(options);
            var repository = new RoboRepository(handlerDb);
            var detachHandler = new AreaRoboDetachFromRoboHandler(repository, NullLogger<AreaRoboDetachFromRoboHandler>.Instance);
            var attachHandler = new AreaRoboAttachHandler(repository, NullLogger<AreaRoboAttachHandler>.Instance);
            var session = new CapturingPlayerSession { CharacterId = 42 };

            await detachHandler.HandleAsync(BuildPayload(1), session, TestContext.Current.CancellationToken);

            var detachNotice = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboDetachNoticeFromRoboNotify, detachNotice.Type);
            AssertUInts(detachNotice.Payload, 1, 42);

            session.Sent.Clear();
            await attachHandler.HandleAsync(BuildPayload(1), session, TestContext.Current.CancellationToken);

            var attachRequest = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboAttachRequestNotify, attachRequest.Type);
            AssertUInts(attachRequest.Payload, 1, 42);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static byte[] BuildPayload(params uint[] values)
    {
        var writer = new PacketWriter();
        foreach (var value in values)
            writer.Write(value);
        return writer.ToBytes();
    }

    private static byte[] BuildTalkPayload(uint roboId, string message)
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(message, "utf-8");
        return writer.ToBytes();
    }

    private static void AssertUInts(byte[] payload, params uint[] values)
    {
        var reader = new PacketReader(payload);
        foreach (var value in values)
            Assert.Equal(value, reader.ReadUInt());
    }
}
