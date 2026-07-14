using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaEventIslandSelectExecRHandler(ServerScriptDispatcher serverScriptDispatcher, ILogger<AreaEventIslandSelectExecRHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventIslandSelectExecRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct))
            return;

        logger.LogDebug("Ignoring EventIslandSelectExecR from character {CharacterId}: no server script handled the packet", session.CharacterId);
    }
}
