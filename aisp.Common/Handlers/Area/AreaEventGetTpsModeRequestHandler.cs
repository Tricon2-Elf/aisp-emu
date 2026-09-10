using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaEventGetTpsModeRequestHandler(
    ILogger<AreaEventGetTpsModeRequestHandler> logger,
    IRoboRepository roboRepository
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventGetTpsModeRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = EventGetTpsModeRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "EventGetTpsMode result={Result} for character {CharacterId} on map {MapId}",
            req.Result,
            session.CharacterId,
            session.MapId
        );

        if (req.Result != 1)
        {
            logger.LogWarning(
                "Client reported TPS controller inactive (result={Result})",
                req.Result
            );
            return;
        }

        if (session.IsTpsMode)
        {
            logger.LogInformation(
                "Already in TPS Charadoll mode for character {CharacterId}; skipping re-enter",
                session.CharacterId
            );
            return;
        }

        await TpsCombatEnter.TryEnterAsCharadollAsync(session, roboRepository, logger, ct);
    }
}
