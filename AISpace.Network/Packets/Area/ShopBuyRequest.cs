using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public sealed class ShopBuyRequest : IIncomingPacket<ShopBuyRequest>
{
    public required IReadOnlyList<ShopBuyRequestedItem> Items { get; init; }
    public ShopPriceType PriceType { get; init; }

    public static ShopBuyRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();

        if (count > 500)
            throw new InvalidDataException(
                $"ShopBuyRequest item count {count} exceeds protocol limit"
            );

        var items = new List<ShopBuyRequestedItem>((int)count);
        for (var i = 0; i < count; i++)
        {
            items.Add(
                new ShopBuyRequestedItem(
                    reader.ReadUInt(),
                    reader.ReadUShort(),
                    reader.ReadUInt(),
                    reader.ReadUInt()
                )
            );
        }

        return new ShopBuyRequest { Items = items, PriceType = (ShopPriceType)reader.ReadByte() };
    }
}
