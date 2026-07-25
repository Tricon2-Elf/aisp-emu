using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboCallHandlerTests
{
    [Fact]
    public async Task HandleAsync_EchoesRoboIdAndKeepsRestingState()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            var objectId = RoboObjectIds.For(1);
            var chara = new CharaData(objectId, 1002011, "Robot") { Visual = new CharaVisual(BloodType.A, 1, 1, 0, objectId, 0, 10930010) };
            await using (var seedDb = new MainContext(options))
            {
                await new RoboRepository(seedDb).UpsertAsync(
                    1,
                    new RoboData(1, chara, state: 1)
                    {
                        OwnerAvatarId = 1,
                        EmotionId = 17,
                        AvailableStatusPoints = 3,
                    },
                    TestContext.Current.CancellationToken
                );
            }

            await using (var handlerDb = new MainContext(options))
            {
                var handler = new AreaRoboCallHandler(new RoboRepository(handlerDb), NullLogger<AreaRoboCallHandler>.Instance);
                var session = new CapturingPlayerSession { CharacterId = 1 };
                var writer = new PacketWriter();
                writer.Write(1u);

                await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

                var sent = Assert.Single(session.Sent);
                Assert.Equal(PacketType.RoboCallResponse, sent.Type);
                var callReader = new PacketReader(sent.Payload);
                Assert.Equal(1u, callReader.ReadUInt());
                Assert.Equal(0u, callReader.ReadUInt());
            }

            await using var restartedDb = new MainContext(options);
            var stored = await new RoboRepository(restartedDb).GetAsync(1, 1, TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(0u, stored.State);
            Assert.Equal(1u, stored.OwnerAvatarId);
            Assert.Equal(0u, stored.EmotionId);
            Assert.Equal(3u, stored.AvailableStatusPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
