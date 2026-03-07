using AISpace.Common.Network.Packets.Area;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaEquipOrderListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EquipOrderListRequest;

    public PacketType ResponseType => PacketType.EquipOrderListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new EquipOrderListResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
