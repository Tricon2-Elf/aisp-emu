using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaNiconiCommonsBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsBaseListRequest;

    public PacketType ResponseType => PacketType.NiconiCommonsBaseListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var response = new NiconiCommonsBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
