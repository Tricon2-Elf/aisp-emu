using System.Text.Json;
using aisp.Common.DAL;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Localisation;

public static class LocalisationCatalogSeeder
{
    public static async Task SeedFromDirectoryAsync(
        MainContext db,
        string seedDirectory,
        ILogger? logger = null,
        CancellationToken ct = default
    )
    {
        var rows = CollectFromDirectory(seedDirectory, logger);
        foreach (var warning in LocalisationSeedValidator.Validate(rows))
            logger?.LogWarning("{Warning}", warning);
        await LocalisedTextSeeder.UpsertMissingAsync(db, rows, ct);
    }

    public static List<(string Key, GameLanguage Language, string Value)> CollectFromDirectory(
        string seedDirectory,
        ILogger? logger = null
    )
    {
        var rows = new List<(string Key, GameLanguage Language, string Value)>();
        CollectItems(Path.Combine(seedDirectory, "baseItems.json"), rows, logger);
        CollectFurniture(Path.Combine(seedDirectory, "furniture.json"), rows, logger);
        CollectMaps(Path.Combine(seedDirectory, "maps.json"), rows, logger);
        CollectWorlds(Path.Combine(seedDirectory, "worlds.json"), rows, logger);
        CollectNpcs(Path.Combine(seedDirectory, "npcs.json"), rows, logger);
        CollectShops(Path.Combine(seedDirectory, "starterShop.json"), rows, logger);
        CollectShops(Path.Combine(seedDirectory, "furnitureShop.json"), rows, logger);
        CollectStandalone(Path.Combine(seedDirectory, "localisation.json"), rows, logger);
        return rows;
    }

    private static void CollectItems(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadArray(path, logger, out var array))
            return;
        foreach (var item in array)
        {
            if (
                !item.TryGetProperty("id", out var idProperty)
                || !idProperty.TryGetInt32(out var id)
            )
                continue;
            AddField(rows, L.Item.Name(id), item, "name");
            AddField(rows, L.Item.Description(id), item, "description");
            AddField(rows, L.Item.LimitDescription(id), item, "limitDesc", "limitDescription");
        }
    }

    private static void CollectFurniture(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadArray(path, logger, out var array))
            return;
        foreach (var item in array)
        {
            if (
                !item.TryGetProperty("itemId", out var idProperty)
                || !idProperty.TryGetInt32(out var id)
            )
                continue;
            AddField(rows, L.Item.Name(id), item, "name");
        }
    }

    private static void CollectMaps(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadArray(path, logger, out var array))
            return;
        foreach (var item in array)
        {
            if (
                !item.TryGetProperty("mapId", out var idProperty)
                || !idProperty.TryGetInt64(out var mapId)
            )
                continue;
            AddField(rows, L.Map.Name(mapId), item, "name");
            AddField(rows, L.Map.Island(mapId), item, "island");
        }
    }

    private static void CollectWorlds(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadArray(path, logger, out var array))
            return;
        foreach (var item in array)
        {
            var name = ReadLocalised(item, "name");
            var code = name.Canonical;
            if (string.IsNullOrWhiteSpace(code))
                continue;
            rows.AddRange(LocalisedTextSeeder.FromLocalised(L.World.Name(code), name));
            AddField(rows, L.World.Description(code), item, "description");
        }
    }

    private static void CollectNpcs(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadRoot(path, logger, out var root))
            return;
        if (!root.TryGetProperty("npcs", out var npcs) || npcs.ValueKind != JsonValueKind.Array)
            return;
        foreach (var npc in npcs.EnumerateArray())
            CollectNpc(npc, rows);
    }

    private static void CollectShops(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadRoot(path, logger, out var root))
            return;
        if (!root.TryGetProperty("shops", out var shops) || shops.ValueKind != JsonValueKind.Array)
            return;
        foreach (var shop in shops.EnumerateArray())
        {
            var code = shop.TryGetProperty("code", out var codeProperty)
                ? codeProperty.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(code))
                AddField(rows, L.Shop.DisplayName(code), shop, "displayName");
            if (!shop.TryGetProperty("npcs", out var npcs) || npcs.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var npc in npcs.EnumerateArray())
                CollectNpc(npc, rows);
        }
    }

    private static void CollectNpc(
        JsonElement npc,
        List<(string Key, GameLanguage Language, string Value)> rows
    )
    {
        if (
            !npc.TryGetProperty("npcObjectId", out var idProperty)
            || !idProperty.TryGetInt64(out var npcObjectId)
        )
            return;
        AddField(rows, L.Npc.Name(npcObjectId), npc, "name");
    }

    private static void CollectStandalone(
        string path,
        List<(string Key, GameLanguage Language, string Value)> rows,
        ILogger? logger
    )
    {
        if (!TryReadRoot(path, logger, out var root))
            return;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Object)
            {
                foreach (
                    var warning in LocalisationSeedValidator.UnsupportedLocaleTags(
                        property.Value,
                        property.Name
                    )
                )
                    logger?.LogWarning("{Warning}", warning);
                rows.AddRange(
                    LocalisedTextSeeder.FromLocalised(
                        property.Name,
                        LocalisedTextSeeder.Read(property.Value)
                    )
                );
            }
        }
    }

    private static void AddField(
        List<(string Key, GameLanguage Language, string Value)> rows,
        LocKey key,
        JsonElement element,
        params string[] names
    )
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var property))
                continue;
            rows.AddRange(
                LocalisedTextSeeder.FromLocalised(key, LocalisedTextSeeder.Read(property))
            );
            return;
        }
    }

    private static LocalisedString ReadLocalised(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property)
            ? LocalisedTextSeeder.Read(property)
            : new LocalisedString();

    private static bool TryReadArray(
        string path,
        ILogger? logger,
        out JsonElement.ArrayEnumerator array
    )
    {
        array = default;
        if (!TryReadRoot(path, logger, out var root) || root.ValueKind != JsonValueKind.Array)
            return false;
        array = root.EnumerateArray();
        return true;
    }

    private static bool TryReadRoot(string path, ILogger? logger, out JsonElement root)
    {
        root = default;
        if (!File.Exists(path))
            return false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            root = document.RootElement.Clone();
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to parse Localisation seed file {Path}", path);
            throw;
        }
    }
}
