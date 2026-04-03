using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AreaMapEnterResponse(uint Result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result); // 4 bytes
        return writer.ToBytes();
    }
}
