using System.Globalization;
using System.Text.Json;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.DAL.Repositories;

public interface INpcRepository
{
    Task<IReadOnlyList<Npc>> GetActiveByMapAsync(
        uint mapId,
        int channelId,
        CancellationToken ct = default
    );
    Task<Npc?> GetActiveByMapAndObjectIdAsync(
        uint mapId,
        int channelId,
        uint npcObjectId,
        CancellationToken ct = default
    );
    Task<Shop?> GetSingleActiveShopForMapAsync(
        uint mapId,
        int channelId,
        CancellationToken ct = default
    );
}

public sealed class NpcRepository(MainContext db) : INpcRepository
{
    public async Task<IReadOnlyList<Npc>> GetActiveByMapAsync(
        uint mapId,
        int channelId,
        CancellationToken ct = default
    )
    {
        var activePhase = (int)TimeZoneService.GetServerTime().Phase;
        var nowUtc = DateTime.UtcNow;
        long mapIdLong = mapId;
        return await db
            .Npcs.AsNoTracking()
            .Include(x => x.Equipment)
            .Where(x =>
                x.IsEnabled
                && x.MapId == mapIdLong
                && (x.ChannelId == -1 || x.ChannelId == channelId)
                && (x.DayPhase == -1 || x.DayPhase == activePhase)
                && x.DateStartUtc <= nowUtc
                && x.DateEndUtc >= nowUtc
            )
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<Npc?> GetActiveByMapAndObjectIdAsync(
        uint mapId,
        int channelId,
        uint npcObjectId,
        CancellationToken ct = default
    )
    {
        var activePhase = (int)TimeZoneService.GetServerTime().Phase;
        var nowUtc = DateTime.UtcNow;
        long mapIdLong = mapId;
        long npcObjectIdLong = npcObjectId;
        return await db
            .Npcs.AsNoTracking()
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(
                x =>
                    x.IsEnabled
                    && x.MapId == mapIdLong
                    && (x.ChannelId == -1 || x.ChannelId == channelId)
                    && x.NpcObjectId == npcObjectIdLong
                    && (x.DayPhase == -1 || x.DayPhase == activePhase)
                    && x.DateStartUtc <= nowUtc
                    && x.DateEndUtc >= nowUtc,
                ct
            );
    }

    public async Task<Shop?> GetSingleActiveShopForMapAsync(
        uint mapId,
        int channelId,
        CancellationToken ct = default
    )
    {
        var activePhase = (int)TimeZoneService.GetServerTime().Phase;
        var nowUtc = DateTime.UtcNow;
        long mapIdLong = mapId;
        var shopIds = await db
            .Npcs.AsNoTracking()
            .Where(x =>
                x.IsEnabled
                && x.MapId == mapIdLong
                && x.ShopId != null
                && (x.ChannelId == -1 || x.ChannelId == channelId)
                && (x.DayPhase == -1 || x.DayPhase == activePhase)
                && x.DateStartUtc <= nowUtc
                && x.DateEndUtc >= nowUtc
            )
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => x.ShopId!.Value)
            .Distinct()
            .Take(2)
            .ToListAsync(ct);

        if (shopIds.Count != 1)
            return null;

        var shopId = shopIds[0];
        return await db
            .Shops.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shopId && x.IsEnabled, ct);
    }

    public static async Task SeedFromJsonAsync(
        MainContext db,
        string jsonPath,
        ILogger? logger = null,
        CancellationToken ct = default
    )
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("NPC seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var root = JsonSerializer.Deserialize<NpcSeedRoot>(json, JsonOptions) ?? new NpcSeedRoot();

        if (root.Version <= 0)
            throw new InvalidDataException("NPC seed version must be greater than zero.");

        foreach (var npcRow in root.Npcs)
        {
            if (npcRow.DayPhase is < -1 or > 4)
                throw new InvalidDataException(
                    $"NPC {npcRow.NpcObjectId} dayPhase must be -1 or 0..4."
                );

            var dateStartUtc = ParseUtc(npcRow.DateStartUtc, DateTime.UnixEpoch, "dateStartUtc");
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
            npc.NamePlate = npcRow.NamePlate ?? Npc.DefaultNamePlate;
            npc.Name = npcRow.Name.Canonical;
            npc.X = npcRow.X;
            npc.Y = npcRow.Y;
            npc.Z = npcRow.Z;
            npc.Rotation = npcRow.Rotation;
            npc.ShopId = null;
            npc.InteractionType = ParseInteractionType(npcRow.InteractionType);
            npc.IsEnabled = npcRow.IsEnabled ?? true;
            npc.SortOrder = npcRow.SortOrder ?? 0;
            npc.EventKind = ParseEventKind(
                npcRow.EventKind,
                npcRow.ScriptedEventKey,
                npcRow.EventKey
            );
            npc.EventKey = ResolveEventKey(npcRow.EventKey, npcRow.ScriptedEventKey);

            var equipmentRows = npcRow.Equipment ?? [];
            var seenSlots = new HashSet<int>();
            foreach (var equipment in equipmentRows)
            {
                if (!seenSlots.Add(equipment.SlotIndex))
                    throw new InvalidDataException(
                        $"NPC {npcRow.NpcObjectId} has duplicate equipment slotIndex {equipment.SlotIndex}."
                    );

                var itemExists = await db
                    .Items.AsNoTracking()
                    .AnyAsync(x => x.Id == equipment.ItemId, ct);
                if (!itemExists)
                    logger?.LogWarning(
                        "NPC seed item {ItemId} for npcObjectId {NpcObjectId} does not exist in Items table.",
                        equipment.ItemId,
                        npcRow.NpcObjectId
                    );
            }

            await db.SaveChangesAsync(ct);

            var bySlot = npc.Equipment.ToDictionary(x => x.SlotIndex, x => x);
            foreach (var equipment in equipmentRows)
            {
                if (!bySlot.TryGetValue(equipment.SlotIndex, out var existing))
                {
                    existing = new NpcEquipment { NpcId = npc.Id, SlotIndex = equipment.SlotIndex };
                    db.NpcEquipments.Add(existing);
                }

                existing.ItemId = equipment.ItemId;
                existing.SortOrder = equipment.SortOrder ?? 0;
            }

            var incomingSlots = equipmentRows.Select(x => x.SlotIndex).ToHashSet();
            var staleRows = npc.Equipment.Where(x => !incomingSlots.Contains(x.SlotIndex)).ToList();
            if (staleRows.Count > 0)
                db.NpcEquipments.RemoveRange(staleRows);
        }

        await db.SaveChangesAsync(ct);
    }

    private static readonly JsonSerializerOptions JsonOptions = SeedJson.Options;

    private static NpcInteractionType ParseInteractionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NpcInteractionType.Decorative;
        if (string.Equals(value, "HomeIslandRegistration", StringComparison.OrdinalIgnoreCase))
            return NpcInteractionType.Decorative;
        return Enum.TryParse<NpcInteractionType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : NpcInteractionType.Decorative;
    }

    private static NpcEventKind ParseEventKind(
        string? eventKind,
        string? legacyScriptedEventKey,
        string? eventKey
    )
    {
        if (
            !string.IsNullOrWhiteSpace(eventKind)
            && Enum.TryParse<NpcEventKind>(eventKind, ignoreCase: true, out var parsed)
        )
            return parsed;

        var key = ResolveEventKey(eventKey, legacyScriptedEventKey);
        return string.IsNullOrWhiteSpace(key) ? NpcEventKind.None : NpcEventKind.ClientScript;
    }

    private static string? ResolveEventKey(string? eventKey, string? legacyScriptedEventKey)
    {
        if (!string.IsNullOrWhiteSpace(eventKey))
            return eventKey;
        return string.IsNullOrWhiteSpace(legacyScriptedEventKey) ? null : legacyScriptedEventKey;
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

    private sealed class NpcSeedRoot
    {
        public int Version { get; set; } = 1;
        public List<NpcSeedRow> Npcs { get; set; } = [];
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
        public uint? NamePlate { get; set; }
        public LocalisedString Name { get; set; } = new();
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Rotation { get; set; }
        public string? InteractionType { get; set; }
        public bool? IsEnabled { get; set; }
        public int? SortOrder { get; set; }
        public string? EventKind { get; set; }
        public string? EventKey { get; set; }
        public string? ScriptedEventKey { get; set; }
        public List<NpcEquipmentSeedRow>? Equipment { get; set; }
    }

    private sealed class NpcEquipmentSeedRow
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int? SortOrder { get; set; }
    }
}
