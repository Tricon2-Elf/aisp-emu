using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleResignRequest : IIncomingPacket<CircleResignRequest>
{
    public ulong CircleId;

    public static CircleResignRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleResignRequest { CircleId = reader.ReadULong() };
    }
}
