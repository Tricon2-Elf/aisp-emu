using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Msg;

public class ItemGetBaseListResponse : IPacket<ItemGetBaseListResponse>
{
    uint result = 0;
    readonly List<ItemData> Items;

    public ItemGetBaseListResponse()
    {
        Items = [];
        foreach (var row in File.ReadLines("testitems.csv"))
        {
            if (string.IsNullOrEmpty(row))
                continue;
            var columns = row.Split(',');

            if (columns.Length < 3)
                continue;

            var temp = new ItemData
            {
                Key = uint.Parse(columns[0]),
                SortedListPriority = uint.Parse(columns[0]),
                ItemId = uint.Parse(columns[0]),
                Socket1 = uint.Parse(columns[1]),
                Socket2 = uint.Parse(columns[1]),
                Name = columns[2],
                IconId = uint.Parse(columns[0]),
            };
            Items.Add(temp);
        }
    }

    public static ItemGetBaseListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((uint)Items.Count);
        foreach (var item in Items)
            writer.Write(item.ToBytes());
        return writer.ToBytes();
    }
}
