using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class UpdateOptionResponse : IPacket<UpdateOptionResponse>
{
    public static UpdateOptionResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)1); //Result
        return writer.ToBytes();
    }
}
