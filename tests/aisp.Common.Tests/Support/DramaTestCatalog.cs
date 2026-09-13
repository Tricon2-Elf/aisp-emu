using System.Text.Json;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;

namespace aisp.Common.Tests.Support;

internal static class DramaTestCatalog
{
    public static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, "seedData");

    public static async Task SeedDefinitionsAndShopsAsync(MainContext db, string directory)
    {
        var ct = TestContext.Current.CancellationToken;
        await DramaCatalog.SeedAsync(db, directory, ct);
        await ShopRepository.SeedShopsFromJsonAsync(
            db,
            Path.Combine(directory, "niconiCommonsShop.json"),
            ct: ct
        );
    }

    public static async Task SeedAsync(MainContext db)
    {
        var ct = TestContext.Current.CancellationToken;
        // Seed only ownership items here; unrelated wardrobe fixtures may already own other ids.
        using var figures = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(DirectoryPath, "dramaFigures.json"), ct)
        );
        using var audio = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(DirectoryPath, "dramaAudio.json"), ct)
        );
        using var baseItems = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(DirectoryPath, "baseItems.json"), ct)
        );
        var items = baseItems
            .RootElement.EnumerateArray()
            .ToDictionary(x => x.GetProperty("id").GetInt32());
        foreach (
            var row in figures
                .RootElement.GetProperty("figures")
                .EnumerateArray()
                .Concat(audio.RootElement.GetProperty("audio").EnumerateArray())
        )
        {
            if (!row.TryGetProperty("itemId", out var itemId))
                continue;
            var id = itemId.GetInt32();
            if (await db.Items.FindAsync([id], ct) == null)
                db.Items.Add(
                    new Item
                    {
                        Id = id,
                        Name = LocalisedTextSeeder.Read(items[id].GetProperty("name")).Canonical,
                        IconId = items[id].GetProperty("iconId").GetInt32(),
                    }
                );
        }
        await db.SaveChangesAsync(ct);
        await SeedDefinitionsAndShopsAsync(db, DirectoryPath);
    }
}
