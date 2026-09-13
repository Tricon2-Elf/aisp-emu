using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class DramaCatalogSeederTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class UpdateFiles : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"drama-seed-{Guid.NewGuid():N}");

        public UpdateFiles(string figures = "{}", string audio = "{}", string shops = "{}")
        {
            Directory.CreateDirectory(Path);
            File.WriteAllText(System.IO.Path.Combine(Path, "dramaFigures.json"), figures);
            File.WriteAllText(System.IO.Path.Combine(Path, "dramaAudio.json"), audio);
            File.WriteAllText(System.IO.Path.Combine(Path, "niconiCommonsShop.json"), shops);
        }

        public void Dispose() => Directory.Delete(Path, true);
    }

    [Fact]
    public async Task Migrated_database_loads_shipped_catalog_and_links_the_shop_npc()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            "Data Source=:memory:"
        );
        await connection.OpenAsync(Ct);
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite(connection).Options;
        await using var db = new MainContext(options);
        await db.Database.MigrateAsync(Ct);
        await ItemRepository.SeedItemsIfEmptyAsync(
            db,
            System.IO.Path.Combine(DramaTestCatalog.DirectoryPath, "baseItems.json"),
            Ct
        );
        await DramaCatalog.SeedAsync(db, DramaTestCatalog.DirectoryPath, Ct);
        Assert.Empty(await db.Shops.ToListAsync(Ct));
        Assert.Empty(await db.Npcs.ToListAsync(Ct));
        await ShopRepository.SeedShopsFromJsonAsync(
            db,
            System.IO.Path.Combine(DramaTestCatalog.DirectoryPath, "niconiCommonsShop.json"),
            ct: Ct
        );
        var seededClerk = await db
            .Npcs.Include(x => x.Shop)
            .Include(x => x.Equipment)
            .SingleAsync(x => x.NpcObjectId == 1342177339, Ct);
        Assert.Equal("niconi-commons", seededClerk.Shop!.Code);
        Assert.Equal(8, seededClerk.Equipment.Count);
        await NpcRepository.SeedFromJsonAsync(
            db,
            System.IO.Path.Combine(DramaTestCatalog.DirectoryPath, "npcs.json"),
            ct: Ct
        );
        await LocalisationCatalogSeeder.SeedFromDirectoryAsync(
            db,
            DramaTestCatalog.DirectoryPath,
            ct: Ct
        );
        var npc = await db
            .Npcs.Include(x => x.Shop)
            .SingleAsync(x => x.NpcObjectId == 1342177339, Ct);
        Assert.Equal("niconi-commons", npc.Shop!.Code);
        Assert.Equal(NpcInteractionType.NiconiCommonsShop, npc.InteractionType);
        Assert.Equal(27, await db.DramaFigures.CountAsync(Ct));
        Assert.Equal(37, await db.ShopItems.CountAsync(Ct));
    }

    [Fact]
    public async Task Nested_npcs_inherit_their_enclosing_shop_and_survive_omission()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        using var initial = new UpdateFiles(
            shops: """
            {"shops":[
              {"code":"first","displayName":{"ja":"First"},"npcs":[{"npcObjectId":100,"name":{"ja":"Clerk A"},"interactionType":"NiconiCommonsShop"}]},
              {"code":"second","displayName":{"ja":"Second"},"npcs":[{"npcObjectId":200,"name":{"ja":"Clerk B"},"interactionType":"NiconiCommonsShop"}]}
            ]}
            """
        );
        await ShopRepository.SeedShopsFromJsonAsync(
            db,
            System.IO.Path.Combine(initial.Path, "niconiCommonsShop.json"),
            ct: Ct
        );
        await ShopRepository.SeedShopsFromJsonAsync(
            db,
            System.IO.Path.Combine(initial.Path, "niconiCommonsShop.json"),
            ct: Ct
        );
        using var update = new UpdateFiles(
            shops: """{"shops":[{"code":"first","displayName":{"ja":"Updated"}}]}"""
        );
        await ShopRepository.SeedShopsFromJsonAsync(
            db,
            System.IO.Path.Combine(update.Path, "niconiCommonsShop.json"),
            ct: Ct
        );
        await LocalisationCatalogSeeder.SeedFromDirectoryAsync(db, initial.Path, ct: Ct);
        var npcs = await db.Npcs.Include(x => x.Shop).OrderBy(x => x.NpcObjectId).ToListAsync(Ct);
        Assert.Equal(2, npcs.Count);
        Assert.Equal("first", npcs[0].Shop!.Code);
        Assert.Equal("second", npcs[1].Shop!.Code);
        Assert.All(
            npcs,
            npc => Assert.Equal(NpcInteractionType.NiconiCommonsShop, npc.InteractionType)
        );
        Assert.Equal(
            "Clerk A",
            (
                await db.LocalisedTexts.SingleAsync(
                    x => x.Key == L.Npc.Name(100).Value && x.Language == GameLanguage.Japanese,
                    Ct
                )
            ).Value
        );
    }

    [Fact]
    public async Task Registry_names_and_icons_use_items_while_standalone_text_remains_in_drama()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        var itemIds = new[] { 14100000, 172001001, 143010001 };
        foreach (var id in itemIds)
        {
            var item = (await db.Items.FindAsync([id], Ct))!;
            item.Name = $"Item {id}";
            item.IconId = 777;
        }
        await db.SaveChangesAsync(Ct);
        await DramaCatalog.SeedAsync(db, DramaTestCatalog.DirectoryPath, Ct);
        var localiser = new TestTextLocaliser(
            new()
            {
                [(L.Item.Name(14100000).Value, GameLanguage.English)] = "Translated figure",
                [(L.Item.Name(172001001).Value, GameLanguage.English)] = "Translated BGM",
                [(L.Item.Name(143010001).Value, GameLanguage.English)] = "Translated voice",
                [(L.Drama.FigureName(1000).Value, GameLanguage.English)] = "Licensed figure",
                [(L.Item.Description(143010001).Value, GameLanguage.English)] = "Voice description",
            }
        );
        var catalog = new DramaCatalog(db, localiser);
        var session = new CapturingPlayerSession { Language = GameLanguage.English };
        var figures = await catalog.FiguresAsync(null, session, Ct);
        Assert.Equal("Translated figure", figures.Single(x => x.FigureId == 257).Name);
        Assert.Equal(777u, figures.Single(x => x.FigureId == 257).IconId);
        Assert.Equal("Licensed figure", figures.Single(x => x.FigureId == 1000).Name);
        var bgm = (await catalog.CommonsAsync(null, session, Ct)).Single(x => x.Id == 32001001);
        Assert.Equal("Translated BGM", bgm.Name);
        Assert.Equal(777u, bgm.IconId);
        var voice = (await catalog.VoicesAsync(null, session, Ct)).Single(x => x.Id == 10001);
        Assert.Equal("Translated voice", voice.Name);
        Assert.Equal("Voice description", voice.Description);
        var seededVoices = await new DramaCatalog(db, TestTextLocaliser.English).VoicesAsync(
            null,
            session,
            Ct
        );
        Assert.Equal(
            "少女ａ・タイトルコール１",
            seededVoices.Single(x => x.Id == 10001).Description
        );
        Assert.Equal(777u, voice.IconId);
        Assert.DoesNotContain(
            await db.LocalisedTexts.ToListAsync(Ct),
            x => x.Key == L.Drama.FigureName(257).Value || x.Key == "drama.audio.2.32001001.name"
        );
        foreach (var id in itemIds)
            Assert.Equal($"Item {id}", (await db.Items.FindAsync([id], Ct))!.Name);
    }

    [Fact]
    public async Task Partial_updates_correct_definitions_and_prices_preserving_item_metadata_and_ownership()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await TestDb.SeedCharacterAsync(options, 9001, Ct);
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        var shopId = await db.Shops.Select(x => x.Id).SingleAsync(Ct);
        var originalName = (await db.Items.FindAsync([14100000], Ct))!.Name;
        db.CharacterInventories.Add(
            new CharacterInventory
            {
                CharacterId = 9001,
                ItemId = 14100000,
                Quantity = 1,
            }
        );
        db.DramaFigureBoxes.Add(new DramaFigureBox { Id = 9000, Name = "Runtime box" });
        db.DramaFigures.Add(
            new DramaFigureDefinition
            {
                Id = 60000,
                BoxId = 9000,
                Name = "Runtime figure",
                Gender = 1,
                People = 1,
                ModelId = 1001021,
                AlwaysGranted = true,
            }
        );
        db.DramaFigureEquipment.Add(
            new DramaFigureEquipment
            {
                FigureId = 257,
                SlotIndex = 29,
                ItemId = 10100220,
            }
        );
        await db.SaveChangesAsync(Ct);
        using var update = new UpdateFiles(
            figures: """
            {"figures":[{"id":257,"hairstyle":10920024,"equipment":[{"slotIndex":0,"itemId":10100000},{"slotIndex":1,"remove":true}]}]}
            """,
            shops: """
            {"shops":[{"code":"niconi-commons","displayName":{"ja":"ニコニ・コモンズショップ"},"items":[{"itemId":14100000,"aiPrice":1234}]}]}
            """
        );
        await DramaTestCatalog.SeedDefinitionsAndShopsAsync(db, update.Path);
        await DramaTestCatalog.SeedDefinitionsAndShopsAsync(db, update.Path);
        db.ChangeTracker.Clear();
        var figure = await db
            .DramaFigures.Include(x => x.Equipment)
            .SingleAsync(x => x.Id == 257, Ct);
        Assert.Empty(figure.Name);
        Assert.Equal(10920024, figure.Hairstyle);
        Assert.Equal(1001021, figure.ModelId);
        Assert.Equal(10100000, figure.Equipment.Single(x => x.SlotIndex == 0).ItemId);
        Assert.DoesNotContain(figure.Equipment, x => x.SlotIndex == 1);
        Assert.Contains(figure.Equipment, x => x.SlotIndex == 4); // omitted seed slot remains
        Assert.Contains(figure.Equipment, x => x.SlotIndex == 29);
        Assert.Equal(28, await db.DramaFigures.CountAsync(Ct));
        Assert.Equal("Runtime figure", (await db.DramaFigures.FindAsync([60000], Ct))!.Name);
        Assert.Equal(
            1234,
            (
                await db.ShopItems.SingleAsync(x => x.ShopId == shopId && x.ItemId == 14100000, Ct)
            ).AiPrice
        );
        Assert.Equal(37, await db.ShopItems.CountAsync(Ct));
        Assert.Equal(1, (await db.CharacterInventories.SingleAsync(Ct)).Quantity);
        Assert.Equal(originalName, (await db.Items.FindAsync([14100000], Ct))!.Name);
    }

    [Fact]
    public async Task Disabling_offers_preserves_owned_figure_audio_and_voice_registry_entries()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        using var update = new UpdateFiles(
            shops: """
            {"shops":[{"code":"niconi-commons","displayName":{"ja":"ニコニ・コモンズショップ"},"items":[
              {"itemId":14100000,"aiPrice":5000,"nicoPrice":0,"isEnabled":false},
              {"itemId":172001001,"aiPrice":5000,"nicoPrice":0,"isEnabled":false},
              {"itemId":143010001,"aiPrice":5000,"nicoPrice":0,"isEnabled":false}]}]}
            """
        );
        await DramaTestCatalog.SeedDefinitionsAndShopsAsync(db, update.Path);
        var catalog = new DramaCatalog(db, TestTextLocaliser.English);
        var character = new Character();
        foreach (var id in new[] { 14100000, 172001001, 143010001 })
            character.Inventory.Add(new CharacterInventory { ItemId = id, Quantity = 1 });
        var session = new CapturingPlayerSession();
        var shopId = await db.Shops.Select(x => x.Id).SingleAsync(Ct);
        Assert.Null(await catalog.FindOfferAsync(shopId, 0, 257, Ct));
        Assert.Null(await catalog.FindOfferAsync(shopId, 2, 32001001, Ct));
        Assert.Null(await catalog.FindOfferAsync(shopId, 1, 10001, Ct));
        Assert.Null(await catalog.FindOfferAsync(null, 0, 258, Ct));
        Assert.NotNull(await catalog.FindOfferAsync(shopId, 0, 258, Ct));
        Assert.Contains(
            await catalog.FiguresAsync(character, session, Ct),
            x => x.FigureId == 257 && x.Owned
        );
        Assert.Contains(
            await catalog.CommonsAsync(character, session, Ct),
            x => x.Id == 32001001 && x.Available
        );
        Assert.Contains(
            await catalog.VoicesAsync(character, session, Ct),
            x => x.Id == 10001 && x.Owned
        );
    }

    [Theory]
    [InlineData(0, 257, 14100000)]
    [InlineData(1, 10001, 143010001)]
    [InlineData(2, 32001001, 172001001)]
    [InlineData(3, 42001001, 182001001)]
    public async Task Registry_purchases_use_the_active_shops_inventory_item_price(
        int kind,
        uint registryId,
        int itemId
    )
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        var originalShop = await db.Shops.SingleAsync(Ct);
        var otherShop = new Shop { Code = "other", DisplayName = "Other" };
        db.Shops.Add(otherShop);
        await db.SaveChangesAsync(Ct);
        var item = new ShopItem
        {
            ShopId = otherShop.Id,
            ItemId = itemId,
            AiPrice = 123,
            NicoPrice = 0,
        };
        db.ShopItems.Add(item);
        await db.SaveChangesAsync(Ct);
        var catalog = new DramaCatalog(db, TestTextLocaliser.English);
        var product = await catalog.FindOfferAsync(otherShop.Id, kind, registryId, Ct);
        Assert.NotNull(product);
        Assert.Equal(itemId, product.ItemId);
        Assert.Equal(123u, product.Offer.AiPrice);
        Assert.NotEqual(
            123u,
            (await catalog.FindOfferAsync(originalShop.Id, kind, registryId, Ct))!.Offer.AiPrice
        );
        item.IsEnabled = false;
        await db.SaveChangesAsync(Ct);
        Assert.Null(await catalog.FindOfferAsync(otherShop.Id, kind, registryId, Ct));
        Assert.NotNull(await catalog.FindOfferAsync(originalShop.Id, kind, registryId, Ct));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 1)]
    [InlineData(4294967296, 0)]
    public async Task Commons_shop_rejects_unsupported_prices(long aiPrice, long nicoPrice)
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        using var update = new UpdateFiles(
            shops: $$"""
            {"shops":[{"code":"niconi-commons","displayName":"Commons","items":[{"itemId":14100000,"aiPrice":{{aiPrice}},"nicoPrice":{{nicoPrice}}}]}]}
            """
        );
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            ShopRepository.SeedShopsFromJsonAsync(
                db,
                System.IO.Path.Combine(update.Path, "niconiCommonsShop.json"),
                ct: Ct
            )
        );
        Assert.Equal(5000, (await db.ShopItems.SingleAsync(x => x.ItemId == 14100000, Ct)).AiPrice);
    }

    [Fact]
    public async Task Invalid_catalog_reference_rolls_back_definitions_and_translations()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        var original = (await db.DramaFigureBoxes.FindAsync([1], Ct))!.Name;
        using var update = new UpdateFiles(
            figures: """{"boxes":[{"id":1,"name":{"ja":"must roll back"}}],"figures":[{"id":258,"boxId":99999}]}"""
        );
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            DramaCatalog.SeedAsync(db, update.Path, Ct)
        );
        Assert.Equal(original, (await db.DramaFigureBoxes.FindAsync([1], Ct))!.Name);
        Assert.DoesNotContain(
            await db.LocalisedTexts.ToListAsync(Ct),
            x => x.Value == "must roll back"
        );
    }

    [Fact]
    public async Task Seed_updates_matching_existing_records_regardless_of_origin()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        db.DramaFigureBoxes.Add(
            new DramaFigureBox
            {
                Id = 1,
                Name = "Runtime",
                SortOrder = 42,
            }
        );
        db.Shops.Add(new Shop { Code = "existing-shop", DisplayName = "Runtime shop" });
        await db.SaveChangesAsync(Ct);
        using var update = new UpdateFiles(
            figures: """{"boxes":[{"id":1,"name":{"ja":"Seed"}}]}""",
            shops: """{"shops":[{"code":"existing-shop","displayName":{"ja":"Updated shop"}}]}"""
        );
        await DramaTestCatalog.SeedDefinitionsAndShopsAsync(db, update.Path);
        await DramaTestCatalog.SeedDefinitionsAndShopsAsync(db, update.Path);
        Assert.Equal("Seed", (await db.DramaFigureBoxes.FindAsync([1], Ct))!.Name);
        Assert.Equal(42, (await db.DramaFigureBoxes.FindAsync([1], Ct))!.SortOrder);
        Assert.Equal("Updated shop", (await db.Shops.SingleAsync(Ct)).DisplayName);
    }

    [Fact]
    public async Task Repeated_drama_seed_preserves_existing_item_metadata_and_legacy_inventory_ids()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await TestDb.SeedCharacterAsync(options, 9002, Ct);
        await using var db = new MainContext(options);
        db.Items.Add(
            new Item
            {
                Id = 172001001,
                Name = "Legacy",
                IconId = 1,
            }
        );
        db.CharacterInventories.Add(
            new CharacterInventory
            {
                CharacterId = 9002,
                ItemId = 172001001,
                Quantity = 1,
            }
        );
        await db.SaveChangesAsync(Ct);
        await DramaTestCatalog.SeedAsync(db);
        await DramaTestCatalog.SeedAsync(db);
        Assert.Equal(27, await db.DramaFigures.CountAsync(Ct));
        Assert.Equal(13, await db.DramaAudio.CountAsync(Ct));
        Assert.Equal(37, await db.ShopItems.CountAsync(Ct));
        Assert.Equal(1, (await db.Items.FindAsync([172001001], Ct))!.IconId);
        Assert.Equal("Legacy", (await db.Items.FindAsync([172001001], Ct))!.Name);
        Assert.Equal(172001001, (await db.CharacterInventories.SingleAsync(Ct)).ItemId);
        Assert.Equal(1, (await db.CharacterInventories.SingleAsync(Ct)).Quantity);
    }
}
