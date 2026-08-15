using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class ShopStartedNotify(uint npcObjectId, string name, uint visualId)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(npcObjectId);
        writer.Write(name, 192);
        writer.Write(visualId);
        writer.Write((uint)0); // talks count
        return writer.ToBytes();
    }
}
