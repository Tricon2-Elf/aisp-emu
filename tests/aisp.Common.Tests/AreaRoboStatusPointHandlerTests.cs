using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaRoboStatusPointHandlerTests
{
    [Fact]
    public async Task PreviewAndFinish_AcknowledgeAndPersistOwnedRoboStatusPoints()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 42;
            const uint roboId = 1;
            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );

            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            var robo = new RoboData(
                roboId,
                new CharaData(
                    RoboRepository.GetObjectId(characterId, roboId),
                    1002011,
                    "Status Robo"
                )
            )
            {
                OwnerAvatarId = characterId,
                AvailableStatusPoints = 10,
                DistributedStatusPoints = [1, 2, 0, 0, 0],
            };
            await repository.UpsertAsync(characterId, robo, TestContext.Current.CancellationToken);

            var session = new CapturingPlayerSession { CharacterId = characterId };
            var addHandler = new AreaDistributeStatusPointAddHandler(
                repository,
                NullLogger<AreaDistributeStatusPointAddHandler>.Instance
            );
            await addHandler.HandleAsync(
                BuildUIntPayload(roboId, 2, 4),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet =>
                {
                    Assert.Equal(PacketType.DistributeStatusPointAddResponse, packet.Type);
                    var reader = new PacketReader(packet.Payload);
                    Assert.Equal(0u, reader.ReadUInt());
                    Assert.Equal(roboId, reader.ReadUInt());
                    Assert.Equal(2u, reader.ReadUInt());
                    Assert.Equal(4u, reader.ReadUInt());
                }
            );

            session.Sent.Clear();
            var finishHandler = new AreaDistributeStatusPointFinishHandler(
                repository,
                NullLogger<AreaDistributeStatusPointFinishHandler>.Instance
            );
            await finishHandler.HandleAsync(
                BuildUIntPayload(roboId, 2, 3, 1, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                session.Sent,
                packet =>
                {
                    Assert.Equal(PacketType.DistributeStatusPointFinishResponse, packet.Type);
                    var reader = new PacketReader(packet.Payload);
                    Assert.Equal(0u, reader.ReadUInt());
                    Assert.Equal(roboId, reader.ReadUInt());
                }
            );

            var stored = await repository.GetAsync(
                characterId,
                roboId,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(stored);
            Assert.Equal([2u, 3u, 1u, 0u, 0u], stored.DistributedStatusPoints);
            Assert.Equal(7u, stored.AvailableStatusPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PreviewAndFinish_RejectInvalidTypeUnownedRoboAndOverBudgetValues()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 42;
            const uint roboId = 1;
            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );

            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            await repository.UpsertAsync(
                characterId,
                new RoboData(
                    roboId,
                    new CharaData(
                        RoboRepository.GetObjectId(characterId, roboId),
                        1002011,
                        "Status Robo"
                    )
                )
                {
                    OwnerAvatarId = characterId,
                    AvailableStatusPoints = 2,
                    DistributedStatusPoints = [0, 0, 0, 0, 0],
                },
                TestContext.Current.CancellationToken
            );

            var session = new CapturingPlayerSession { CharacterId = characterId };
            var addHandler = new AreaDistributeStatusPointAddHandler(
                repository,
                NullLogger<AreaDistributeStatusPointAddHandler>.Instance
            );

            await addHandler.HandleAsync(
                BuildUIntPayload(roboId, 5, 1),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());

            session.Sent.Clear();
            await addHandler.HandleAsync(
                BuildUIntPayload(2, 0, 1),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());

            session.Sent.Clear();
            var finishHandler = new AreaDistributeStatusPointFinishHandler(
                repository,
                NullLogger<AreaDistributeStatusPointFinishHandler>.Instance
            );
            await finishHandler.HandleAsync(
                BuildUIntPayload(roboId, 3, 0, 0, 0, 0),
                session,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(1u, new PacketReader(Assert.Single(session.Sent).Payload).ReadUInt());

            var stored = await repository.GetAsync(
                characterId,
                roboId,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(stored);
            Assert.Equal([0u, 0u, 0u, 0u, 0u], stored.DistributedStatusPoints);
            Assert.Equal(2u, stored.AvailableStatusPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void PacketOpcodes_MatchDecompiledClientBuilders()
    {
        Assert.Equal(0x6755, (ushort)PacketType.DistributeStatusPointAddRequest);
        Assert.Equal(0xC252, (ushort)PacketType.DistributeStatusPointFinishRequest);
        Assert.Equal(0x7764, (ushort)PacketType.DistributeStatusPointAddResponse);
        Assert.Equal(0x7735, (ushort)PacketType.DistributeStatusPointFinishResponse);
        Assert.Equal(0x96B9, (ushort)PacketType.GetTpsUseItemListRequest);
        Assert.Equal(0x6841, (ushort)PacketType.GetTpsUseItemListResponse);
        Assert.Equal(0x6A62, (ushort)PacketType.LiveContestPlayResponse);
    }

    private static byte[] BuildUIntPayload(params uint[] values)
    {
        var writer = new PacketWriter();
        foreach (var value in values)
            writer.Write(value);
        return writer.ToBytes();
    }
}
