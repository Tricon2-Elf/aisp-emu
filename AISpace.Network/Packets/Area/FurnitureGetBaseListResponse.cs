using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FurnitureGetBaseListResponse : IPacket<FurnitureGetBaseListResponse>
{
    public static FurnitureGetBaseListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);
        writer.Write((uint)0);
        return writer.ToBytes();
    }
}
