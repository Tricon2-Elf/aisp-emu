using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleNotifyMarkChange(ulong circleId, uint markId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(markId);
        return writer.ToBytes();
    }
}
