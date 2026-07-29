using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaEventSelectExecRHandler(
    ServerScriptDispatcher serverScriptDispatcher,
    ILogger<AreaEventSelectExecRHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventSelectExecRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct))
            return;

        logger.LogDebug(
            "Ignoring EventSelectExecR from character {CharacterId}: no server script handled the packet",
            session.CharacterId
        );
    }
}
