using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaFriendLinkTagGetHandler : IPacketHandler, IRequiresAuthenticatedSession
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
        // Empty collections are client-compatible. The stored custom tags remain in
        // the database, but must not be emitted until ReadTagData is fully decoded.
        var response = new FriendLinkTagGetResponse(0, req.TargetObjectId);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
