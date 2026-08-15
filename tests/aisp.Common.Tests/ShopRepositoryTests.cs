using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class ShopRepositoryTests
{
    [Fact]
    public async Task FurnitureShopSeed_AddsEveryFurnitureItemAndNpcToAllShoppingDistricts()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var seedDirectory = Path.Combine(AppContext.BaseDirectory, "seedData");
            var furnitureCatalogPath = Path.Combine(seedDirectory, "furniture.json");
            var furnitureShopPath = Path.Combine(seedDirectory, "furnitureShop.json");
            var ct = TestContext.Current.CancellationToken;

            await MyRoomRepository.EnsureFurnitureCatalogPresentAsync(db, furnitureCatalogPath, ct);
            await ShopRepository.SeedShopsFromJsonAsync(db, furnitureShopPath, ct: ct);
            await ShopRepository.SeedShopsFromJsonAsync(db, furnitureShopPath, ct: ct);

            var shop = await db.Shops.SingleAsync(x => x.Code == "furniture", ct);
            var furnitureItemIds = await db
                .Furniture.AsNoTracking()
                .OrderBy(x => x.ItemId)
                .Select(x => x.ItemId)
                .ToListAsync(ct);
            var shopItems = await db
                .ShopItems.AsNoTracking()
                .Where(x => x.ShopId == shop.Id)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            Assert.Equal(furnitureItemIds, shopItems.Select(x => x.ItemId).ToList());
            Assert.All(
                shopItems,
                item =>
                {
                    Assert.Equal(50, item.AiPrice);
                    Assert.Equal(50, item.NicoPrice);
                    Assert.True(item.IsEnabled);
                }
            );

            var npcs = await db
                .Npcs.AsNoTracking()
                .Where(x => x.ShopId == shop.Id)
                .OrderBy(x => x.MapId)
                .ToListAsync(ct);
            Assert.Equal(
                [10_010_200L, 10_020_200L, 10_030_200L],
                npcs.Select(x => x.MapId).ToArray()
            );
            Assert.Equal(
                [1_342_177_311L, 1_342_177_312L, 1_342_177_313L],
                npcs.Select(x => x.NpcObjectId).ToArray()
            );
            Assert.All(
                npcs,
                npc =>
                {
                    Assert.Equal(-3165f, npc.X);
                    Assert.Equal(0f, npc.Y);
                    Assert.Equal(-738f, npc.Z);
                    Assert.Equal(0, npc.Rotation);
                    Assert.True(npc.IsEnabled);
                }
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
