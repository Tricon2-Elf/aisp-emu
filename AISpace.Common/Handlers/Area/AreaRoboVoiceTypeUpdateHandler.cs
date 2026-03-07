using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboVoiceTypeUpdateHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.RoboVoiceTypeUpdateRequest;
    public PacketType ResponseType => PacketType.RoboVoiceTypeUpdateResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        byte voiceType = reader.ReadByte();

        // Response should contain result (4 bytes) and confirmed type (1 byte)
        var writer = new PacketWriter();
        writer.Write((uint)0); // Success
        writer.Write(voiceType); // Voice type

        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
