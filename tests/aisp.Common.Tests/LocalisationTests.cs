using System.Text.Json;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Common.Services;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class LocalisationTests
{
    [Fact]
    public void LocalisedString_DeserializesScalarAsJapanese()
    {
        var value = JsonSerializer.Deserialize<LocalisedString>("\"Rin\"", SeedJson.Options);
        Assert.NotNull(value);
        Assert.Equal("Rin", value.Ja);
        Assert.Equal("Rin", value.Canonical);
    }

    [Fact]
    public void LocalisedString_DeserializesBcp47Object()
    {
        var json = """{"ja":"真珠","en":"Shinju","zh-Hans":"真珠","zh-Hant":"真珠"}""";
        var value = JsonSerializer.Deserialize<LocalisedString>(json, SeedJson.Options);
        Assert.NotNull(value);
        Assert.Equal("真珠", value.Get(GameLanguage.Japanese));
        Assert.Equal("Shinju", value.Get(GameLanguage.English));
        Assert.Equal("真珠", value.Get(GameLanguage.ChineseSimplified));
    }

    [Fact]
    public async Task CatalogSeeder_InsertsMissingKeysWithoutOverwritingEdits()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var seedDir = Path.Combine(AppContext.BaseDirectory, "seedData");
            await LocalisationCatalogSeeder.SeedFromDirectoryAsync(
                db,
                seedDir,
                ct: TestContext.Current.CancellationToken
            );
            var original = await db.LocalisedTexts.SingleAsync(
                row =>
                    row.Key == L.Script.Shinju.Help.Value && row.Language == GameLanguage.English,
                TestContext.Current.CancellationToken
            );
            original.Value = "Edited";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            await LocalisationCatalogSeeder.SeedFromDirectoryAsync(
                db,
                seedDir,
                ct: TestContext.Current.CancellationToken
            );
            var kept = await db.LocalisedTexts.SingleAsync(
                row =>
                    row.Key == L.Script.Shinju.Help.Value && row.Language == GameLanguage.English,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("Edited", kept.Value);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task TextLocaliser_UsesRequestedLanguageThenJapanese()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.LocalisedTexts.AddRange(
                    new LocalisedText
                    {
                        Key = "demo.both",
                        Language = GameLanguage.Japanese,
                        Value = "日本語",
                    },
                    new LocalisedText
                    {
                        Key = "demo.both",
                        Language = GameLanguage.English,
                        Value = "English",
                    },
                    new LocalisedText
                    {
                        Key = "demo.only_ja",
                        Language = GameLanguage.Japanese,
                        Value = "日本語のみ",
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var localiser = CreateLocaliser(options);
            await localiser.ReloadAsync(TestContext.Current.CancellationToken);

            Assert.Equal("English", localiser.Get(GameLanguage.English, new LocKey("demo.both")));
            Assert.Equal(
                "日本語",
                localiser.Get(GameLanguage.ChineseSimplified, new LocKey("demo.both"))
            );
            Assert.Equal(
                "日本語のみ",
                localiser.Get(GameLanguage.English, new LocKey("demo.only_ja"))
            );
            Assert.Equal(
                "demo.missing",
                localiser.Get(GameLanguage.Japanese, new LocKey("demo.missing"))
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void ItemMapper_UsesCanonicalNameNotLocalisedDisplayName()
    {
        var item = new Item
        {
            Id = 10200000,
            Name = "テストスカート",
            CatalogCategory = (int)WardrobeCategoryId.Skirt,
        };
        var data = ItemEntityMapper.ToItemBaseListData(item, localisedName: "Test Skirt");
        Assert.Equal("Test Skirt", data.Name);
        Assert.Equal(4u, data.Category);
    }

    [Fact]
    public void GameLanguages_ParseBcp47Values()
    {
        Assert.True(GameLanguages.TryParse("zh-Hans", out var simplified));
        Assert.Equal(GameLanguage.ChineseSimplified, simplified);
        Assert.True(GameLanguages.TryParse("zh-Hant", out var traditional));
        Assert.Equal(GameLanguage.ChineseTraditional, traditional);
        Assert.False(GameLanguages.TryParse("fr", out _));
    }

    [Fact]
    public async Task UserRepository_PersistsPreferredLanguage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User { Username = "locale-user" };
            user.SetPassword("password1");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var repo = new UserRepository(db);
            await repo.SetLanguageAsync(
                user.Id,
                GameLanguage.ChineseTraditional,
                TestContext.Current.CancellationToken
            );
            var reloaded = await repo.GetById(user.Id);
            Assert.Equal(GameLanguage.ChineseTraditional, reloaded!.Language);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void LocalisationSeedValidator_DetectsPlaceholderMismatchAndDuplicates()
    {
        var warnings = LocalisationSeedValidator.Validate([
            ("demo.greeting", GameLanguage.Japanese, "こんにちは{0}"),
            ("demo.greeting", GameLanguage.English, "Hello"),
            ("demo.greeting", GameLanguage.English, "Hello again"),
        ]);
        Assert.Contains(
            warnings,
            warning => warning.Contains("placeholders", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(
            warnings,
            warning => warning.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void StandaloneLocalisationJson_HasAllFourLocalesAndMatchingPlaceholders()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "seedData", "localisation.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var rows = new List<(string Key, GameLanguage Language, string Value)>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            Assert.Empty(
                LocalisationSeedValidator.UnsupportedLocaleTags(property.Value, property.Name)
            );
            var localised = LocalisedTextSeeder.Read(property.Value);
            Assert.False(string.IsNullOrWhiteSpace(localised.Ja), property.Name);
            Assert.False(string.IsNullOrWhiteSpace(localised.En), property.Name);
            Assert.False(string.IsNullOrWhiteSpace(localised.ZhHans), property.Name);
            Assert.False(string.IsNullOrWhiteSpace(localised.ZhHant), property.Name);
            rows.AddRange(LocalisedTextSeeder.FromLocalised(property.Name, localised));
        }

        Assert.Empty(LocalisationSeedValidator.Validate(rows));
    }

    [Fact]
    public async Task TextLocaliser_UsesSessionLanguage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.LocalisedTexts.Add(
                    new LocalisedText
                    {
                        Key = L.Script.Shinju.Help.Value,
                        Language = GameLanguage.English,
                        Value = "Can I help you?",
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var localiser = CreateLocaliser(options);
            await localiser.ReloadAsync(TestContext.Current.CancellationToken);
            var session = new CapturingPlayerSession { Language = GameLanguage.English };
            Assert.Equal("Can I help you?", localiser.Get(session, L.Script.Shinju.Help));
            session.Language = GameLanguage.Japanese;
            Assert.Equal(L.Script.Shinju.Help.Value, localiser.Get(session, L.Script.Shinju.Help));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ItemBaseListCache_BuildsDistinctPayloadsPerLanguage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 10100220,
                        Name = "シャツ",
                        CatalogCategory = 3,
                    }
                );
                db.LocalisedTexts.AddRange(
                    new LocalisedText
                    {
                        Key = L.Item.Name(10100220).Value,
                        Language = GameLanguage.Japanese,
                        Value = "シャツ",
                    },
                    new LocalisedText
                    {
                        Key = L.Item.Name(10100220).Value,
                        Language = GameLanguage.English,
                        Value = "Shirt",
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var localiser = CreateLocaliser(options);
            await localiser.ReloadAsync(TestContext.Current.CancellationToken);

            var services = new ServiceCollection();
            services.AddSingleton(options);
            services.AddScoped(_ => new MainContext(options));
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddSingleton<ITextLocaliser>(localiser);
            await using var provider = services.BuildServiceProvider();
            var itemCache = new ItemBaseListCache(
                provider.GetRequiredService<IServiceScopeFactory>()
            );
            await itemCache.WarmAsync(TestContext.Current.CancellationToken);

            var japanese = itemCache.GetResponsePayload(GameLanguage.Japanese).ToArray();
            var english = itemCache.GetResponsePayload(GameLanguage.English).ToArray();
            Assert.NotEmpty(japanese);
            Assert.NotEmpty(english);
            Assert.NotEqual(japanese, english);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static TextLocaliser CreateLocaliser(DbContextOptions<MainContext> options)
    {
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddScoped(_ => new MainContext(options));
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new TextLocaliser(scopeFactory, NullLogger<TextLocaliser>.Instance);
    }
}
