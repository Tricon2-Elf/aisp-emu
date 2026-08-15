using aisp.Network;

namespace aisp.Network.Packets.Area;

public class RoboVoiceTypeUpdateResponse(uint result, byte voiceType) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(voiceType);
        return writer.ToBytes();
    }
}
