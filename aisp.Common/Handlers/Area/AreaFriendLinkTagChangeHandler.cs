using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaFriendLinkTagChangeHandler(
    IFriendRepository friends,
    IWordFilter wordFilter
)
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

        if (
            !string.IsNullOrWhiteSpace(request.Name)
            && wordFilter.ContainsBlockedWord(WordFilterLevel.Complete, request.Name)
        )
            return new FriendLinkResultResponse(0);

        var result = await friends.SetLinkTagAsync(
            (int)session.CharacterId,
            request.Slot,
            request.Name,
            ct
        );
        // Unlike most result packets, this response is consumed as the tag's global ID.
        return new FriendLinkResultResponse(
            result == FriendResult.Ok
                ? FriendLinkTagCatalog.GetFreeTagId(request.Name, request.Slot)
                : 0u
        );
    }
}
