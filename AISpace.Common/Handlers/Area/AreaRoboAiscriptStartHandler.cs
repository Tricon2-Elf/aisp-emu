using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboAiscriptStartHandler(ILogger<AreaRoboAiscriptStartHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboAiscriptStartRequest;
    public PacketType ResponseType => PacketType.RoboAiscriptStartResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = RoboAiscriptStartRequest.FromBytes(payload.Span);
        // Non-zero result = failure. Success (0) opens an upload session the client immediately ends and retries ~500ms.
        logger.LogDebug("RoboAiscriptStartRequest from character {CharacterId}: roboId={RoboId} (rejected — aiscript not implemented)", session.CharacterId, request.RoboId);
        await session.SendAsync(ResponseType, new RoboAiscriptStartResponse(request.RoboId, 1).ToBytes(), ct);
    }
}
