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
}
