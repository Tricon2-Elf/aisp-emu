using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaTradeHandler(ILogger<AreaTradeHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.TradeRequest;
    public PacketType ResponseType => (PacketType)0; // Пока без ответа
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        uint targetObjId = reader.ReadUInt();

        logger.LogInformation($"[TRADE] Player {session.CharacterId} wants to trade with {targetObjId}. (Not implemented yet)");

        // В будущем здесь будет логика открытия окна трейда у обоих игроков
        await Task.CompletedTask;
    }
}
