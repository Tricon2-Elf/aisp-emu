using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaFriendLinkTagGetFreeHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.FriendLinkTagGetFreeRequest;
    public PacketType ResponseType => PacketType.GetFreeFriendLinkTagResponse;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    ) =>
        session.SendAsync(
            ResponseType,
            new GetFreeFriendLinkTagResponse(0, FriendLinkTagCatalog.FreeTags).ToBytes(),
            ct
        );
}
