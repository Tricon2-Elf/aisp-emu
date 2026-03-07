using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AreasvLeaveResponse(uint Result = 0) : IPacket<AreasvLeaveResponse>
{
    public static AreasvLeaveResponse FromBytes(ReadOnlySpan<byte> data)
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
