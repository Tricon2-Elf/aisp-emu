using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboAiscriptStartHandlerTests
{
    [Fact]
    public async Task OwnedRobo_ReturnsSuccess()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboObjectIds.For(1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Owned Robo")) { OwnerAvatarId = 1 };
                await new RoboRepository(seedDb).UpsertAsync(1, robo, TestContext.Current.CancellationToken);
            }

            await AssertResponseAsync(options, characterId: 1, roboId: 1, expectedResult: 0);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RoboOwnedByAnotherCharacter_ReturnsFailure()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboObjectIds.For(1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Other Robo")) { OwnerAvatarId = 2 };
                await new RoboRepository(seedDb).UpsertAsync(2, robo, TestContext.Current.CancellationToken);
            }

            await AssertResponseAsync(options, characterId: 1, roboId: 1, expectedResult: 1);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task AssertResponseAsync(Microsoft.EntityFrameworkCore.DbContextOptions<MainContext> options, uint characterId, uint roboId, uint expectedResult)
    {
        await using var handlerDb = new MainContext(options);
        var handler = new AreaRoboAiscriptStartHandler(new RoboRepository(handlerDb), NullLogger<AreaRoboAiscriptStartHandler>.Instance);
        var session = new CapturingPlayerSession { CharacterId = characterId };
        var writer = new PacketWriter();
        writer.Write(roboId);

        await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.RoboAiscriptStartResponse, sent.Type);
        var reader = new PacketReader(sent.Payload);
        Assert.Equal(roboId, reader.ReadUInt());
        Assert.Equal(expectedResult, reader.ReadUInt());
    }
}
