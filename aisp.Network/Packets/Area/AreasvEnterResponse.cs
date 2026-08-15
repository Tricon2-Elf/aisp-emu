using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AreasvEnterResponse(uint Result, uint ObjID) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(ObjID);
        return writer.ToBytes();
    }
}
