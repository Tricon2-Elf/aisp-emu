using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaTradeHandler(ILogger<AreaTradeHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.TradeRequest;
    public PacketType ResponseType => (PacketType)0; // Пока без ответа
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = TradeRequestPayload.FromBytes(payload.Span);
        logger.LogInformation(
            $"[TRADE] Player {session.CharacterId} wants to trade with {req.TargetObjectId}. (Not implemented yet)"
        );

        await Task.CompletedTask;
    }
}
