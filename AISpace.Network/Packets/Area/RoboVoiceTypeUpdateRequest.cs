using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class RoboVoiceTypeUpdateRequest(byte voiceType) : IIncomingPacket<RoboVoiceTypeUpdateRequest>
{
    public byte VoiceType { get; } = voiceType;

    public static RoboVoiceTypeUpdateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboVoiceTypeUpdateRequest(reader.ReadByte());
    }
}
