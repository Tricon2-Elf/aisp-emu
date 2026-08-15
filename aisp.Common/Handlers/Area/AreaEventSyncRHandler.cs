using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaEventSyncRHandler(
    ServerScriptDispatcher serverScriptDispatcher,
    ILogger<AreaEventSyncRHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventSyncRRequest;
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
            "Ignoring EventSyncR from character {CharacterId}: no server script handled the packet",
            session.CharacterId
        );
    }
}
