using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaMapEnterHandler(ILogger<AreaMapEnterHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapEnterRequest;
    public PacketType ResponseType => PacketType.MapEnterResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        logger.LogInformation($"[MAP] Client {connection.Id} entering map (Escape/Entry)");

        // Сбрасываем позицию при входе на карту
        connection.X = 0f;
        connection.Y = 0.1f;
        connection.Z = 0f;

        // Результат 0 - успех
        var response = new AreaMapEnterResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}