using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class UccAdvFigureBaseListResponse : IPacket<UccAdvFigureBaseListResponse>
{
    public static UccAdvFigureBaseListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // adv_figures
        return writer.ToBytes();
    }
}
