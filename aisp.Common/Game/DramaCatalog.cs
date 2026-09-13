using System.Text.Json;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Game;

/// <summary>Database-backed definitions, offers, and per-character registry projections.</summary>
public sealed class DramaCatalog(MainContext db, ITextLocaliser localiser)
{
    private string Text(IPlayerSession session, LocKey key, string fallback) =>
        localiser.TryGet(session.Language, key, out var text) ? text : fallback;

    private static HashSet<int> OwnedItems(Character? character) =>
        character?.Inventory.Where(x => x.Quantity > 0).Select(x => x.ItemId).ToHashSet() ?? [];

    public async Task<IReadOnlyList<UccAdvFigure>> FiguresAsync(
        Character? character,
        IPlayerSession session,
        CancellationToken ct
    )
    {
        var owned = OwnedItems(character);
        var definitions = await db
            .DramaFigures.AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Equipment)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        return definitions
            .Select(x => new UccAdvFigure
            {
                FigureId = (uint)x.Id,
                BoxId = (uint)x.BoxId,
                IconId = (uint)(x.Item?.IconId ?? 0),
                Name = x.Item is { } item
                    ? Text(session, L.Item.Name(item.Id), item.Name)
                    : Text(session, L.Drama.FigureName(x.Id), x.Name),
                Owned = x.AlwaysGranted || (x.ItemId is int itemId && owned.Contains(itemId)),
                Gender = (uint)x.Gender,
                People = (uint)x.People,
                PackageId = (uint)x.PackageId,
                Face = (uint)x.Face,
                Hairstyle = (uint)x.Hairstyle,
                ModelId = (uint)x.ModelId,
                Equipment = Enumerable
                    .Range(0, UccAdvFigure.EquipSlotCount)
                    .Select(slot =>
                        (uint)(x.Equipment.FirstOrDefault(e => e.SlotIndex == slot)?.ItemId ?? 0)
                    )
                    .ToArray(),
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<NiconiCommonsEntry>> CommonsAsync(
        Character? character,
        IPlayerSession session,
        CancellationToken ct
    )
    {
        var owned = OwnedItems(character);
        var figures = await FiguresAsync(character, session, ct);
        var boxes = figures.Where(x => x.Owned).Select(x => (int)x.BoxId).ToHashSet();
        var titles = await db
            .DramaFigureBoxes.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        var audio = await db
            .DramaAudio.AsNoTracking()
            .Include(x => x.Item)
            .Where(x => x.Kind == 2 || x.Kind == 3)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        return titles
            .Where(x => boxes.Contains(x.Id))
            .Select(x => new NiconiCommonsEntry
            {
                Id = (uint)x.Id,
                Name = Text(session, L.Drama.BoxName(x.Id), x.Name),
            })
            .Concat(
                audio.Select(x => new NiconiCommonsEntry
                {
                    Id = (uint)x.Id,
                    Type = (uint)x.Kind,
                    IconId = (uint)x.Item.IconId,
                    Name = Text(session, L.Item.Name(x.ItemId), x.Item.Name),
                    Available = owned.Contains(x.ItemId),
                })
            )
            .ToArray();
    }

    public async Task<IReadOnlyList<UccVoice>> VoicesAsync(
        Character? character,
        IPlayerSession session,
        CancellationToken ct
    )
    {
        var owned = OwnedItems(character);
        var audio = await db
            .DramaAudio.AsNoTracking()
            .Include(x => x.Item)
            .Where(x => x.Kind == 1)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
        return audio
            .Select(x => new UccVoice(
                (uint)x.Id,
                (uint)x.Item.IconId,
                Text(session, L.Item.Name(x.ItemId), x.Item.Name),
                owned.Contains(x.ItemId),
                localiser.GetOr(
                    session.Language,
                    L.Item.Description(x.ItemId),
                    L.Item.NoDescription
                )
            ))
            .ToArray();
    }

    public async Task RefreshOwnershipForItemsAsync(
        IPlayerSession session,
        int[] itemIds,
        CancellationToken ct
    )
    {
        if (itemIds.Length == 0)
            return;
        var figuresChanged = await db.DramaFigures.AnyAsync(
            x => x.ItemId != null && itemIds.Contains(x.ItemId.Value),
            ct
        );
        var audioKinds = await db
            .DramaAudio.Where(x => itemIds.Contains(x.ItemId))
            .Select(x => x.Kind)
            .Distinct()
            .ToListAsync(ct);
        if (!figuresChanged && audioKinds.Count == 0)
            return;

        // Bag notifications do not update the client's drama registries. Refresh them when ownership changes.
        var character = await db
            .Characters.AsNoTracking()
            .Include(x => x.Inventory)
            .SingleOrDefaultAsync(x => x.Id == session.CharacterId, ct);
        if (figuresChanged)
            await session.SendAsync(
                PacketType.UccAdvFigureBaseListResponse,
                new UccAdvFigureBaseListResponse(
                    0,
                    await FiguresAsync(character, session, ct)
                ).ToBytes(),
                ct
            );
        // Figure ownership also controls the available figure boxes in the Commons registry.
        if (figuresChanged || audioKinds.Any(x => x is 2 or 3))
            await session.SendAsync(
                PacketType.NiconiCommonsBaseListResponse,
                new NiconiCommonsBaseListResponse(
                    0,
                    await CommonsAsync(character, session, ct)
                ).ToBytes(),
                ct
            );
        if (audioKinds.Contains(1))
            await session.SendAsync(
                PacketType.UccVoiceBaseListResponse,
                new UccVoiceBaseListResponse(await VoicesAsync(character, session, ct)).ToBytes(),
                ct
            );
    }

    private IQueryable<ShopItem> EnabledShopItems(int shopId) =>
        db
            .ShopItems.AsNoTracking()
            .Where(x =>
                x.ShopId == shopId
                && x.Shop.IsEnabled
                && x.IsEnabled
                && x.AiPrice > 0
                && x.AiPrice <= uint.MaxValue
                && x.NicoPrice == 0
            );

    public async Task<NiconiCommonsShopItemNotify> SnapshotAsync(int shopId, CancellationToken ct)
    {
        var items = await EnabledShopItems(shopId).ToDictionaryAsync(x => x.ItemId, ct);
        var figures = await db
            .DramaFigures.AsNoTracking()
            .Where(x => x.ItemId != null)
            .Select(x => new { x.Id, ItemId = x.ItemId!.Value })
            .ToListAsync(ct);
        var audio = await db
            .DramaAudio.AsNoTracking()
            .Select(x => new
            {
                x.Kind,
                x.Id,
                x.ItemId,
            })
            .ToListAsync(ct);
        NiconiCommonsShopItemRecord[] Rows(IEnumerable<(int Kind, int Id, int ItemId)> registry) =>
            registry
                .Where(x => items.ContainsKey(x.ItemId))
                .OrderBy(x => items[x.ItemId].SortOrder)
                .ThenBy(x => x.Kind)
                .ThenBy(x => x.Id)
                .Select(x => new NiconiCommonsShopItemRecord(
                    (uint)x.Kind,
                    (uint)x.Id,
                    (uint)items[x.ItemId].AiPrice,
                    (uint)items[x.ItemId].NicoPrice
                ))
                .ToArray();
        return new(
            Rows(audio.Where(x => x.Kind is 2 or 3).Select(x => (x.Kind, x.Id, x.ItemId))),
            Rows(figures.Select(x => (0, x.Id, x.ItemId))),
            Rows(audio.Where(x => x.Kind == 1).Select(x => (x.Kind, x.Id, x.ItemId)))
        );
    }

    public sealed record Product(NiconiCommonsShopItemRecord Offer, int ItemId);

    public async Task<Product?> FindOfferAsync(int? shopId, int kind, uint id, CancellationToken ct)
    {
        if (shopId == null || id > int.MaxValue)
            return null;
        var itemId =
            kind == 0
                ? await db
                    .DramaFigures.Where(x => x.Id == (int)id)
                    .Select(x => x.ItemId)
                    .SingleOrDefaultAsync(ct)
                : await db
                    .DramaAudio.Where(x => x.Kind == kind && x.Id == (int)id)
                    .Select(x => (int?)x.ItemId)
                    .SingleOrDefaultAsync(ct);
        if (itemId is not int item)
            return null;
        var offer = await EnabledShopItems(shopId.Value)
            .SingleOrDefaultAsync(x => x.ItemId == item, ct);
        return offer is null
            ? null
            : new(new((uint)kind, id, (uint)offer.AiPrice, (uint)offer.NicoPrice), item);
    }

    /// <summary>Applies partial baseline updates. Omission preserves records and fields; supplied identities are inserted or updated.</summary>
    public static async Task SeedAsync(
        MainContext db,
        string directory,
        CancellationToken ct = default
    )
    {
        using var figures = await ReadAsync(Path.Combine(directory, "dramaFigures.json"), ct);
        using var audio = await ReadAsync(Path.Combine(directory, "dramaAudio.json"), ct);
        var translations = new List<(string Key, GameLanguage Language, string Value)>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var row in Rows(figures.RootElement, "boxes"))
            {
                var id = row.GetProperty("id").GetInt32();
                var box = await GetOrAddAsync<DramaFigureBox>(db, [id], ct);
                Apply(db, box, row, ["Id", "Name", "SortOrder"]);
                if (id <= 0 || string.IsNullOrWhiteSpace(box.Name))
                    throw new InvalidDataException($"Invalid figure box {id}.");
                CollectText(translations, row, "name", L.Drama.BoxName(id));
            }
            await db.SaveChangesAsync(ct);
            foreach (var row in Rows(figures.RootElement, "figures"))
            {
                var id = row.GetProperty("id").GetInt32();
                var figure = await GetOrAddAsync<DramaFigureDefinition>(db, [id], ct);
                Apply(
                    db,
                    figure,
                    row,
                    [
                        "Id",
                        "BoxId",
                        "ItemId",
                        "Gender",
                        "People",
                        "PackageId",
                        "Face",
                        "Hairstyle",
                        "ModelId",
                        "AlwaysGranted",
                        "SortOrder",
                    ]
                );
                if (figure.ItemId is null)
                {
                    Apply(db, figure, row, ["Name"]);
                    CollectText(translations, row, "name", L.Drama.FigureName(id));
                }
                foreach (var equipment in Rows(row, "equipment"))
                {
                    var slot = equipment.GetProperty("slotIndex").GetInt32();
                    if (slot is < 0 or >= 30)
                        throw new InvalidDataException(
                            $"Figure {id}: invalid equipment slot {slot}."
                        );
                    var existing = await db.DramaFigureEquipment.FindAsync([id, slot], ct);
                    if (equipment.TryGetProperty("remove", out var remove) && remove.GetBoolean())
                    {
                        if (existing != null)
                            db.Remove(existing);
                        continue;
                    }
                    var entry = await GetOrAddAsync<DramaFigureEquipment>(db, [id, slot], ct);
                    entry.FigureId = id;
                    Apply(db, entry, equipment, ["SlotIndex", "ItemId"]);
                    if (entry.ItemId <= 0)
                        throw new InvalidDataException(
                            $"Figure {id}: slot {slot} requires an item id."
                        );
                }
                if (
                    id is <= 0 or > ushort.MaxValue
                    || figure.Gender is < 1 or > 2
                    || figure.People is < 1 or > 3
                    || figure.ModelId <= 0
                    || (figure.ItemId is null && string.IsNullOrWhiteSpace(figure.Name))
                )
                    throw new InvalidDataException($"Invalid figure definition {id}.");
                if (figure.Face < 0 || figure.PackageId < 0 || figure.Hairstyle < 0)
                    throw new InvalidDataException($"Figure {id}: visual ids cannot be negative.");
                if (!await db.DramaFigureBoxes.AnyAsync(x => x.Id == figure.BoxId, ct))
                    throw new InvalidDataException($"Figure {id}: unknown box {figure.BoxId}.");
                if (!figure.AlwaysGranted && figure.ItemId is null)
                    throw new InvalidDataException($"Figure {id} requires an ownership item.");
                if (figure.ItemId is int itemId)
                    await RequireItemAsync(db, itemId, ct);
            }
            foreach (var row in Rows(audio.RootElement, "audio"))
            {
                var kind = row.GetProperty("kind").GetInt32();
                var id = row.GetProperty("id").GetInt32();
                if (kind is < 1 or > 3 || id <= 0)
                    throw new InvalidDataException("Invalid audio identity.");
                var entry = await GetOrAddAsync<DramaAudioDefinition>(db, [kind, id], ct);
                Apply(db, entry, row, ["Kind", "Id", "ItemId", "SortOrder"]);
                await RequireItemAsync(db, entry.ItemId, ct);
            }
            await db.SaveChangesAsync(ct);
            if (
                await db.DramaFigures.CountAsync(ct)
                > UccAdvFigureBaseListResponse.MaximumEntryCount
            )
                throw new InvalidDataException(
                    "Drama figure catalog exceeds the client registry capacity."
                );
            if (
                await db.DramaAudio.CountAsync(x => x.Kind == 1, ct)
                > UccVoiceBaseListResponse.MaximumEntryCount
            )
                throw new InvalidDataException(
                    "Drama voice catalog exceeds the client registry capacity."
                );
            if (
                await db.DramaFigureBoxes.CountAsync(ct)
                    + await db.DramaAudio.CountAsync(x => x.Kind != 1, ct)
                > NiconiCommonsBaseListResponse.MaximumEntryCount
            )
                throw new InvalidDataException(
                    "Drama commons catalog exceeds the client registry capacity."
                );
            await LocalisedTextSeeder.UpsertMissingAsync(db, translations, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static async Task RequireItemAsync(MainContext db, int id, CancellationToken ct)
    {
        if (!await db.Items.AnyAsync(x => x.Id == id, ct))
            throw new InvalidDataException(
                $"Drama ownership item {id} is missing from the item catalog."
            );
    }

    private static async Task<T> GetOrAddAsync<T>(
        MainContext db,
        object[] keys,
        CancellationToken ct
    )
        where T : class, new()
    {
        var entry = await db.Set<T>().FindAsync(keys, ct);
        if (entry != null)
            return entry;
        entry = new T();
        var tracked = db.Entry(entry);
        var keyProperties = tracked.Metadata.FindPrimaryKey()!.Properties;
        for (var i = 0; i < keys.Length; i++)
            tracked.Property(keyProperties[i].Name).CurrentValue = keys[i];
        db.Add(entry);
        return entry;
    }

    private static void Apply<T>(MainContext db, T entry, JsonElement row, string[] fields)
        where T : class
    {
        foreach (var field in fields)
        {
            var jsonName = char.ToLowerInvariant(field[0]) + field[1..];
            if (!row.TryGetProperty(jsonName, out var value))
                continue;
            var property = db.Entry(entry).Property(field);
            property.CurrentValue = field is "Name" or "Description" or "DisplayName"
                ? LocalisedTextSeeder.Read(value).Canonical
                : JsonSerializer.Deserialize(
                    value.GetRawText(),
                    property.Metadata.ClrType,
                    SeedJson.Options
                );
        }
    }

    private static void CollectText(
        List<(string Key, GameLanguage Language, string Value)> translations,
        JsonElement row,
        string field,
        LocKey key
    )
    {
        if (row.TryGetProperty(field, out var value))
            translations.AddRange(
                LocalisedTextSeeder.FromLocalised(key.Value, LocalisedTextSeeder.Read(value))
            );
    }

    private static IEnumerable<JsonElement> Rows(JsonElement root, string field)
    {
        if (!root.TryGetProperty(field, out var rows))
            return [];
        var result = rows.EnumerateArray().ToArray();
        string Identity(JsonElement row) =>
            field switch
            {
                "equipment" => row.GetProperty("slotIndex").GetRawText(),
                "audio" => row.GetProperty("kind").GetRawText()
                    + "/"
                    + row.GetProperty("id").GetRawText(),
                _ => row.GetProperty("id").GetRawText(),
            };
        if (result.Select(Identity).Distinct().Count() != result.Length)
            throw new InvalidDataException($"Duplicate identity in {field}.");
        return result;
    }

    private static async Task<JsonDocument> ReadAsync(string path, CancellationToken ct) =>
        JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
}
