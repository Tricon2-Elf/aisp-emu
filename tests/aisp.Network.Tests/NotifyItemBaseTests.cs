using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class NotifyItemBaseTests
{
    [Fact]
    public void ToBytes_MatchesItemDataCatalogRow()
    {
        var item = new ItemData
        {
            Key = 12300010,
            SortedListPriority = 12300010,
            ItemId = 12300010,
            IconId = 12300010,
            Socket1 = 1u << 19,
            Category = 11,
        };

        Assert.Equal(item.ToBytes(), new NotifyItemBase(item).ToBytes());
    }
}
