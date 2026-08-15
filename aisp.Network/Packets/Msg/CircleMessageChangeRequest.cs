using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleMessageChangeRequest : IIncomingPacket<CircleMessageChangeRequest>
{
    public ulong CircleId;
    public string Message = string.Empty;

    public static CircleMessageChangeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleMessageChangeRequest
        {
            CircleId = reader.ReadULong(),
            Message = reader.ReadString("utf-8"),
        };
    }
}
