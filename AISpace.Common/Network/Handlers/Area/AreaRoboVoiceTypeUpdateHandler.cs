namespace AISpace.Common.Network.Handlers;

public class AreaRoboVoiceTypeUpdateHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.RoboVoiceTypeUpdateRequest; // 0x9305
    public PacketType ResponseType => PacketType.RoboVoiceTypeUpdateResponse; // 0x8F10
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        byte voiceType = reader.ReadByte();

        // Ответ должен содержать результат (4 байта) и подтвержденный тип (1 байт)
        var writer = new PacketWriter();
        writer.Write((uint)0);    // Success
        writer.Write(voiceType);  // Тип голоса

        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}