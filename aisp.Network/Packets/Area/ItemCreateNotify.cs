using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class ItemCreateNotify(ItemInstanceData item) : IOutgoingPacket
{
    public ItemInstanceData Item { get; } = item;

    public ItemCreateNotify(uint place, uint serialId, ushort num, uint itemId, ulong expireAt = 0)
        : this(new ItemInstanceData(place, serialId, num, itemId, expireAt)) { }

    public byte[] ToBytes() => Item.ToBytes();
}
