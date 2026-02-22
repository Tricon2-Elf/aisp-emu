using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class ItemGetBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;
    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // Отправляем пустой список (0 предметов), чтобы клиент не вылетал
        var writer = new PacketWriter();
        writer.Write((uint)0); // Result
        writer.Write((uint)0); // Количество предметов: 0
        
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}