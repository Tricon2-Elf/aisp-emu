using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Msg;

public class ItemGetBaseListResponse : IPacket<ItemGetBaseListResponse>
{
    uint result = 0;
    readonly List<ItemData> Items = [];

    public ItemGetBaseListResponse()
    {
        if (File.Exists("testitems.csv"))
        {
            foreach (var row in File.ReadLines("testitems.csv"))
            {
                var columns = row.Split(',');
                if (columns.Length < 3) continue;

                var id = uint.Parse(columns[0]);
                Items.Add(new ItemData
                {
                    Key = id,
                    SortedListPriority = id,
                    ItemId = id,
                    IconId = id, // Важно: клиент берет иконку отсюда
                    Name = columns[2],
                    Socket1 = uint.Parse(columns[1]),
                    Socket2 = uint.Parse(columns[1])
                });
            }
        }
    }

    public static ItemGetBaseListResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

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