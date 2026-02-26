using AISpace.Common.Network.Packets.Area;
namespace AISpace.Common.Network.Handlers;

public class AreaAdventureUploadRateGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AdventureUploadRateGetRequest;
    public PacketType ResponseType => PacketType.AdventureUploadRateGetResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // Просто отвечаем ОК (Результат 1)
        var writer = new PacketWriter();
        writer.Write((uint)1); 
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}