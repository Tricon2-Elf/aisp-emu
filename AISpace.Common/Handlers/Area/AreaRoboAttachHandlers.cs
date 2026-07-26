using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaRoboAttachHandler(IRoboRepository roboRepository, ILogger<AreaRoboAttachHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboAttachRequest;
    public PacketType ResponseType => PacketType.RoboAttachRequestNotify;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = RoboAttachRequest.FromBytes(payload.Span);
        var owned = session.CharacterId != 0 && await roboRepository.ExistsAsync(checked((int)session.CharacterId), request.RoboId, ct);
        if (!owned)
        {
            logger.LogWarning("Rejected Robo attach for character {CharacterId}: Robo {RoboId} is not owned by the character", session.CharacterId, request.RoboId);
            await session.SendAsync(PacketType.RoboAttachResponse, new RoboAttachResponse(request.RoboId, 1).ToBytes(), ct);
            return;
        }

        logger.LogDebug("Starting Robo conversation handshake for character {CharacterId}: Robo {RoboId}, avatar object {AvatarObjectId}", session.CharacterId, request.RoboId, session.CharacterId);
        await session.SendAsync(ResponseType, new RoboAttachRequestNotify(request.RoboId, session.CharacterId).ToBytes(), ct);
    }
}

public sealed class AreaRoboAttachRequestRHandler(IRoboRepository roboRepository, ILogger<AreaRoboAttachRequestRHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboAttachRequestRRequest;
    public PacketType ResponseType => PacketType.RoboAttachResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = RoboAttachRequestRRequest.FromBytes(payload.Span);
        var owned = session.CharacterId != 0 && await roboRepository.ExistsAsync(checked((int)session.CharacterId), request.RoboId, ct);
        var result = owned ? request.Result : 1u;

        if (!owned)
            logger.LogWarning("Rejected Robo attach reply for character {CharacterId}: Robo {RoboId} is not owned by the character", session.CharacterId, request.RoboId);
        else
            logger.LogDebug("Completing Robo conversation handshake for character {CharacterId}: Robo {RoboId}, result {Result}", session.CharacterId, request.RoboId, result);

        await session.SendAsync(ResponseType, new RoboAttachResponse(request.RoboId, result).ToBytes(), ct);
    }
}
