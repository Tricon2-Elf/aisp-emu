using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class ItemRepositoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Seed_upgrade_fills_placeholders_and_preserves_custom_metadata(bool customized)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        var seedPath = Path.Combine(Path.GetTempPath(), $"aisp-items-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(
                seedPath,
                """
                [{ "id": 10109990, "socket": 4, "iconId": 10109990,
                   "name": { "ja": "新しいジャケット", "en": "New jacket" } }]
                """,
                TestContext.Current.CancellationToken
            );
            await using var db = new MainContext(options);
            db.Items.Add(
                new Item
                {
                    Id = 10109990,
                    Name = customized ? "Custom name" : "N/A",
                    Socket = customized ? 8 : 0,
                    CatalogCategory = customized ? 1 : 3,
                    IconId = 10109990,
                }
            );
            foreach (
                var language in new[]
                {
                    GameLanguage.Japanese,
                    GameLanguage.English,
                    GameLanguage.ChineseSimplified,
                }
            )
                db.LocalisedTexts.Add(
                    new LocalisedText
                    {
                        Key = L.Item.Name(10109990),
                        Language = language,
                        Value = customized ? "Custom translation" : "N/A",
                    }
                );
            db.LocalisedTexts.Add(
                new LocalisedText
                {
                    Key = L.Item.Description(10109990),
                    Language = GameLanguage.English,
                    Value = "N/A",
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await ItemRepository.EnsureSeedItemsPresentAsync(
                db,
                seedPath,
                TestContext.Current.CancellationToken
            );
            await ItemRepository.EnsureSeedItemsPresentAsync(
                db,
                seedPath,
                TestContext.Current.CancellationToken
            );
            db.ChangeTracker.Clear();
            var item = await db.Items.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(customized ? "Custom name" : "新しいジャケット", item.Name);
            Assert.Equal(customized ? 8 : 4, item.Socket);
            Assert.Equal(customized ? 1 : 3, item.CatalogCategory);
            var texts = await db.LocalisedTexts.ToListAsync(TestContext.Current.CancellationToken);
            string Name(GameLanguage language) =>
                texts
                    .Single(text => text.Key == L.Item.Name(item.Id) && text.Language == language)
                    .Value;
            Assert.Equal(
                customized ? "Custom translation" : "新しいジャケット",
                Name(GameLanguage.Japanese)
            );
            Assert.Equal(
                customized ? "Custom translation" : "New jacket",
                Name(GameLanguage.English)
            );
            Assert.Equal(
                customized ? "Custom translation" : "N/A",
                Name(GameLanguage.ChineseSimplified)
            );
            Assert.Equal(
                "N/A",
                texts.Single(text => text.Key == L.Item.Description(item.Id)).Value
            );
        }
        finally
        {
            await connection.DisposeAsync();
            File.Delete(seedPath);
        }
    }

    [Fact]
    public async Task EnsureSeedItemsPresentAsync_identifies_existing_cosplay_placeholders()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        var seedPath = Path.Combine(Path.GetTempPath(), $"aisp-items-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(
                seedPath,
                """
                [
                  { "id": 40100040, "socket": 8, "name": { "ja": "小恋とお揃いのトップス♀" }, "iconId": 40100040 },
                  { "id": 40200040, "socket": 16, "name": { "ja": "小恋とお揃いの黒スカート♀" }, "iconId": 40200040 },
                  { "id": 40500010, "socket": 512, "name": { "ja": "小恋とお揃いのストラップ付靴♀" }, "iconId": 40500010 },
                  { "id": 40100050, "socket": 0, "name": { "ja": "N/A" }, "iconId": 40100050 }
                ]
                """,
                TestContext.Current.CancellationToken
            );
            await using var db = new MainContext(options);
            foreach (var id in new[] { 40100040, 40200040, 40500010, 40100050 })
                db.Items.Add(
                    new Item
                    {
                        Id = id,
                        Name = "N/A",
                        Socket = 0,
                        IconId = id,
                        CatalogCategory = 12,
                    }
                );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await ItemRepository.EnsureSeedItemsPresentAsync(
                db,
                seedPath,
                TestContext.Current.CancellationToken
            );
            await ItemRepository.EnsureSeedItemsPresentAsync(
                db,
                seedPath,
                TestContext.Current.CancellationToken
            );
            db.ChangeTracker.Clear();
            var unidentified = await db.Items.SingleAsync(
                x => x.Id == 40100050,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("N/A", unidentified.Name);
            Assert.Equal(3, unidentified.CatalogCategory);

            foreach (
                var (id, socket, category) in new[]
                {
                    (40100040, 8, 3),
                    (40200040, 16, 4),
                    (40500010, 512, 8),
                }
            )
            {
                var item = await db.Items.SingleAsync(
                    x => x.Id == id,
                    TestContext.Current.CancellationToken
                );
                Assert.StartsWith("小恋とお揃いの", item.Name);
                Assert.Equal(socket, item.Socket);
                Assert.Equal(category, item.CatalogCategory);
            }
        }
        finally
        {
            await connection.DisposeAsync();
            File.Delete(seedPath);
        }
    }

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

    [Fact]
    public async Task EnsureSeedItemsPresentAsync_adds_missing_drama_figure_boxes()
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
                    "id": 14100000,
                    "socket": 0,
                    "name": { "ja": "長身+りりしいモデル" },
                    "iconId": 14100000
                  }
                ]
                """,
                TestContext.Current.CancellationToken
            );

            await using (var db = new MainContext(options))
            {
                await ItemRepository.EnsureSeedItemsPresentAsync(
                    db,
                    seedPath,
                    TestContext.Current.CancellationToken
                );
            }

            await using (var db = new MainContext(options))
            {
                var item = await db.Items.SingleAsync(
                    x => x.Id == 14100000,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal("長身+りりしいモデル", item.Name);
                Assert.Equal((int)WardrobeCategoryId.DramaFigure, item.CatalogCategory);
            }
        }
        finally
        {
            await connection.DisposeAsync();
            if (File.Exists(seedPath))
                File.Delete(seedPath);
        }
    }

    [Fact]
    public async Task EnsureSeedItemsPresentAsync_rewrites_stale_furniture_category_for_141_figure_boxes()
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
                    "id": 14100000,
                    "socket": 0,
                    "name": { "ja": "長身+りりしいモデル" },
                    "iconId": 14100000
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
                        Id = 14100000,
                        Name = "長身+りりしいモデル",
                        Socket = 0,
                        IconId = 14100000,
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
                    x => x.Id == 14100000,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal((int)WardrobeCategoryId.DramaFigure, item.CatalogCategory);
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
