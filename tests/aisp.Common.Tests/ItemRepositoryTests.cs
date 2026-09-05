using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class ItemRepositoryTests
{
    [Fact]
    public async Task EnsureSeedItemsPresentAsync_rewrites_stale_furniture_category_for_114_backpacks()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        var seedPath = Path.Combine(Path.GetTempPath(), $"aisp-items-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(
                seedPath,
                """
                [
                  {
                    "id": 11400020,
                    "socket": 26,
                    "name": { "ja": "スクールリュック" },
                    "iconId": 11400020
                  }
                ]
                """,
                TestContext.Current.CancellationToken
            );

            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 11400020,
                        Name = "スクールリュック",
                        Socket = 26,
                        IconId = 11400020,
                        CatalogCategory = (int)WardrobeCategoryId.FurnitureFloor,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                await ItemRepository.EnsureSeedItemsPresentAsync(
                    db,
                    seedPath,
                    TestContext.Current.CancellationToken
                );
            }

            await using (var db = new MainContext(options))
            {
                var item = await db.Items.SingleAsync(
                    x => x.Id == 11400020,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal((int)WardrobeCategoryId.Accessory, item.CatalogCategory);
            }
        }
        finally
        {
            await connection.DisposeAsync();
            if (File.Exists(seedPath))
                File.Delete(seedPath);
        }
    }
}
