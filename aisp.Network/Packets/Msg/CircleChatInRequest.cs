using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleChatInRequest : IIncomingPacket<CircleChatInRequest>
{
    public ulong CircleId;

    public static CircleChatInRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleChatInRequest { CircleId = reader.ReadULong() };
    }
}
