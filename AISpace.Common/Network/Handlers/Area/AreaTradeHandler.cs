using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaTradeHandler(ILogger<AreaTradeHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.TradeRequest;
    public PacketType ResponseType => (PacketType)0; // Пока без ответа
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        uint targetObjId = reader.ReadUInt();
        
        logger.LogInformation($"[TRADE] Player {connection.CharacterId} wants to trade with {targetObjId}. (Not implemented yet)");
        
        // В будущем здесь будет логика открытия окна трейда у обоих игроков
        await Task.CompletedTask;
    }
}