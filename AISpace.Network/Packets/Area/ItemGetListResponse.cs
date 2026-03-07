using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemGetListResponse(uint Result) : IPacket<ItemGetListResponse>
{
    public static ItemGetListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
