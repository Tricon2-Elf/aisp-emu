using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaShopBuyHandlerTests
{
    private const uint StarterMapId = 10990100;
    private const uint StarterNpcObjectId = 0x50000001;

    [Fact]
    public async Task HandleAsync_AiPointsPurchase_DeductsCurrency_AndAddsInventory()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 1;
            const int characterId = 9001;
            const uint itemId = 10100220;
            var user = CreateUserWithCharacter(userId, characterId, aiPoints: 200, nicoPoints: 999);

            await using (var seed = new MainContext(options))
            {
                seed.Users.Add(user);
                seed.Items.Add(new Item { Id = (int)itemId, Name = "Shop Item", Socket = 8 });
                await SeedShopDataAsync(seed, itemId);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                CharacterId = (uint)characterId,
                Character = user.Characters.Single(),
                MapId = StarterMapId,
            };
            var handler = new AreaShopBuyHandler(
                db,
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                new NpcRepository(db),
                new ShopRepository(db),
                NullLogger<AreaShopBuyHandler>.Instance
            );

            var payload = BuildShopBuyPayload(
                [
                    new ShopBuyRequestedItem(itemId, 0, 0, 0),
                    new ShopBuyRequestedItem(itemId, 0, 0, 0),
                ],
                ShopPriceType.AiPoints
            );

            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            var responsePacket = session.Sent.Single(p => p.Type == PacketType.ShopBuyResponse);
            var responseReader = new PacketReader(responsePacket.Payload);
            Assert.Equal(0u, responseReader.ReadUInt());
            Assert.Equal(100UL, responseReader.ReadULong());

            Assert.Contains(session.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.MoneyUpdatedNicopoint);

            await using var verify = new MainContext(options);
            var persistedUser = await verify.Users.SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);
            var inventory = await verify.CharacterInventories.SingleAsync(i => i.CharacterId == characterId && i.ItemId == (int)itemId, TestContext.Current.CancellationToken);
            Assert.Equal(100, persistedUser.AiPoints);
            Assert.Equal(999, persistedUser.NicoPoints);
            Assert.Equal(2, inventory.Quantity);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_NicoPointsPurchase_DeductsOnlyNicoCurrency()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 2;
            const int characterId = 9002;
            const uint itemId = 10100220;
            var user = CreateUserWithCharacter(userId, characterId, aiPoints: 500, nicoPoints: 90);

            await using (var seed = new MainContext(options))
            {
                seed.Users.Add(user);
                seed.Items.Add(new Item { Id = (int)itemId, Name = "Shop Item", Socket = 8 });
                await SeedShopDataAsync(seed, itemId);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                CharacterId = (uint)characterId,
                Character = user.Characters.Single(),
                MapId = StarterMapId,
            };
            var handler = new AreaShopBuyHandler(
                db,
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                new NpcRepository(db),
                new ShopRepository(db),
                NullLogger<AreaShopBuyHandler>.Instance
            );

            var payload = BuildShopBuyPayload([new ShopBuyRequestedItem(itemId, 0, 0, 0)], ShopPriceType.NicoPoints);
            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            var responsePacket = session.Sent.Single(p => p.Type == PacketType.ShopBuyResponse);
            var responseReader = new PacketReader(responsePacket.Payload);
            Assert.Equal(0u, responseReader.ReadUInt());
            Assert.Equal(40UL, responseReader.ReadULong());

            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);
            Assert.Contains(session.Sent, p => p.Type == PacketType.MoneyUpdatedNicopoint);

            await using var verify = new MainContext(options);
            var persistedUser = await verify.Users.SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);
            Assert.Equal(500, persistedUser.AiPoints);
            Assert.Equal(40, persistedUser.NicoPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_FailsWithoutCurrencyLossOrItems()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 3;
            const int characterId = 9003;
            const uint itemId = 10100220;
            var user = CreateUserWithCharacter(userId, characterId, aiPoints: 20, nicoPoints: 20);

            await using (var seed = new MainContext(options))
            {
                seed.Users.Add(user);
                seed.Items.Add(new Item { Id = (int)itemId, Name = "Shop Item", Socket = 8 });
                await SeedShopDataAsync(seed, itemId);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                CharacterId = (uint)characterId,
                Character = user.Characters.Single(),
                MapId = StarterMapId,
            };
            var handler = new AreaShopBuyHandler(
                db,
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                new NpcRepository(db),
                new ShopRepository(db),
                NullLogger<AreaShopBuyHandler>.Instance
            );

            var payload = BuildShopBuyPayload([new ShopBuyRequestedItem(itemId, 0, 0, 0)], ShopPriceType.AiPoints);
            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            var responsePacket = session.Sent.Single(p => p.Type == PacketType.ShopBuyResponse);
            var responseReader = new PacketReader(responsePacket.Payload);
            Assert.Equal(1u, responseReader.ReadUInt());
            Assert.Equal(20UL, responseReader.ReadULong());

            await using var verify = new MainContext(options);
            var persistedUser = await verify.Users.SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);
            var inventoryRows = await verify.CharacterInventories.Where(i => i.CharacterId == characterId).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(20, persistedUser.AiPoints);
            Assert.Equal(20, persistedUser.NicoPoints);
            Assert.Empty(inventoryRows);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_InvalidPriceType_FailsWithoutGrantingItems()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 4;
            const int characterId = 9004;
            const uint itemId = 10100220;
            var user = CreateUserWithCharacter(userId, characterId, aiPoints: 120, nicoPoints: 120);

            await using (var seed = new MainContext(options))
            {
                seed.Users.Add(user);
                seed.Items.Add(new Item { Id = (int)itemId, Name = "Shop Item", Socket = 8 });
                await SeedShopDataAsync(seed, itemId);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                CharacterId = (uint)characterId,
                Character = user.Characters.Single(),
                MapId = StarterMapId,
            };
            var handler = new AreaShopBuyHandler(
                db,
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                new NpcRepository(db),
                new ShopRepository(db),
                NullLogger<AreaShopBuyHandler>.Instance
            );

            var payload = BuildShopBuyPayload([new ShopBuyRequestedItem(itemId, 0, 0, 0)], (ShopPriceType)99);
            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            var responsePacket = session.Sent.Single(p => p.Type == PacketType.ShopBuyResponse);
            var responseReader = new PacketReader(responsePacket.Payload);
            Assert.Equal(1u, responseReader.ReadUInt());
            Assert.Equal(120UL, responseReader.ReadULong());

            await using var verify = new MainContext(options);
            var persistedUser = await verify.Users.SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);
            var inventoryRows = await verify.CharacterInventories.Where(i => i.CharacterId == characterId).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(120, persistedUser.AiPoints);
            Assert.Equal(120, persistedUser.NicoPoints);
            Assert.Empty(inventoryRows);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_UnknownItemsOnly_FailsWithoutGrantingFreeItems()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int userId = 5;
            const int characterId = 9005;
            var user = CreateUserWithCharacter(userId, characterId, aiPoints: 1000, nicoPoints: 1000);

            await using (var seed = new MainContext(options))
            {
                seed.Users.Add(user);
                seed.Items.Add(new Item { Id = 10100220, Name = "Shop Item", Socket = 8 });
                await SeedShopDataAsync(seed, 10100220);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var db = new MainContext(options);
            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = user.Id,
                CharacterId = (uint)characterId,
                Character = user.Characters.Single(),
                MapId = StarterMapId,
            };
            var handler = new AreaShopBuyHandler(
                db,
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
                new NpcRepository(db),
                new ShopRepository(db),
                NullLogger<AreaShopBuyHandler>.Instance
            );

            var payload = BuildShopBuyPayload([new ShopBuyRequestedItem(99999999, 0, 0, 0)], ShopPriceType.AiPoints);
            await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

            var responsePacket = session.Sent.Single(p => p.Type == PacketType.ShopBuyResponse);
            var responseReader = new PacketReader(responsePacket.Payload);
            Assert.Equal(1u, responseReader.ReadUInt());
            Assert.Equal(1000UL, responseReader.ReadULong());

            await using var verify = new MainContext(options);
            var persistedUser = await verify.Users.SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);
            var inventoryRows = await verify.CharacterInventories.Where(i => i.CharacterId == characterId).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1000, persistedUser.AiPoints);
            Assert.Equal(1000, persistedUser.NicoPoints);
            Assert.Empty(inventoryRows);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static byte[] BuildShopBuyPayload(IReadOnlyList<ShopBuyRequestedItem> items, ShopPriceType priceType)
    {
        var writer = new PacketWriter();
        writer.Write((uint)items.Count);
        foreach (var item in items)
        {
            writer.Write(item.ItemId);
            writer.Write(item.UnknownWord);
            writer.Write(item.Unknown1);
            writer.Write(item.Unknown2);
        }

        writer.Write((byte)priceType);
        return writer.ToBytes();
    }

    private static User CreateUserWithCharacter(int userId, int characterId, long aiPoints, long nicoPoints)
    {
        var user = new User
        {
            Id = userId,
            Username = $"shop-user-{userId}",
            AiPoints = aiPoints,
            NicoPoints = nicoPoints,
        };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = $"Shop Character {characterId}",
                UserId = userId,
                CurrentMapId = StarterMapId,
                ModelId = 100,
                Birthdate = new DateTime(2000, 1, 2),
                BloodType = BloodType.A,
                Gender = 1,
                FaceType = 1,
                Hairstyle = 2,
            }
        );
        return user;
    }

    private static async Task SeedShopDataAsync(MainContext db, uint itemId)
    {
        var shop = new Shop
        {
            Code = "test_shop",
            DisplayName = "Test Shop",
            BannerVisualId = 10110,
            IsEnabled = true,
        };
        db.Shops.Add(shop);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.ShopItems.Add(
            new ShopItem
            {
                ShopId = shop.Id,
                ItemId = (int)itemId,
                AiPrice = 50,
                NicoPrice = 50,
                SortOrder = 0,
                IsEnabled = true,
            }
        );
        db.Npcs.Add(
            new Npc
            {
                MapId = StarterMapId,
                ChannelId = -1,
                DayPhase = -1,
                DateStartUtc = DateTime.UnixEpoch,
                DateEndUtc = DateTime.MaxValue,
                NpcObjectId = StarterNpcObjectId,
                ModelId = 1001021,
                Name = "Test Shop NPC",
                X = -9000f,
                Y = 2f,
                Z = -17900f,
                Rotation = 90,
                ShopId = shop.Id,
                InteractionType = NpcInteractionType.Shop,
                IsEnabled = true,
                SortOrder = 0,
            }
        );
    }
}
