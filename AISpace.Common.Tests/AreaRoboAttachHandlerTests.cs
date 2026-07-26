using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboAttachHandlerTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(7u)]
    public async Task OwnedRobo_CompletesTwoStepAttachHandshake(uint clientResult)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(1, 1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Attach Robo")) { OwnerAvatarId = 1 };
                await new RoboRepository(seedDb).UpsertAsync(1, robo, TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession { CharacterId = 1 };
            await using (var requestDb = new MainContext(options))
            {
                var handler = new AreaRoboAttachHandler(new RoboRepository(requestDb), NullLogger<AreaRoboAttachHandler>.Instance);
                await handler.HandleAsync(BuildPayload(1), session, TestContext.Current.CancellationToken);
            }

            var attachRequest = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboAttachRequestNotify, attachRequest.Type);
            var requestReader = new PacketReader(attachRequest.Payload);
            Assert.Equal(1u, requestReader.ReadUInt());
            Assert.Equal(1u, requestReader.ReadUInt());

            session.Sent.Clear();
            await using (var replyDb = new MainContext(options))
            {
                var handler = new AreaRoboAttachRequestRHandler(new RoboRepository(replyDb), NullLogger<AreaRoboAttachRequestRHandler>.Instance);
                await handler.HandleAsync(BuildPayload(1, clientResult), session, TestContext.Current.CancellationToken);
            }

            var response = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboAttachResponse, response.Type);
            var responseReader = new PacketReader(response.Payload);
            Assert.Equal(1u, responseReader.ReadUInt());
            Assert.Equal(clientResult, responseReader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UnownedRobo_ReturnsFailureWithoutStartingHandshake()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var handler = new AreaRoboAttachHandler(new RoboRepository(db), NullLogger<AreaRoboAttachHandler>.Instance);
            var session = new CapturingPlayerSession { CharacterId = 1 };

            await handler.HandleAsync(BuildPayload(99), session, TestContext.Current.CancellationToken);

            var response = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboAttachResponse, response.Type);
            var reader = new PacketReader(response.Payload);
            Assert.Equal(99u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
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
}
