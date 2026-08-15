using aisp.Network;

namespace aisp.Network.Packets.Area;

public class UccVoiceBaseListResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // voice_data
        return writer.ToBytes();
    }
}
