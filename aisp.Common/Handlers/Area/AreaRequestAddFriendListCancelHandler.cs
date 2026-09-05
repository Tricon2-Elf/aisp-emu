using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

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
