using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaFriendLinkTagOtherHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.FriendLinkTagGetOtherRequest;
    public PacketType ResponseType => PacketType.FriendLinkTagGetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = FriendLinkTagGetRequest.FromBytes(payload.Span);
        var response = new FriendLinkTagGetResponse(0, req.TargetObjectId);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
