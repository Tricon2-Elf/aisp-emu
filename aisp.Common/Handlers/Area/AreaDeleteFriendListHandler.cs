using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaDeleteFriendListHandler(IFriendRepository friends, SharedState state)
    : PacketHandlerBase<DeleteFriendListRequest, FriendLinkResultResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.DeleteFriendListRequest;
    public override PacketType ResponseType => PacketType.DeleteFriendListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<FriendLinkResultResponse?> HandleAsync(
        DeleteFriendListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId > int.MaxValue || request.AvatarId > int.MaxValue)
            return new FriendLinkResultResponse(1);

        var result = await friends.DeleteAsync((int)session.CharacterId, (int)request.AvatarId, ct);
        if (result.Result != FriendResult.Ok)
            return new FriendLinkResultResponse(1);

        var target = state.GetAreaSessionByCharacterId(request.AvatarId);
        if (target is not null)
            await target.SendAsync(
                PacketType.NotifyDeleteFriendListAvatar,
                new NotifyDeleteFriendListAvatar(session.CharacterId).ToBytes(),
                ct
            );

        return new FriendLinkResultResponse(0);
    }
}
