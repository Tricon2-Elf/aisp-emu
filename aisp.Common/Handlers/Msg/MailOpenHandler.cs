using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class MailOpenHandler
    : PacketHandlerBase<MailOpenRequest, MailOpenResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MailOpenRequest;
    public override PacketType ResponseType => PacketType.MailOpenResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override Task<MailOpenResponse?> HandleAsync(
        MailOpenRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // Stub: acknowledge open; client already has MailData from notify / mailbox list.
        return Task.FromResult<MailOpenResponse?>(
            new MailOpenResponse(0, request.MailId, request.Type)
        );
    }
}
