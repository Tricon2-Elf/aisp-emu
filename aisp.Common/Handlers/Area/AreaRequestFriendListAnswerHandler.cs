using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

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
