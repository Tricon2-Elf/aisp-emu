using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network.Data;

namespace AISpace.Common.Tests;

public class ItemEntityMapperTests
{
    [Theory]
    [InlineData(10100220, 8)] // shirt
    [InlineData(10200100, 32)] // pants
    [InlineData(10200000, 16)] // skirt
    [InlineData(10400030, 128)] // socks
    [InlineData(10500070, 512)] // shoes (primary wardrobe slot)
    [InlineData(10000010, 2)] // hat
    public void ResolveBodyspot_maps_clothing_to_client_slot_dockets(int itemId, uint expected)
    {
        Assert.Equal(expected, ItemEntityMapper.ResolveBodyspot(itemId));
    }

    [Fact]
    public void ResolveBodyspot_ignores_stored_socket_for_clothing()
    {
        var item = new Item { Id = 10500070, Socket = 16, Name = "テスト靴" };
        Assert.Equal(512u, ItemEntityMapper.ResolveBodyspot(item));
    }

    [Theory]
    [InlineData(10100220)] // shirt
    [InlineData(10200100)] // pants
    [InlineData(10400030)] // socks
    [InlineData(10500070)] // shoes
    public void ResolveEquipSocket_sends_zero_for_clothing_so_client_uses_catalog_bodyspot(uint itemId)
    {
        var slot = new CharacterEquipSlot(0, itemId);
        Assert.Equal(0u, ItemEntityMapper.ResolveEquipSocket(slot));
    }

    [Fact]
    public void ToItemBaseListData_sets_dual_shoe_catalog_sockets()
    {
        var item = new Item { Id = 10500070, Socket = 0, Name = "テスト靴" };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(512u, data.Socket1);
        Assert.Equal(256u, data.Socket2);
        Assert.Equal(8u, data.Category);
    }

    [Fact]
    public void ToItemBaseListData_sets_socket2_zero_for_single_slot_clothing()
    {
        var item = new Item { Id = 10100220, Socket = 0, Name = "テストシャツ" };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(8u, data.Socket1);
        Assert.Equal(0u, data.Socket2);
    }

    [Theory]
    [InlineData(10100220, 101)]
    [InlineData(10500070, 105)]
    public void ToItemBaseListData_sets_limit_map_key_for_wardrobe_equip_checks(int itemId, uint expectedKey)
    {
        var item = new Item { Id = itemId, Socket = 0, Name = "test" };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedKey, data._0x044c);
    }

    [Theory]
    [InlineData(10100220, 3)] // shirt
    [InlineData(10200100, 5)] // pants
    [InlineData(10200000, 4)] // skirt
    [InlineData(10400030, 7)] // socks
    [InlineData(10500070, 8)] // shoes
    [InlineData(10600000, 9)] // bra
    public void ToItemBaseListData_maps_wardrobe_category_by_item_type(int itemId, uint expectedCategory)
    {
        var item = new Item { Id = itemId, Socket = 0, Name = "test" };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedCategory, data.Category);
    }

    [Fact]
    public void ResolveBodyspot_uses_stored_socket_for_accessories()
    {
        var item = new Item { Id = 10800000, Socket = 11, Name = "ふちなしメガネ" };
        Assert.Equal(11u, ItemEntityMapper.ResolveBodyspot(item));
    }

    [Theory]
    [InlineData(10200100, 32, 0x40u)] // pants
    [InlineData(10200000, 16, 0x40u)] // skirt
    public void ToItemBaseListData_sets_modesty_flag_on_outer_lower_body_without_underwear_socket(
        int itemId,
        uint expectedSocket,
        uint expectedFlags
    )
    {
        var name = expectedSocket == 32u ? "(男性用アイテム)" : "テストスカート";
        var item = new Item { Id = itemId, Socket = 0, Name = name };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedSocket, data.Socket1);
        Assert.Equal(0u, data.Socket2);
        Assert.Equal(expectedFlags, data.Flags);
    }

    [Fact]
    public void ToItemBaseListData_does_not_set_modesty_flag_on_shirt()
    {
        var item = new Item { Id = 10100220, Socket = 0, Name = "テストシャツ" };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(0u, data.Flags);
    }
}
