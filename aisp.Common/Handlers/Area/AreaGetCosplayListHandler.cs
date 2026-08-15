using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaGetCosplayListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetCosplayListRequest;
    public PacketType ResponseType => PacketType.GetCosplayListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetCosplayListRequest.FromBytes(payload.Span);
        await session.SendAsync(
            ResponseType,
            new GetCosplayListResponse(0, request.RoboId).ToBytes(),
            ct
        );
    }
}
