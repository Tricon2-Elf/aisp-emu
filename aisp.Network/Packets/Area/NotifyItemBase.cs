using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_item_base: one ITEM_DATA catalog row (same payload as get_item_base_list entries).
/// </summary>
public sealed class NotifyItemBase(ItemData item) : IOutgoingPacket
{
    public ItemData Item { get; } = item;

    public byte[] ToBytes() => Item.ToBytes();
}
