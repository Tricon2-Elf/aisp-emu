using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleMemberJoinAnswerHandler(ICircleRepository circles, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleMemberJoinAnswerRequest;
    public PacketType ResponseType => PacketType.CircleNotifyJoinRequestResult;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = CircleMemberJoinAnswerRequest.FromBytes(payload.Span);
        // Client send_circle_request_join_answer: Yes → 0, No → 1
        // (dialog Yes button id 0x82040001 maps to answer = (id != Yes) = false).
        var accept = request.Answer == 0;
        var result = await circles.AnswerInviteAsync((int)session.CharacterId, accept, ct);

        // Invitee gets result notify; on accept, roster + add-member go to circle.
        await session.SendAsync(
            PacketType.CircleNotifyJoinRequestResult,
            new CircleNotifyJoinRequestResult((uint)result.Result).ToBytes(),
            ct
        );

        if (result.Result != CircleResult.Ok || result.JoinRequest is null || result.Circle is null)
            return;

        // Also notify the inviter of the answer.
        foreach (
            var client in state.GetOnlineMsgClientsByCharacterId(
                result.JoinRequest.RequesterCharacterId
            )
        )
        {
            await client.SendAsync(
                PacketType.CircleNotifyJoinRequestResult,
                new CircleNotifyJoinRequestResult(accept ? 0u : 1u).ToBytes(),
                ct
            );
        }

        if (!accept || result.Member is null)
            return;

        var name =
            session.Character?.Name
            ?? session.User?.Characters.FirstOrDefault(c => c.Id == (int)session.CharacterId)?.Name
            ?? string.Empty;
        var add = new CircleNotifyAddMember(
            (ulong)result.Circle.Id,
            session.CharacterId,
            name
        ).ToBytes();
        await CircleNotifyHelper.NotifyMembersAsync(
            circles,
            state,
            result.Circle.Id,
            PacketType.CircleNotifyAddMember,
            add,
            ct
        );
        await CircleNotifyHelper.SendRosterAsync(circles, state, result.Circle.Id, ct);
    }
}
