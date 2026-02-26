using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Msg;

public class ItemGetBaseListResponse : IPacket<ItemGetBaseListResponse>
{
    uint result = 0;
    readonly List<ItemData> Items = [];

    // White list of IDs for starter clothes
    private readonly HashSet<uint> _starterIds = [10100220, 10200100, 10400030, 10500070, 10100060, 10200090, 10400000, 10500010];

    public ItemGetBaseListResponse()
    {
        if (File.Exists("testitems.csv"))
        {
            foreach (var line in File.ReadLines("testitems.csv"))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = line.Split(',');
                if (columns.Length < 3)
                    continue;

                if (!uint.TryParse(columns[0], out var id))
                    continue;
                if (!_starterIds.Contains(id))
                    continue;

                uint socket = 0;
                if (columns.Length > 1)
                    uint.TryParse(columns[1], out socket);

                var name = columns[2];

                uint iconId = id;
                if (columns.Length > 3 && uint.TryParse(columns[3], out var parsedIcon))
                {
                    iconId = parsedIcon;
                }

                uint category = 1;
                if (socket == 2)
                    category = 2;
                if (socket == 4)
                    category = 8;
                if (socket == 8)
                    category = 8;
                if (socket == 16)
                    category = 4;

                Items.Add(
                    new ItemData
                    {
                        Key = id,
                        SortedListPriority = id,
                        ItemId = id,
                        IconId = iconId,
                        Name = name,
                        Socket1 = socket,
                        Socket2 = socket,
                        Category = category,
                    }
                );
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
