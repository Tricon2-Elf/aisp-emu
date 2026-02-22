using AISpace.Common.Network.Packets.Area;

namespace AISpace.Common.Network.Handlers;

public class AreaEmotionGetObtainedListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EmotionGetObtainedListRequest;
    public PacketType ResponseType => PacketType.EmotionGetObtainedListResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var ids = new List<uint>();

        // Разблокируем анимации
        for (uint i = 1; i <= 36; i++) ids.Add(i);
        for (uint i = 100; i <= 105; i++) ids.Add(i);

        // Разблокируем базовые голоса игрока
        for (uint i = 1; i <= 48; i++) 
        {
            ids.Add(10101000 + i);
        }

        var response = new EmotionGetObtainedListResponse(0, ids);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}