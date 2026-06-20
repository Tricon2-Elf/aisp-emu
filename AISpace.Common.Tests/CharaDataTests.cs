using AISpace.Common.Game;
using AISpace.Network.Data;

namespace AISpace.Common.Tests;

public class CharaDataTests
{
    [Fact]
    public void AddEquip_places_items_at_slot_index_not_packed_order()
    {
        var cd = new CharaData(1, 1, "test");
        cd.AddEquip(
            new[]
            {
                new CharacterEquipSlot(1, 10200100),
                new CharacterEquipSlot(3, 10500070),
            },
            ItemEntityMapper.ResolveEquipSocket
        );

        Assert.Equal(0u, cd.Equips[0].ItemId);
        Assert.Equal(10200100u, cd.Equips[1].ItemId);
        Assert.Equal(0u, cd.Equips[2].ItemId);
        Assert.Equal(10500070u, cd.Equips[3].ItemId);
    }
}
