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

public class AreaRoboCreateHandlerTests
{
    [Fact]
    public void RoboCreateRequest_ParsesNameVisualAndModel()
    {
        // Payload from client log: "Robot\0" + visual(19) + model 1002011
        byte[] payload = [0x52, 0x6F, 0x62, 0x6F, 0x74, 0x00, 0x04, 0x00, 0x00, 0x00, 0x0C, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1B, 0x4A, 0x0F, 0x00];

        var request = RoboCreateRequest.FromBytes(payload);
        Assert.Equal("Robot", request.Name);
        Assert.Equal(1002011u, request.ModelId);
        Assert.Equal(12, request.Visual.Month);
        Assert.Equal(12, request.Visual.Day);
    }

    [Fact]
    public void RoboData_WireSize_MatchesClientReadRoboData()
    {
        var chara = new CharaData(RoboRepository.GetObjectId(1, 1), 1002011, "Robot");
        var bytes = new RoboData(1, chara).ToBytes();
        Assert.Equal(RoboData.WireSize, bytes.Length);
        Assert.Equal(sizeof(uint) + RoboData.WireSize, new RoboCreateResponse(0, new RoboData(1, chara)).ToBytes().Length);
    }

    [Fact]
    public async Task HandleAsync_SendsSuccessWithRoboData()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var repository = new RoboRepository(db);
            var handler = new AreaRoboCreateHandler(repository, NullLogger<AreaRoboCreateHandler>.Instance);
            var session = new CapturingPlayerSession { CharacterId = 42 };
            byte[] payload = [0x52, 0x6F, 0x62, 0x6F, 0x74, 0x00, 0x04, 0x00, 0x00, 0x00, 0x0C, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1B, 0x4A, 0x0F, 0x00];

            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboCreateResponse, sent.Type);
            Assert.Equal(sizeof(uint) + RoboData.WireSize, sent.Payload.Length);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt()); // result
            Assert.Equal(1u, reader.ReadUInt()); // roboId
            Assert.Equal(42u, reader.ReadUInt()); // ownerAvatarId
            Assert.Equal(0u, reader.ReadUInt()); // state resting

            await using var restartedDb = new MainContext(options);
            var stored = await new RoboRepository(restartedDb).GetAsync(42, 1, TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(42u, stored.OwnerAvatarId);
            Assert.Equal(RoboRepository.GetObjectId(42, 1), stored.Chara.Visual.VisualId);
            Assert.Equal("Robot", stored.Character.Name);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
