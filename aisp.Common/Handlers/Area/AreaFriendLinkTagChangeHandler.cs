using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaFriendLinkTagChangeHandler(IFriendRepository friends)
    : PacketHandlerBase<FriendLinkTagChangeRequest, FriendLinkResultResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.FriendLinkTagChangeRequest;
    public override PacketType ResponseType => PacketType.FriendLinkTagChangeResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<FriendLinkResultResponse?> HandleAsync(
        FriendLinkTagChangeRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId > int.MaxValue)
            return new FriendLinkResultResponse(0);

        var result = await friends.SetLinkTagAsync(
            (int)session.CharacterId,
            request.Slot,
            request.Name,
            ct
        );
        // Unlike most result packets, this response is consumed by the Friend Link edit
        // dialog as a tag ID: values <= 0 are a failure, while a positive value is
        // inserted into its local free-tag list. Slots are zero-based, so use a stable
        // one-based ID for the tag itself.
        return new FriendLinkResultResponse(result == FriendResult.Ok ? request.Slot + 1 : 0u);
    }
}
