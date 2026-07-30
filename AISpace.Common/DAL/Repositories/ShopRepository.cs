using System.Globalization;
using System.Text.Json;
using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.DAL.Repositories;

public interface IShopRepository
{
    Task<IReadOnlyList<ShopItem>> GetEnabledItemsAsync(int shopId, CancellationToken ct = default);
    Task<ShopItem?> GetEnabledItemAsync(int shopId, int itemId, CancellationToken ct = default);
}

public sealed class ShopRepository(MainContext db) : IShopRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<ShopItem>> GetEnabledItemsAsync(
        int shopId,
        CancellationToken ct = default
    ) =>
        await db
            .ShopItems.AsNoTracking()
            .Where(x => x.ShopId == shopId && x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<ShopItem?> GetEnabledItemAsync(
        int shopId,
        int itemId,
        CancellationToken ct = default
    ) =>
        await db
            .ShopItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShopId == shopId && x.ItemId == itemId && x.IsEnabled, ct);

    public static async Task SeedShopsFromJsonAsync(
        MainContext db,
        string jsonPath,
        ILogger? logger = null,
        CancellationToken ct = default
    )
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Shop seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var root =
            JsonSerializer.Deserialize<ShopSeedRoot>(json, JsonOptions) ?? new ShopSeedRoot();

        if (root.Version <= 0)
            throw new InvalidDataException("Shop seed version must be greater than zero.");

        foreach (var shopRow in root.Shops)
        {
            if (string.IsNullOrWhiteSpace(shopRow.Code))
                throw new InvalidDataException("Shop code is required.");
            if (string.IsNullOrWhiteSpace(shopRow.DisplayName))
                throw new InvalidDataException($"Shop {shopRow.Code} displayName is required.");

            var shop = await db.Shops.SingleOrDefaultAsync(x => x.Code == shopRow.Code, ct);
            if (shop is null)
            {
                shop = new Shop { Code = shopRow.Code };
                db.Shops.Add(shop);
            }

            shop.DisplayName = shopRow.DisplayName;
            shop.BannerVisualId = shopRow.BannerVisualId;
            shop.IsEnabled = shopRow.IsEnabled ?? true;

            await db.SaveChangesAsync(ct);

            var expandedItems = new List<ShopItemSeedRow>(shopRow.Items);
            var catalogItemIds = new List<int>(shopRow.ItemIds);
            if (shopRow.IncludeAllFurniture)
            {
                catalogItemIds.AddRange(
                    await db
                        .Furniture.AsNoTracking()
                        .OrderBy(x => x.ItemId)
                        .Select(x => x.ItemId)
                        .ToListAsync(ct)
                );
            }

            if (catalogItemIds.Count > 0)
            {
                if (
                    shopRow.DefaultAiPrice is null
                    || shopRow.DefaultNicoPrice is null
                    || shopRow.DefaultAiPrice <= 0
                    || shopRow.DefaultNicoPrice <= 0
                )
                    throw new InvalidDataException(
                        $"Shop {shopRow.Code} using itemIds must define defaultAiPrice/defaultNicoPrice > 0."
                    );

                var seenItemIds = expandedItems.Select(x => x.ItemId).ToHashSet();
                for (var i = 0; i < catalogItemIds.Count; i++)
                {
                    var id = catalogItemIds[i];
                    if (!seenItemIds.Add(id))
                        continue;

                    expandedItems.Add(
                        new ShopItemSeedRow
                        {
                            ItemId = id,
                            AiPrice = shopRow.DefaultAiPrice.Value,
                            NicoPrice = shopRow.DefaultNicoPrice.Value,
                            SortOrder = (i + 1) * 10,
                            IsEnabled = shopRow.DefaultItemEnabled ?? true,
                        }
                    );
                }
            }

            foreach (var itemRow in expandedItems)
            {
                if (itemRow.ItemId <= 0)
                    throw new InvalidDataException(
                        $"Shop {shopRow.Code} has invalid item id {itemRow.ItemId}."
                    );
                if (itemRow.AiPrice <= 0 || itemRow.NicoPrice <= 0)
                    throw new InvalidDataException(
                        $"Shop {shopRow.Code} item {itemRow.ItemId} must have aiPrice and nicoPrice > 0."
                    );

                var itemExists = await db
                    .Items.AsNoTracking()
                    .AnyAsync(x => x.Id == itemRow.ItemId, ct);
                if (!itemExists)
                {
                    logger?.LogWarning(
                        "Skipping shop seed item {ItemId} for shop {ShopCode}; item does not exist in Items table.",
                        itemRow.ItemId,
                        shopRow.Code
                    );
                    continue;
                }

                var shopItem = await db.ShopItems.SingleOrDefaultAsync(
                    x => x.ShopId == shop.Id && x.ItemId == itemRow.ItemId,
                    ct
                );
                if (shopItem is null)
                {
                    shopItem = new ShopItem { ShopId = shop.Id, ItemId = itemRow.ItemId };
                    db.ShopItems.Add(shopItem);
                }

                shopItem.AiPrice = itemRow.AiPrice;
                shopItem.NicoPrice = itemRow.NicoPrice;
                shopItem.SortOrder = itemRow.SortOrder ?? 0;
                shopItem.IsEnabled = itemRow.IsEnabled ?? true;
            }

            foreach (var npcRow in shopRow.Npcs)
            {
                if (npcRow.DayPhase is < -1 or > 4)
                    throw new InvalidDataException(
                        $"NPC {npcRow.NpcObjectId} dayPhase must be -1 or 0..4."
                    );
                if (string.IsNullOrWhiteSpace(npcRow.Name))
                    throw new InvalidDataException($"NPC {npcRow.NpcObjectId} name is required.");

                var dateStartUtc = ParseUtc(
                    npcRow.DateStartUtc,
                    DateTime.UnixEpoch,
                    "dateStartUtc"
                );
                var dateEndUtc = ParseUtc(npcRow.DateEndUtc, DateTime.MaxValue, "dateEndUtc");
                if (dateStartUtc > dateEndUtc)
                    throw new InvalidDataException(
                        $"NPC {npcRow.NpcObjectId} dateStartUtc must be <= dateEndUtc."
                    );

                var npc = await db
                    .Npcs.Include(x => x.Equipment)
                    .SingleOrDefaultAsync(x => x.NpcObjectId == npcRow.NpcObjectId, ct);
                if (npc is null)
                {
                    npc = new Npc { NpcObjectId = npcRow.NpcObjectId };
                    db.Npcs.Add(npc);
                }

                npc.MapId = npcRow.MapId;
                npc.ChannelId = npcRow.ChannelId ?? -1;
                npc.DayPhase = npcRow.DayPhase ?? -1;
                npc.DateStartUtc = dateStartUtc;
                npc.DateEndUtc = dateEndUtc;
                npc.ModelId = npcRow.ModelId;
                npc.Name = npcRow.Name;
                npc.X = npcRow.X;
                npc.Y = npcRow.Y;
                npc.Z = npcRow.Z;
                npc.Rotation = npcRow.Rotation;
                npc.ShopId = shop.Id;
                npc.InteractionType = ParseInteractionType(npcRow.InteractionType);
                npc.IsEnabled = npcRow.IsEnabled ?? true;
                npc.SortOrder = npcRow.SortOrder ?? 0;

                var equipmentRows = npcRow.Equipment ?? [];
                var seenSlots = new HashSet<int>();
                foreach (var equipment in equipmentRows)
                {
                    if (!seenSlots.Add(equipment.SlotIndex))
                        throw new InvalidDataException(
                            $"NPC {npcRow.NpcObjectId} has duplicate equipment slotIndex {equipment.SlotIndex}."
                        );
                }

                await db.SaveChangesAsync(ct);

                var bySlot = npc.Equipment.ToDictionary(x => x.SlotIndex, x => x);
                foreach (var equipment in equipmentRows)
                {
                    if (!bySlot.TryGetValue(equipment.SlotIndex, out var existing))
                    {
                        existing = new NpcEquipment
                        {
                            NpcId = npc.Id,
                            SlotIndex = equipment.SlotIndex,
                        };
                        db.NpcEquipments.Add(existing);
                    }

                    existing.ItemId = equipment.ItemId;
                    existing.SortOrder = equipment.SortOrder ?? 0;
                }

                var incomingSlots = equipmentRows.Select(x => x.SlotIndex).ToHashSet();
                var staleRows = npc
                    .Equipment.Where(x => !incomingSlots.Contains(x.SlotIndex))
                    .ToList();
                if (staleRows.Count > 0)
                    db.NpcEquipments.RemoveRange(staleRows);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static NpcInteractionType ParseInteractionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NpcInteractionType.Shop;
        return Enum.TryParse<NpcInteractionType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : NpcInteractionType.Shop;
    }

    private static DateTime ParseUtc(string? value, DateTime fallback, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (
            !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed
            )
        )
            throw new InvalidDataException($"Invalid UTC timestamp for {fieldName}: '{value}'.");
        return parsed.UtcDateTime;
    }

    private sealed class ShopSeedRoot
    {
        public int Version { get; set; } = 1;
        public List<ShopSeedRow> Shops { get; set; } = [];
    }

    private sealed class ShopSeedRow
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long BannerVisualId { get; set; }
        public bool? IsEnabled { get; set; }
        public List<ShopItemSeedRow> Items { get; set; } = [];
        public List<int> ItemIds { get; set; } = [];
        public bool IncludeAllFurniture { get; set; }
        public long? DefaultAiPrice { get; set; }
        public long? DefaultNicoPrice { get; set; }
        public bool? DefaultItemEnabled { get; set; }
        public List<NpcSeedRow> Npcs { get; set; } = [];
    }

    private sealed class ShopItemSeedRow
    {
        public int ItemId { get; set; }
        public long AiPrice { get; set; }
        public long NicoPrice { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsEnabled { get; set; }
    }

    private sealed class NpcSeedRow
    {
        public long MapId { get; set; }
        public int? ChannelId { get; set; }
        public int? DayPhase { get; set; }
        public string? DateStartUtc { get; set; }
        public string? DateEndUtc { get; set; }
        public long NpcObjectId { get; set; }
        public long ModelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Rotation { get; set; }
        public string? InteractionType { get; set; }
        public bool? IsEnabled { get; set; }
        public int? SortOrder { get; set; }
        public List<NpcEquipmentSeedRow>? Equipment { get; set; }
    }

    private sealed class NpcEquipmentSeedRow
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int? SortOrder { get; set; }
    }
}
