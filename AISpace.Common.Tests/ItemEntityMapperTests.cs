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
    [InlineData(10000010, 1)] // hat
    public void ResolveBodyspot_maps_clothing_to_client_slot_dockets(int itemId, uint expected)
    {
        Assert.Equal(expected, ItemEntityMapper.ResolveBodyspot(itemId));
    }

    [Fact]
    public void ResolveBodyspot_ignores_stored_socket_for_clothing()
    {
        var item = new Item
        {
            Id = 10500070,
            Socket = 16,
            Name = "テスト靴",
        };
        Assert.Equal(512u, ItemEntityMapper.ResolveBodyspot(item));
    }

    [Theory]
    [InlineData(10100220)] // shirt
    [InlineData(10200100)] // pants
    [InlineData(10400030)] // socks
    [InlineData(10500070)] // shoes
    public void ResolveEquipSocket_sends_zero_for_clothing_so_client_uses_catalog_bodyspot(
        uint itemId
    )
    {
        var slot = new CharacterEquipSlot(0, itemId);
        Assert.Equal(0u, ItemEntityMapper.ResolveEquipSocket(slot));
    }

    [Fact]
    public void ToItemBaseListData_sets_dual_shoe_catalog_sockets()
    {
        var item = new Item
        {
            Id = 10500070,
            Socket = 0,
            Name = "テスト靴",
        };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(512u, data.Socket1);
        Assert.Equal(256u, data.Socket2);
        Assert.Equal(8u, data.Category);
    }

    [Fact]
    public void ToItemBaseListData_sets_socket2_zero_for_single_slot_clothing()
    {
        var item = new Item
        {
            Id = 10100220,
            Socket = 0,
            Name = "テストシャツ",
        };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(8u, data.Socket1);
        Assert.Equal(0u, data.Socket2);
    }

    [Fact]
    public void ToItemBaseListData_uses_runtime_head_bodyspot_for_hats()
    {
        var item = new Item
        {
            Id = 10000050,
            Socket = 10,
            Name = "保護帽",
        };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(1u, data.Socket1);
        Assert.Equal(0u, data.Socket2);
        Assert.Equal(0u, data.Category);
    }

    [Theory]
    [InlineData(10100220, 101)]
    [InlineData(10500070, 105)]
    [InlineData(10000050, 200)]
    [InlineData(10800000, 200)]
    public void ToItemBaseListData_sets_limit_map_key_for_wardrobe_equip_checks(
        int itemId,
        uint expectedKey
    )
    {
        var item = new Item
        {
            Id = itemId,
            Socket = 0,
            Name = "test",
        };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedKey, data.PlacementTypeId);
    }

    [Theory]
    [InlineData(10100220, 3)] // shirt
    [InlineData(10200100, 5)] // pants
    [InlineData(10200000, 4)] // skirt
    [InlineData(10400030, 7)] // socks
    [InlineData(10500070, 8)] // shoes
    [InlineData(10600000, 9)] // bra
    public void ToItemBaseListData_maps_wardrobe_category_by_item_type(
        int itemId,
        uint expectedCategory
    )
    {
        var item = new Item
        {
            Id = itemId,
            Socket = 0,
            Name = "test",
        };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedCategory, data.Category);
    }

    [Fact]
    public void ResolveBodyspot_uses_stored_socket_for_accessories()
    {
        var item = new Item
        {
            Id = 10800000,
            Socket = 11,
            Name = "ふちなしメガネ",
        };
        Assert.Equal(11u, ItemEntityMapper.ResolveBodyspot(item));
    }

    [Theory]
    [InlineData(10200100, 32u)] // pants
    [InlineData(10700020, 2048u)] // lower underwear
    public void ToItemBaseListData_keeps_lower_body_sockets_separate(
        int itemId,
        uint expectedSocket
    )
    {
        var item = new Item
        {
            Id = itemId,
            Socket = 0,
            Name = "test",
        };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedSocket, data.Socket1);
        Assert.Equal(0u, data.Socket2);
    }

    [Fact]
    public void ToItemBaseListData_sets_modesty_coverage_flags_for_upper_and_lower_clothing()
    {
        var top = new Item
        {
            Id = 10100220,
            Socket = 0,
            Name = "テストシャツ",
        };
        var bottom = new Item
        {
            Id = 10200100,
            Socket = 0,
            Name = "テストパンツ",
        };
        var bra = new Item
        {
            Id = 10600000,
            Socket = 0,
            Name = "テストブラ",
        };
        var underwear = new Item
        {
            Id = 10700020,
            Socket = 0,
            Name = "テスト下着",
        };
        var hat = new Item
        {
            Id = 10000050,
            Socket = 10,
            Name = "保護帽",
        };

        var topData = ItemEntityMapper.ToItemBaseListData(top);
        var bottomData = ItemEntityMapper.ToItemBaseListData(bottom);
        var braData = ItemEntityMapper.ToItemBaseListData(bra);
        var underwearData = ItemEntityMapper.ToItemBaseListData(underwear);
        var hatData = ItemEntityMapper.ToItemBaseListData(hat);

        Assert.True((topData.Flags & ItemFlags.PermitsUnderwearTop) != 0);
        Assert.True((bottomData.Flags & ItemFlags.PermitsUnderwearBottom) != 0);
        Assert.True((braData.Flags & ItemFlags.PermitsUnderwearTop) != 0);
        Assert.True((underwearData.Flags & ItemFlags.PermitsUnderwearBottom) != 0);
        Assert.Equal(ItemFlags.None, hatData.Flags);
    }

    [Theory]
    [InlineData(FurniturePlacementFlags.Floor, 12u)]
    [InlineData(FurniturePlacementFlags.Wall, 13u)]
    [InlineData(FurniturePlacementFlags.Ceiling, 14u)]
    public void ToItemBaseListData_maps_furniture_to_wardrobe_furniture_categories(
        FurniturePlacementFlags flags,
        uint expectedCategory
    )
    {
        var item = new Item
        {
            Id = 11_000_000,
            Socket = 0,
            Name = "テスト家具",
            Furniture = new Furniture
            {
                ItemId = 11_000_000,
                PlacementFlags = flags,
                Type = 0,
            },
        };

        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(expectedCategory, data.Category);
    }

    [Fact]
    public void ToItemBaseListData_furniture_without_row_defaults_to_floor_category()
    {
        var item = new Item
        {
            Id = 11_000_590,
            Socket = 0,
            Name = "テスト家具",
        };

        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(12u, data.Category);
    }
}
