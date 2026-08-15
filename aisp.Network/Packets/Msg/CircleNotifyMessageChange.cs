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
        writer.Write(Truncate(authorName, 36), "utf-8");
        writer.Write(Truncate(date, 20), "utf-8");
        writer.Write(Truncate(message, 750), "utf-8");
        return writer.ToBytes();
    }

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars];
}
