using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

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
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Conversation Robo"))
                {
                    OwnerAvatarId = 1,
                };
                await new RoboRepository(seedDb).UpsertAsync(
                    1,
                    robo,
                    TestContext.Current.CancellationToken
                );
            }

            await using var handlerDb = new MainContext(options);
            var repository = new RoboRepository(handlerDb);
            var attachHandler = new AreaRoboAttachHandler(
                repository,
                NullLogger<AreaRoboAttachHandler>.Instance
            );
            var attachReplyHandler = new AreaRoboAttachRequestRHandler(
                repository,
                NullLogger<AreaRoboAttachRequestRHandler>.Instance
            );
            var state = new SharedState();
            var talkHandler = new AreaRoboTalkPostHandler(
                state,
                repository,
                WordFilter.FromTerms([]),
                NullLogger<AreaRoboTalkPostHandler>.Instance
            );
            var detachHandler = new AreaRoboDetachFromAvatarHandler(
                repository,
                NullLogger<AreaRoboDetachFromAvatarHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                CharacterId = 1,
                MapId = 10990100,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);
            const string message = "ご一緒にお出かけでもしませんか？";

            await attachHandler.HandleAsync(
                BuildPayload(1),
                session,
                TestContext.Current.CancellationToken
            );
            await attachReplyHandler.HandleAsync(
                BuildPayload(1, 0),
                session,
                TestContext.Current.CancellationToken
            );
            await talkHandler.HandleAsync(
                BuildTalkPayload(1, message),
                session,
                TestContext.Current.CancellationToken
            );
            await detachHandler.HandleAsync(
                BuildPayload(1),
                session,
                TestContext.Current.CancellationToken
            );

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
            var talkHandler = new AreaRoboTalkPostHandler(
                new SharedState(),
                repository,
                WordFilter.FromTerms([]),
                NullLogger<AreaRoboTalkPostHandler>.Instance
            );
            var detachHandler = new AreaRoboDetachFromAvatarHandler(
                repository,
                NullLogger<AreaRoboDetachFromAvatarHandler>.Instance
            );
            var roboSideDetachHandler = new AreaRoboDetachFromRoboHandler(
                repository,
                NullLogger<AreaRoboDetachFromRoboHandler>.Instance
            );
            var session = new CapturingPlayerSession { CharacterId = 1 };

            await talkHandler.HandleAsync(
                BuildTalkPayload(99, "unowned"),
                session,
                TestContext.Current.CancellationToken
            );
            await detachHandler.HandleAsync(
                BuildPayload(99),
                session,
                TestContext.Current.CancellationToken
            );
            await roboSideDetachHandler.HandleAsync(
                BuildPayload(99),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Empty(session.Sent);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BlockedTalk_DoesNotForwardMessage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(1, 1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Conversation Robo"))
                {
                    OwnerAvatarId = 1,
                };
                await new RoboRepository(seedDb).UpsertAsync(
                    1,
                    robo,
                    TestContext.Current.CancellationToken
                );
            }

            await using var handlerDb = new MainContext(options);
            var talkHandler = new AreaRoboTalkPostHandler(
                new SharedState(),
                new RoboRepository(handlerDb),
                WordFilter.FromTerms(["faggot"]),
                NullLogger<AreaRoboTalkPostHandler>.Instance
            );
            var session = new CapturingPlayerSession { CharacterId = 1 };

            await talkHandler.HandleAsync(
                BuildTalkPayload(1, "Faggot"),
                session,
                TestContext.Current.CancellationToken
            );

            var grant = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboGrantNextMessageNoticeNotify, grant.Type);
            AssertUInts(grant.Payload, 1);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task OwnedRobo_Talk_ForwardsToAreaPeersAndGrantsOwnerOnly()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(1, 1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Broadcast Robo"))
                {
                    OwnerAvatarId = 1,
                };
                await new RoboRepository(seedDb).UpsertAsync(
                    1,
                    robo,
                    TestContext.Current.CancellationToken
                );
            }

            await using var handlerDb = new MainContext(options);
            var state = new SharedState();
            var owner = new CapturingPlayerSession
            {
                CharacterId = 1,
                MapId = 10990100,
                ChannelId = 1,
            };
            var sameArea = new CapturingPlayerSession
            {
                CharacterId = 2,
                MapId = 10990100,
                ChannelId = 1,
            };
            var otherMap = new CapturingPlayerSession
            {
                CharacterId = 3,
                MapId = 10990110,
                ChannelId = 1,
            };
            var otherChannel = new CapturingPlayerSession
            {
                CharacterId = 4,
                MapId = 10990100,
                ChannelId = 2,
            };
            state.RegisterClient(ServerType.Area, owner);
            state.RegisterClient(ServerType.Area, sameArea);
            state.RegisterClient(ServerType.Area, otherMap);
            state.RegisterClient(ServerType.Area, otherChannel);

            var talkHandler = new AreaRoboTalkPostHandler(
                state,
                new RoboRepository(handlerDb),
                WordFilter.FromTerms([]),
                NullLogger<AreaRoboTalkPostHandler>.Instance
            );
            const string message = "こんにちは";
            await talkHandler.HandleAsync(
                BuildTalkPayload(1, message),
                owner,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                owner.Sent,
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
                }
            );
            var peerForward = Assert.Single(sameArea.Sent);
            Assert.Equal(PacketType.RoboTalkForwardNotify, peerForward.Type);
            var peerReader = new PacketReader(peerForward.Payload);
            Assert.Equal(1u, peerReader.ReadUInt());
            Assert.Equal(message, peerReader.ReadString("utf-8"));
            Assert.Empty(otherMap.Sent);
            Assert.Empty(otherChannel.Sent);
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
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Retry Robo"))
                {
                    OwnerAvatarId = 42,
                };
                await new RoboRepository(seedDb).UpsertAsync(
                    42,
                    robo,
                    TestContext.Current.CancellationToken
                );
            }

            await using var handlerDb = new MainContext(options);
            var repository = new RoboRepository(handlerDb);
            var detachHandler = new AreaRoboDetachFromRoboHandler(
                repository,
                NullLogger<AreaRoboDetachFromRoboHandler>.Instance
            );
            var attachHandler = new AreaRoboAttachHandler(
                repository,
                NullLogger<AreaRoboAttachHandler>.Instance
            );
            var session = new CapturingPlayerSession { CharacterId = 42 };

            await detachHandler.HandleAsync(
                BuildPayload(1),
                session,
                TestContext.Current.CancellationToken
            );

            var detachNotice = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboDetachNoticeFromRoboNotify, detachNotice.Type);
            AssertUInts(detachNotice.Payload, 1, 42);

            session.Sent.Clear();
            await attachHandler.HandleAsync(
                BuildPayload(1),
                session,
                TestContext.Current.CancellationToken
            );

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
