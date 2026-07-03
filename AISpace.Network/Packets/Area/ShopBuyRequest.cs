using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class ShopBuyRequest : IIncomingPacket<ShopBuyRequest>
{
    // send_shop_buy encodes each entry as 16 bytes (sub_797C00):
    // uint + ushort + uint + uint
    public sealed record RequestedItem(uint ItemId, ushort UnknownWord, uint Unknown1, uint Unknown2);

    public required IReadOnlyList<RequestedItem> Items { get; init; }
    public ShopPriceType PriceType { get; init; }

    public static ShopBuyRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();

        if (count > 500)
            throw new InvalidDataException($"ShopBuyRequest item count {count} exceeds protocol limit");

        var items = new List<RequestedItem>((int)count);
        for (var i = 0; i < count; i++)
        {
            items.Add(new RequestedItem(reader.ReadUInt(), reader.ReadUShort(), reader.ReadUInt(), reader.ReadUInt()));
        }

        return new ShopBuyRequest
        {
            Items = items,
            PriceType = (ShopPriceType)reader.ReadByte(),
        };
    }
}
