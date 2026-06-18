using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;

namespace AISpace.Common.Tests;

public class ItemEntityMapperTests
{
    [Theory]
    [InlineData(10100220, 8)] // shirt
    [InlineData(10200100, 32)] // pants
    [InlineData(10200000, 16)] // skirt
    [InlineData(10400030, 128)] // socks
    [InlineData(10500070, 256)] // shoes
    [InlineData(10000010, 2)] // hat
    public void ResolveBodyspot_maps_clothing_to_client_slot_dockets(int itemId, uint expected)
    {
        Assert.Equal(expected, ItemEntityMapper.ResolveBodyspot(itemId));
    }

    [Fact]
    public void ResolveBodyspot_ignores_stored_socket_for_clothing()
    {
        var item = new Item { Id = 10500070, Socket = 16, Name = "テスト靴" };
        Assert.Equal(256u, ItemEntityMapper.ResolveBodyspot(item));
    }

    [Fact]
    public void ResolveBodyspot_uses_stored_socket_for_accessories()
    {
        var item = new Item { Id = 10800000, Socket = 11, Name = "ふちなしメガネ" };
        Assert.Equal(11u, ItemEntityMapper.ResolveBodyspot(item));
    }
}
