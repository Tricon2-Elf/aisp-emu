using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleNotifyKick(ulong circleId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        return writer.ToBytes();
    }
}
