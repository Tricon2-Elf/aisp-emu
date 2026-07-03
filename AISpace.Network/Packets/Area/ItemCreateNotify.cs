using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class ItemCreateNotify(ItemInstanceData item) : IOutgoingPacket
{
    public ItemInstanceData Item { get; } = item;

    public ItemCreateNotify(uint place, uint serialId, ushort num, uint itemId, ulong expireAt = 0)
        : this(new ItemInstanceData(place, serialId, num, itemId, expireAt)) { }

    public byte[] ToBytes() => Item.ToBytes();
}
