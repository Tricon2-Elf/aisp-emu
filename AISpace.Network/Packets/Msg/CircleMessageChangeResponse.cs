using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleMessageChangeResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
