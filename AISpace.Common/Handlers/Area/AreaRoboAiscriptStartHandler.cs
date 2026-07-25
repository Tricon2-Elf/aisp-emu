using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboAiscriptStartHandler(IRoboRepository roboRepository, ILogger<AreaRoboAiscriptStartHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboAiscriptStartRequest;
    public PacketType ResponseType => PacketType.RoboAiscriptStartResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = RoboAiscriptStartRequest.FromBytes(payload.Span);
        var exists = await roboRepository.ExistsAsync(checked((int)session.CharacterId), request.RoboId, ct);
        var result = exists ? 0u : 1u;

        if (exists)
            logger.LogDebug("Acknowledging Robo AI-script start for character {CharacterId}: roboId={RoboId}", session.CharacterId, request.RoboId);
        else
            logger.LogWarning("Rejected Robo AI-script start for character {CharacterId}: roboId={RoboId} is not owned by the character", session.CharacterId, request.RoboId);

        await session.SendAsync(ResponseType, new RoboAiscriptStartResponse(request.RoboId, result).ToBytes(), ct);
    }
}
