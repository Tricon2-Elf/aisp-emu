using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Common.Tests;

public class AreaRoboGetListHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_robo_loaded_from_database()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 9, TestContext.Current.CancellationToken);
            var objectId = RoboObjectIds.For(1);
            var robo = new RoboData(1, new CharaData(objectId, 1002011, "Database Robo"), state: 0) { OwnerAvatarId = 9 };

            await using (var writeDb = new MainContext(options))
                await new RoboRepository(writeDb).UpsertAsync(9, robo, TestContext.Current.CancellationToken);

            await using var readDb = new MainContext(options);
            var handler = new AreaRoboGetListHandler(new RoboRepository(readDb));
            var session = new CapturingPlayerSession { CharacterId = 9 };

            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboGetListResponse, sent.Type);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            var loaded = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
            Assert.Equal(9u, loaded.OwnerAvatarId);
            Assert.Equal("Database Robo", loaded.Character.Name);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
