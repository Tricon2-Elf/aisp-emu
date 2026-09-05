using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;

namespace aisp.Common.Tests;

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

    [Theory]
    [InlineData(10100320, "女性用シャツ紫色", 8)]
    [InlineData(10100340, "外神田ショップエプロン", 2)]
    [InlineData(10100341, "調理部のエプロン(ひよこ)", 2)]
    [InlineData(10100310, "黒コート♂", 2)]
    [InlineData(10100260, "風見学園本校・女子ジャケット♀", 4)]
    public void ResolveBodyspot_splits_101_prefix_by_wiki_upper_layer(
        int itemId,
        string name,
        uint expected
    )
    {
        var item = new Item
        {
            Id = itemId,
            Socket = 8,
            Name = name,
        };
        Assert.Equal(expected, ItemEntityMapper.ResolveBodyspot(item));
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
    public void ResolveEquipSocket_keeps_114_wings_backpacks_and_tails_on_distinct_cells()
    {
        var backpack = new CharacterEquipSlot(
            (byte)CharacterEquipmentSlotIndex.LeftShoulderBag,
            11400020
        );
        var wings = new CharacterEquipSlot((byte)CharacterEquipmentSlotIndex.Wings, 11400001);
        var tail = new CharacterEquipSlot((byte)CharacterEquipmentSlotIndex.Tail, 11400070);

        Assert.Equal(
            (uint)WardrobeSocketBit.LeftShoulderBag,
            ItemEntityMapper.ResolveEquipSocket(backpack)
        );
        Assert.Equal((uint)WardrobeSocketBit.Wings, ItemEntityMapper.ResolveEquipSocket(wings));
        Assert.Equal((uint)WardrobeSocketBit.Tail, ItemEntityMapper.ResolveEquipSocket(tail));
        Assert.Equal(1u << 28, ItemEntityMapper.ResolveEquipSocket(tail));
        Assert.NotEqual(1u << 25, ItemEntityMapper.ResolveEquipSocket(tail));
        Assert.NotEqual(
            ItemEntityMapper.ResolveEquipSocket(backpack),
            ItemEntityMapper.ResolveEquipSocket(wings)
        );
        Assert.NotEqual(
            ItemEntityMapper.ResolveEquipSocket(wings),
            ItemEntityMapper.ResolveEquipSocket(tail)
        );
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
    [InlineData(10000050, 100)]
    [InlineData(10800000, 108)]
    [InlineData(10900000, 109)]
    [InlineData(11200000, 112)]
    [InlineData(11400020, 114)]
    [InlineData(11600060, 116)]
    [InlineData(11700020, 117)]
    [InlineData(11800030, 118)]
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
    [InlineData(10800000, 11)] // accessory
    [InlineData(11400020, 11)] // backpack / wings
    [InlineData(11600060, 11)] // necklace
    [InlineData(11700020, 11)] // head accessory (hat cell)
    [InlineData(11800030, 11)] // mask (hat cell)
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
        Assert.Equal((uint)WardrobeSocketBit.Glasses, ItemEntityMapper.ResolveBodyspot(item));
    }

    [Theory]
    [InlineData(10800000, 11, (uint)WardrobeSocketBit.Glasses)]
    [InlineData(10800080, 15, (uint)WardrobeSocketBit.LeftEarring)]
    [InlineData(10800150, 16, (uint)WardrobeSocketBit.RightEarring)]
    [InlineData(10899999, 23, (uint)WardrobeSocketBit.Bracelet)]
    [InlineData(10900000, 51, (uint)WardrobeSocketBit.Wig)]
    [InlineData(10930050, 11, (uint)WardrobeSocketBit.Wig)]
    [InlineData(11600060, 12, (uint)WardrobeSocketBit.Necklace)]
    [InlineData(11600010, 0, (uint)WardrobeSocketBit.Necklace)]
    [InlineData(11700020, 14, (uint)WardrobeSocketBit.Head)]
    [InlineData(11800030, 11, (uint)WardrobeSocketBit.Head)]
    [InlineData(11200000, 0, (uint)WardrobeSocketBit.RightHandbag)]
    public void ResolveBodyspot_maps_accessory_seed_ids_to_one_hot_bits(
        int itemId,
        int storedSocket,
        uint expectedBit
    )
    {
        const uint clothingMask = 1 | 2 | 4 | 8 | 16 | 32 | 64 | 128 | 256 | 512 | 1024 | 2048;
        var bit = ItemEntityMapper.ResolveBodyspot(itemId, storedSocket);
        Assert.Equal(expectedBit, bit);
        // 117/118 share the hat/head clothing bit so they replace a worn hat.
        Assert.Equal(0u, bit & (clothingMask & ~expectedBit));
        Assert.Equal(0u, bit & (1u << 26));
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

    [Theory]
    [InlineData(11400020, 26, (uint)WardrobeSocketBit.LeftShoulderBag)]
    [InlineData(11400001, 26, (uint)WardrobeSocketBit.Wings)]
    [InlineData(11400150, 0, (uint)WardrobeSocketBit.Wings)]
    [InlineData(11400070, 27, (uint)WardrobeSocketBit.Tail)]
    [InlineData(11400070, 26, (uint)WardrobeSocketBit.Tail)]
    public void ResolveBodyspot_maps_114_backpacks_wings_and_tails(
        int itemId,
        int storedSocket,
        uint expectedBit
    )
    {
        Assert.Equal(expectedBit, ItemEntityMapper.ResolveBodyspot(itemId, storedSocket));
    }

    [Fact]
    public void ToItemBaseListData_keeps_114_backpacks_as_accessories_even_when_persisted_as_furniture()
    {
        var item = new Item
        {
            Id = 11400020,
            Socket = 26,
            Name = "スクールリュック",
            CatalogCategory = (int)WardrobeCategoryId.FurnitureFloor,
            Furniture = new Furniture
            {
                ItemId = 11400020,
                PlacementFlags = FurniturePlacementFlags.Floor,
                Type = 0,
            },
        };

        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal((uint)WardrobeCategoryId.Accessory, data.Category);
        Assert.Equal((uint)WardrobeSocketBit.LeftShoulderBag, data.Socket1);
        Assert.Equal(114u, data.PlacementTypeId);
        Assert.Equal(
            (uint)WardrobeCategoryId.Accessory,
            ItemEntityMapper.ResolveInventoryTabCategory(item)
        );
    }
}
