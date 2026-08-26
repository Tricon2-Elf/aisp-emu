using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaEquipOrderListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EquipOrderListRequest;

    public PacketType ResponseType => PacketType.EquipOrderListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new EquipOrderListResponse { CharaOrders = CharaOrderData.WardrobeOrders };
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
