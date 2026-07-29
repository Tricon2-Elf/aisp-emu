using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaMyRoomUseFurnitureHandlerTests
{
    [Fact]
    public async Task CatalogFurnitureUse_IsAcknowledgedWithoutOpeningStorage()
    {
        var handler = new AreaMyRoomUseFurnitureHandler(NullLogger<AreaMyRoomUseFurnitureHandler>.Instance);
        var session = new CapturingPlayerSession { MapId = MyRoomInfo.BaseMapId, CharacterId = 42 };

        await handler.HandleAsync(BuildRequest(42, 77, 1), session, TestContext.Current.CancellationToken);

        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.MyRoomUseFurnitureResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            },
            packet =>
            {
                Assert.Equal(PacketType.NotifyMyRoomUseFurniture, packet.Type);
                var reader = new PacketReader(packet.Payload);
                Assert.Equal(42u, reader.ReadUInt());
                Assert.Equal(77u, reader.ReadUInt());
            }
        );
    }

    [Fact]
    public async Task WrongRoom_RejectsWithoutStorage()
    {
        var handler = new AreaMyRoomUseFurnitureHandler(NullLogger<AreaMyRoomUseFurnitureHandler>.Instance);
        var session = new CapturingPlayerSession { MapId = MyRoomInfo.BaseMapId, CharacterId = 42 };

        await handler.HandleAsync(BuildRequest(99, 77, 1), session, TestContext.Current.CancellationToken);

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.MyRoomUseFurnitureResponse, session.Sent[0].Type);
        Assert.Equal(1u, new PacketReader(session.Sent[0].Payload).ReadUInt());
    }

    private static byte[] BuildRequest(uint roomId, uint furnId, uint reason)
    {
        var writer = new PacketWriter();
        writer.Write(roomId);
        writer.Write(furnId);
        writer.Write(reason);
        return writer.ToBytes();
    }
}
