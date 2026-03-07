using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AreasvEnterResponse(uint Result, uint ObjID) : IPacket<AreasvEnterResponse>
{
    public static AreasvEnterResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(ObjID);
        return writer.ToBytes();
    }
}
