using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public sealed class MailProtectCancelHandler(IMailRepository mailRepository)
    : PacketHandlerBase<MailProtectCancelRequest, MailProtectCancelResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MailProtectCancelRequest;
    public override PacketType ResponseType => PacketType.MailProtectCancelResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<MailProtectCancelResponse?> HandleAsync(
        MailProtectCancelRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var updated =
            session.CharacterId != 0
            && request.MailId <= long.MaxValue
            && await mailRepository.SetProtectedAsync(
                checked((int)session.CharacterId),
                (long)request.MailId,
                false,
                ct
            );
        return new MailProtectCancelResponse(updated ? 0u : 1u);
    }
}
