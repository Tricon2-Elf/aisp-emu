using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaEventScriptPlayHandler(
    ILogger<AreaEventScriptPlayHandler> logger,
    ServerScriptDispatcher? serverScriptDispatcher = null
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventScriptPlayRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            serverScriptDispatcher is not null
            && await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct)
        )
            return;

        var request = EventScriptPlayRequest.FromBytes(payload.Span);
        if (request.Result != 0)
        {
            logger.LogWarning(
                "EventScriptPlay from character {CharacterId}: result={Result}",
                session.CharacterId,
                request.Result
            );
            session.PendingEventEndAfterFade = false;
            session.ActiveEventKey = null;
            session.ActiveEventKind = NpcEventKind.None;
            session.ActiveEventCompletionPolicy = EventCompletionPolicy.Once;
            await session.SendAsync(
                PacketType.EventEndNotify,
                new EventEndNotify(unchecked((uint)request.Result)).ToBytes(),
                ct
            );
            return;
        }

        logger.LogInformation(
            "EventScriptPlay from character {CharacterId}: success — clearing fade then ending event",
            session.CharacterId
        );
        session.PendingEventEndAfterFade = true;
        await session.SendAsync(
            PacketType.EventFadeInNotify,
            new EventFadeNotify(1f, 255, 255, 255).ToBytes(),
            ct
        );
    }
}
