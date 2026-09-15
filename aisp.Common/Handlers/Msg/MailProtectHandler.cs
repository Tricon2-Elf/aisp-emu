using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public sealed class MailProtectHandler(IMailRepository mailRepository)
    : PacketHandlerBase<MailProtectRequest, MailProtectResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MailProtectRequest;
    public override PacketType ResponseType => PacketType.MailProtectResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<MailProtectResponse?> HandleAsync(
        MailProtectRequest request,
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
                true,
                ct
            );
        return new MailProtectResponse(updated ? 0u : 1u);
    }
}
