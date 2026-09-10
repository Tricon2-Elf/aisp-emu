using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Tests.Support;
using aisp.Network;

namespace aisp.Common.Tests;

public class CharacterItemSyncTests
{
    [Fact]
    public async Task SendBootstrapAsync_SyncsInventoryAndEquippedWithSameSerialId()
    {
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var character = new Character
        {
            Id = 1,
            Equipment =
            [
                new CharacterEquipment
                {
                    CharacterId = 1,
                    SlotIndex = 0,
                    ItemId = 10100220,
                },
            ],
            Inventory =
            [
                new CharacterInventory
                {
                    CharacterId = 1,
                    ItemId = 10100220,
                    Quantity = 1,
                },
                new CharacterInventory
                {
                    CharacterId = 1,
                    ItemId = 10200100,
                    Quantity = 1,
                },
            ],
        };

        await CharacterItemSync.SendBootstrapAsync(
            session,
            character,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, session.Sent.Count(p => p.Type == PacketType.ItemCreateNotify));
        Assert.Single(session.Sent, p => p.Type == PacketType.ItemEquippedNotify);
        Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.ItemUpdateListNotify);
    }

    [Fact]
    public async Task SendInventoryBootstrapAsync_SendsCompletionAfterLargeInventory()
    {
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var character = new Character
        {
            Id = 1,
            Inventory = Enumerable
                .Range(1, 258)
                .Select(itemId => new CharacterInventory
                {
                    CharacterId = 1,
                    ItemId = itemId,
                    Quantity = 1,
                })
                .ToList(),
        };

        await CharacterItemSync.SendInventoryBootstrapAsync(
            session,
            character,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(259, session.Sent.Count);
        Assert.All(
            session.Sent.Take(258),
            packet => Assert.Equal(PacketType.ItemCreateNotify, packet.Type)
        );
        Assert.Equal(PacketType.ItemGetListResponse, session.Sent[^1].Type);
    }
}
