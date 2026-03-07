using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaMyRoomGetFurnitureHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MyRoomGetFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomGetFurnitureResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new MyRoomGetFurnitureResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
