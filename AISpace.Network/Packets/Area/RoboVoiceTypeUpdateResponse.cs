using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class RoboVoiceTypeUpdateResponse : IPacket<RoboVoiceTypeUpdateResponse>
{
    public static RoboVoiceTypeUpdateResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((byte)0); //VoiceType
        return writer.ToBytes();
    }
}
