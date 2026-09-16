using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public sealed class MailDeleteHandler(IMailRepository mailRepository)
    : PacketHandlerBase<MailDeleteRequest, MailDeleteResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MailDeleteRequest;
    public override PacketType ResponseType => PacketType.MailDeleteResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<MailDeleteResponse?> HandleAsync(
        MailDeleteRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var deleted =
            session.CharacterId != 0
            && request.MailId <= long.MaxValue
            && await mailRepository.DeleteAsync(
                checked((int)session.CharacterId),
                (long)request.MailId,
                request.Type,
                ct
            );
        return new MailDeleteResponse(deleted ? 0u : 1u, request.MailId, request.Type);
    }
}
