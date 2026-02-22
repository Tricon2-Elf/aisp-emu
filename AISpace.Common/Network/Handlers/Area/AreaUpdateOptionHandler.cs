namespace AISpace.Common.Network.Handlers;

public class AreaUpdateOptionHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.UpdateOptionRequest;
    public PacketType ResponseType => PacketType.UpdateOptionResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // 1. Подтверждаем получение настройки
        var writer = new PacketWriter();
        writer.Write((uint)1); // Пробуем '1' (Успех в некоторых билдах)
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);

        // 2. ХИТРОСТЬ: Шлем пакет "перезагрузки" опций. 
        // В AISp@ce это заставляет клиент обновить флаги управления (WASD)
        await connection.SendAsync(PacketType.UpdateOptionResponse, writer.ToBytes(), ct);
    }
}