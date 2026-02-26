namespace AISpace.Common.Network.Packets.Area;

public class MascotGetCountResponse : IPacket<MascotGetCountResponse>
{
    public static MascotGetCountResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)1); //Result
        writer.Write((uint)0); //count
        writer.Write((uint)0); //serial_id
        writer.Write((uint)0); //name
        return writer.ToBytes();
    }
}
