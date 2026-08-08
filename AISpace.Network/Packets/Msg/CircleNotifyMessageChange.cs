using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleNotifyMessageChange(ulong circleId, string name, string date, string message)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.WriteFixedString(name, 37);
        writer.WriteFixedString(date, 21);
        writer.WriteFixedString(message, 751);
        return writer.ToBytes();
    }
}
