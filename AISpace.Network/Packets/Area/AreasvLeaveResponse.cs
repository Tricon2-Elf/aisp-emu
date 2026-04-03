using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AreasvLeaveResponse(uint Result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
