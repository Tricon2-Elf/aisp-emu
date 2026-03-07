using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEquipOrderListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EquipOrderListRequest;

    public PacketType ResponseType => PacketType.EquipOrderListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new EquipOrderListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
