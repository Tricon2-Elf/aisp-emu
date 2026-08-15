using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class MoneyUpdatedNicopointNotify(ulong niconicoPoints) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(niconicoPoints);
        return writer.ToBytes();
    }
}
