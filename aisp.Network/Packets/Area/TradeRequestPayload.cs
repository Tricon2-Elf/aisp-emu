using aisp.Network;

namespace aisp.Network.Packets.Area;

public class TradeRequestPayload(uint targetObjectId) : IIncomingPacket<TradeRequestPayload>
{
    public uint TargetObjectId { get; } = targetObjectId;

    public static TradeRequestPayload FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new TradeRequestPayload(reader.ReadUInt());
    }
}
