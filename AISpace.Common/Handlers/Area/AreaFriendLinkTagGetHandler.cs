using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaFriendLinkTagGetHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.FriendLinkTagGetRequest;
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
