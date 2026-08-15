using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleNotifyMessageChange(
    ulong circleId,
    string authorName,
    string date,
    string message
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        // Client reads null-terminated author/date/message (max 37 / 21 / 751 including NUL).
        writer.Write(authorName, 36);
        writer.Write(date, 20);
        writer.Write(message, 750);
        return writer.ToBytes();
    }
}
