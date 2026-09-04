using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaRequestAddFriendListHandler(IFriendRepository friends, SharedState state)
    : PacketHandlerBase<RequestAddFriendListRequest, FriendResultResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.RequestAddFriendListRequest;
    public override PacketType ResponseType => PacketType.RequestAddFriendListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<FriendResultResponse?> HandleAsync(
        RequestAddFriendListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId > int.MaxValue || request.TargetAvatarId > int.MaxValue)
            return new FriendResultResponse(1);

        var result = await friends.RequestAsync(
            (int)session.CharacterId,
            (int)request.TargetAvatarId,
            ct
        );
        if (result.Result != FriendResult.Ok || result.Request is null)
            return new FriendResultResponse(1);

        var target = state.GetAreaSessionByCharacterId(request.TargetAvatarId);
        if (target is not null)
        {
            var name = session.Character?.Name ?? string.Empty;
            await target.SendAsync(
                PacketType.NotifyRequestFriendList,
                new NotifyRequestFriendList(session.CharacterId, name).ToBytes(),
                ct
            );
        }

        return new FriendResultResponse(0);
    }
}
