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

    [Fact]
    public void ResolveEquipSocket_sends_zero_for_shoes_so_client_uses_catalog_mesh()
    {
        var slot = new CharacterEquipSlot(3, 10500070);
        Assert.Equal(0u, ItemEntityMapper.ResolveEquipSocket(slot));
    }

    [Fact]
    public void ResolveEquipSocket_sends_bodyspot_for_other_clothing()
    {
        var slot = new CharacterEquipSlot(0, 10100220);
        Assert.Equal(8u, ItemEntityMapper.ResolveEquipSocket(slot));
    }

    [Fact]
    public void ToItemBaseListData_sets_dual_shoe_catalog_sockets()
    {
        var item = new Item { Id = 10500070, Socket = 0, Name = "テスト靴" };
        var data = ItemEntityMapper.ToItemBaseListData(item);
        Assert.Equal(512u, data.Socket1);
        Assert.Equal(256u, data.Socket2);
    }

    [Fact]
    public void ResolveBodyspot_uses_stored_socket_for_accessories()
    {
        var item = new Item { Id = 10800000, Socket = 11, Name = "ふちなしメガネ" };
        Assert.Equal(11u, ItemEntityMapper.ResolveBodyspot(item));
    }
}
