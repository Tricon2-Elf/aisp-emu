using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboCallHandler(IRoboRepository roboRepository, ILogger<AreaRoboCallHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboCallRequest;
    public PacketType ResponseType => PacketType.RoboCallResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var characterId = checked((int)session.CharacterId);
        var request = RoboCallRequest.FromBytes(payload.Span);
        logger.LogInformation("RoboCallRequest from character {CharacterId}: roboId={RoboId}", session.CharacterId, request.RoboId);

        // Keep state=0 (resting). state=1 spawns a SelfRobo with AI-script sync that start/ends in a ~500ms loop
        // until aiscript upload is implemented. Unique object ids already make the resting cleanup path safe.
        await roboRepository.UpdateStateAsync(characterId, request.RoboId, 0, ct);

        await session.SendAsync(ResponseType, new RoboCallResponse(request.RoboId, 0).ToBytes(), ct);
    }
}
