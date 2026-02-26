using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class CircleTalkHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleTalkRequest;
    public PacketType ResponseType => (PacketType)0xA9C1; 
    public MessageDomain Domain => MessageDomain.Area; 

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // В Circle чате клиент шлет MessageID (4) + BalloonID (4) + Text...
        // Чтобы тебя не крашило, нужно вернуть ТОТ ЖЕ MessageID
        var reader = new PacketReader(payload.Span);
        uint msgId = reader.ReadUInt();

        var writer = new PacketWriter();
        writer.Write(msgId);   // ID сообщения из запроса
        writer.Write((uint)0); // Result = Success
        
        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}