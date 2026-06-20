using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class ShopItemNotify(IReadOnlyList<ShopItemNotify.ShopItem> items) : IOutgoingPacket
{
    public sealed record ShopItem(uint ItemId, ulong NpsPrice, ulong NicoPrice);

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)items.Count);

        foreach (var item in items)
        {
            // Decompiled parser (sub_799AF0) reads fixed 20-byte entries for recv_shop_item.
            // Layout: UInt64 + UInt64 + UInt32.
            writer.Write(item.NpsPrice);
            writer.Write(item.NicoPrice);
            writer.Write(item.ItemId);
        }

        return writer.ToBytes();
    }
}
