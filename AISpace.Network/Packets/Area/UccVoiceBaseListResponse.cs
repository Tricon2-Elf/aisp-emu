using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class UccVoiceBaseListResponse : IPacket<UccVoiceBaseListResponse>
{
    public static UccVoiceBaseListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // voice_data
        return writer.ToBytes();
    }
}
