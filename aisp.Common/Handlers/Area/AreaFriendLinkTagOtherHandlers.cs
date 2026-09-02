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

public sealed class AreaRequestFriendListAnswerHandler(IFriendRepository friends, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RequestFriendListAnswerRequest;
    public PacketType ResponseType => PacketType.NotifyAddFriendListResult;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = RequestFriendListAnswerRequest.FromBytes(payload.Span);
        if (session.CharacterId > int.MaxValue)
        {
            await session.SendAsync(
                PacketType.NotifyAddFriendListResult,
                new FriendResultResponse(1).ToBytes(),
                ct
            );
            return;
        }

        var accept = request.Answer == 0;
        var result = await friends.AnswerAsync((int)session.CharacterId, accept, ct);
        var notifyResult = result.Result == FriendResult.Ok && accept ? 0u : 1u;
        var notify = new FriendResultResponse(notifyResult).ToBytes();

        await session.SendAsync(PacketType.NotifyAddFriendListResult, notify, ct);
        if (result.Request is null)
            return;

        var requester = state.GetAreaSessionByCharacterId(
            checked((uint)result.Request.RequesterCharacterId)
        );
        if (requester is not null)
            await requester.SendAsync(PacketType.NotifyAddFriendListResult, notify, ct);
    }
}

public sealed class AreaRequestAddFriendListCancelHandler(
    IFriendRepository friends,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RequestAddFriendListCancelRequest;
    public PacketType ResponseType => PacketType.NotifyRequestFriendListCancel;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId > int.MaxValue)
            return;

        var result = await friends.CancelAsync((int)session.CharacterId, ct);
        if (result.Result != FriendResult.Ok || result.Request is null)
            return;

        var target = state.GetAreaSessionByCharacterId(
            checked((uint)result.Request.TargetCharacterId)
        );
        if (target is not null)
        {
            await target.SendAsync(
                PacketType.NotifyRequestFriendListCancel,
                new FriendResultResponse(0).ToBytes(),
                ct
            );
        }
    }
}
