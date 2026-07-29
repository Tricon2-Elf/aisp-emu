using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class ShopStartedNotify(uint npcObjectId, string name, uint visualId)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(npcObjectId);
        writer.Write(name, "ASCII");
        writer.Write(visualId);
        writer.Write((uint)0); // talks count
        return writer.ToBytes();
    }
}
