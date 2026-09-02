using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.DAL.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Character?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Character> CreateAsync(
        string name,
        int userId,
        uint modelId,
        BloodType bloodType,
        DateTime birthday,
        int Gender,
        uint faceType,
        uint hairStyle,
        CancellationToken ct = default
    );
    Task<Character?> UpdateCurrentMapAsync(
        int characterId,
        uint mapId,
        CancellationToken ct = default
    );
    Task<Character?> UpdateCurrentLocationAsync(
        int characterId,
        uint mapId,
        int? roomId,
        CancellationToken ct = default
    );
    Task<Character?> UpdateHomeIslandAsync(
        int characterId,
        uint homeIslandId,
        CancellationToken ct = default
    );
    Task<Character?> CompleteHomeRegistrationAsync(
        int characterId,
        uint homeIslandId,
        CharadollPersonality personality,
        CancellationToken ct = default
    );
    Task TouchLastLoggedInAsync(int characterId, CancellationToken ct = default);
    Task AddInventoryAsync(
        int characterId,
        int itemId,
        int quantity,
        CancellationToken ct = default
    );
    Task EquipAsync(int characterId, byte slotIndex, int itemId, CancellationToken ct = default);
    Task UnequipAsync(int characterId, byte slotIndex, CancellationToken ct = default);
    Task RemoveInventoryAsync(
        int characterId,
        int itemId,
        int quantity,
        CancellationToken ct = default
    );
    Task<EquipReplaceResult> ReplaceEquipmentAsync(
        int characterId,
        IEnumerable<ItemEquipEntry> equips,
        CancellationToken ct = default
    );
}

public sealed class CharacterRepository(MainContext db, ILogger<CharacterRepository> _logger)
    : ICharacterRepository
{
    public async Task<Character?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db
            .Characters.Include(c => c.Inventory)
                .ThenInclude(ci => ci.Item)
            .Include(c => c.Equipment)
                .ThenInclude(ce => ce.Item)
            .SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Character?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await db
            .Characters.Include(c => c.Inventory)
                .ThenInclude(ci => ci.Item)
            .Include(c => c.Equipment)
                .ThenInclude(ce => ce.Item)
            .SingleOrDefaultAsync(c => c.Name == name, ct);

    public async Task<Character> CreateAsync(
        string name,
        int userId,
        uint modelId,
        BloodType bloodType,
        DateTime birthday,
        int Gender,
        uint faceType,
        uint hairStyle,
        CancellationToken ct = default
    )
    {
        var c = new Character
        {
            Name = name,
            UserId = userId,
            ModelId = modelId,
            BloodType = bloodType,
            Birthdate = birthday,
            Gender = Gender,
            FaceType = faceType,
            Hairstyle = hairStyle,
        };
        c.Rooms.Add(
            new Room
            {
                Name = "My Room",
                Stage = aisp.Network.MyRoomStage.SixTatami,
                IsDefault = true,
            }
        );
        db.Characters.Add(c);
        await db.SaveChangesAsync(ct);
        return c;
    }

    public Task<Character?> UpdateCurrentMapAsync(
        int characterId,
        uint mapId,
        CancellationToken ct = default
    ) => UpdateCurrentLocationAsync(characterId, mapId, null, ct);

    public async Task<Character?> UpdateCurrentLocationAsync(
        int characterId,
        uint mapId,
        int? roomId,
        CancellationToken ct = default
    )
    {
        var character = await db
            .Characters.Include(c => c.Inventory)
                .ThenInclude(ci => ci.Item)
            .Include(c => c.Equipment)
                .ThenInclude(ce => ce.Item)
            .SingleOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return null;

        character.CurrentMapId = mapId;
        character.CurrentRoomId = roomId;
        await db.SaveChangesAsync(ct);
        return character;
    }

    public async Task<Character?> UpdateHomeIslandAsync(
        int characterId,
        uint homeIslandId,
        CancellationToken ct = default
    )
    {
        var character = await db
            .Characters.Include(c => c.Inventory)
                .ThenInclude(ci => ci.Item)
            .Include(c => c.Equipment)
                .ThenInclude(ce => ce.Item)
            .SingleOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return null;

        character.HomeIslandId = homeIslandId;
        await db.SaveChangesAsync(ct);
        return character;
    }

    public async Task<Character?> CompleteHomeRegistrationAsync(
        int characterId,
        uint homeIslandId,
        CharadollPersonality personality,
        CancellationToken ct = default
    )
    {
        var character = await db
            .Characters.Include(c => c.Inventory)
                .ThenInclude(ci => ci.Item)
            .Include(c => c.Equipment)
                .ThenInclude(ce => ce.Item)
            .SingleOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return null;

        character.HomeIslandId = homeIslandId;
        character.CharadollPersonality = personality;
        await db.SaveChangesAsync(ct);
        return character;
    }

    public async Task TouchLastLoggedInAsync(int characterId, CancellationToken ct = default)
    {
        var character = await db.Characters.SingleOrDefaultAsync(c => c.Id == characterId, ct);
        if (character is null)
            return;

        character.LastLoggedInAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task AddInventoryAsync(
        int characterId,
        int itemId,
        int quantity,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var existing = await db.CharacterInventories.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.ItemId == itemId,
            ct
        );

        if (existing is null)
        {
            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = characterId,
                    ItemId = itemId,
                    Quantity = quantity,
                }
            );
        }
        else
        {
            existing.Quantity += quantity;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task EquipAsync(
        int characterId,
        byte slotIndex,
        int itemId,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation(
            "Equipping item {ItemId} to character {CharacterId} in slot {SlotIndex}",
            itemId,
            characterId,
            slotIndex
        );
        if (slotIndex > 29)
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "0..29 only");

        // Ensure the character has the item in inventory
        //var hasItem = await db.CharacterInventories
        //    .AnyAsync(x => x.CharacterId == characterId && x.ItemId == itemId, ct);
        //if (!hasItem)
        //    throw new InvalidOperationException("Character does not own this item.");

        // Upsert the equipment for this slot
        var existing = await db.CharacterEquipments.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.SlotIndex == slotIndex,
            ct
        );

        if (existing is null)
        {
            db.CharacterEquipments.Add(
                new CharacterEquipment
                {
                    CharacterId = characterId,
                    SlotIndex = slotIndex,
                    ItemId = itemId,
                }
            );
        }
        else
        {
            existing.ItemId = itemId;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task UnequipAsync(int characterId, byte slotIndex, CancellationToken ct = default)
    {
        var existing = await db.CharacterEquipments.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.SlotIndex == slotIndex,
            ct
        );

        if (existing is null)
            return;

        db.CharacterEquipments.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveInventoryAsync(
        int characterId,
        int itemId,
        int quantity,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var existing = await db.CharacterInventories.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.ItemId == itemId,
            ct
        );
        if (existing is null)
            return;

        var remainingQuantity = Math.Max(0, existing.Quantity - quantity);
        var placedQuantity =
            itemId < 0
                ? 0
                : await db.MyRoomFurniture.CountAsync(
                    x => x.Room.OwnerCharacterId == characterId && x.ItemId == itemId,
                    ct
                );
        if (remainingQuantity < placedQuantity)
            throw new InvalidOperationException(
                $"Cannot remove furniture item {itemId} from character {characterId}: {placedQuantity} owned copies are currently placed in MyRoom."
            );

        existing.Quantity = remainingQuantity;
        if (existing.Quantity == 0)
            db.CharacterInventories.Remove(existing);

        await db.SaveChangesAsync(ct);
    }

    public async Task<EquipReplaceResult> ReplaceEquipmentAsync(
        int characterId,
        IEnumerable<ItemEquipEntry> equips,
        CancellationToken ct = default
    )
    {
        var existing = await db
            .CharacterEquipments.Include(e => e.Item)
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(ct);

        var newBySlot = new Dictionary<byte, ItemEquipEntry>();
        foreach (var equip in equips)
        {
            if (equip.ItemId == 0)
                continue;

            if (
                !EquipSlotMapper.TryResolveSlotIndex(
                    equip.ItemId,
                    equip.SocketBit,
                    out var slotIndex
                )
            )
            {
                _logger.LogWarning(
                    "Skipping unmapped wardrobe equip item {ItemId} socket {Socket} for character {CharacterId}",
                    equip.ItemId,
                    equip.SocketBit,
                    characterId
                );
                continue;
            }

            newBySlot[slotIndex] = equip;
        }

        var removed = new List<EquippedItemChange>();
        var pendingAdds = new List<(byte SlotIndex, ItemEquipEntry Equip)>();

        foreach (var old in existing)
        {
            if (
                newBySlot.TryGetValue(old.SlotIndex, out var replacement)
                && replacement.ItemId == (uint)old.ItemId
            )
                continue;

            removed.Add(
                new EquippedItemChange(
                    old.ItemId,
                    old.Item?.Name,
                    ItemEntityMapper.ResolveBodyspot(
                        old.ItemId,
                        storedSocket: old.Item?.Socket ?? 0,
                        name: old.Item?.Name
                    )
                )
            );
        }

        foreach (var (slotIndex, equip) in newBySlot)
        {
            var old = existing.FirstOrDefault(e => e.SlotIndex == slotIndex);
            var equipItemId = (int)equip.ItemId;

            if (old is not null && old.ItemId == equipItemId)
                continue;

            pendingAdds.Add((slotIndex, equip));
        }

        var addedItemIds = pendingAdds.Select(x => (int)x.Equip.ItemId).Distinct().ToList();
        var addedItemsById = await db
            .Items.Where(i => addedItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var added = new List<EquippedItemChange>(pendingAdds.Count);
        foreach (var (_, equip) in pendingAdds)
        {
            var equipItemId = (int)equip.ItemId;
            addedItemsById.TryGetValue(equipItemId, out var item);
            // Treat incoming socket bits as advisory; derive canonical bodyspot from item metadata
            // so mis-categorized UI tabs cannot force wrong slots (e.g. hats showing as coat).
            var socket = ItemEntityMapper.ResolveBodyspot(
                equipItemId,
                storedSocket: item?.Socket ?? (int)equip.SocketBit,
                name: item?.Name
            );
            if (socket == 0)
                socket = equip.SocketBit;
            added.Add(new EquippedItemChange(equipItemId, item?.Name, socket));
        }

        var changedItemIds = removed
            .Select(x => x.ItemId)
            .Concat(added.Select(x => x.ItemId))
            .Distinct()
            .ToList();
        var inventoryByItemId = await db
            .CharacterInventories.Where(i =>
                i.CharacterId == characterId && changedItemIds.Contains(i.ItemId)
            )
            .ToDictionaryAsync(i => i.ItemId, ct);

        // Shared wardrobe: pieces may come from bag, from avatar slots removed in this replace,
        // or from any Charadoll currently wearing them.
        var roboEquipment = await db
            .RoboEquipment.Where(e => e.CharacterId == characterId && e.ItemId != 0)
            .ToListAsync(ct);

        var availableByItemId = inventoryByItemId.ToDictionary(x => x.Key, x => x.Value.Quantity);
        foreach (var change in removed)
            availableByItemId[change.ItemId] =
                availableByItemId.GetValueOrDefault(change.ItemId) + 1;
        foreach (var row in roboEquipment)
            availableByItemId[(int)row.ItemId] =
                availableByItemId.GetValueOrDefault((int)row.ItemId) + 1;

        var requiredByItemId = pendingAdds
            .GroupBy(x => (int)x.Equip.ItemId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (itemId, required) in requiredByItemId)
        {
            if (availableByItemId.GetValueOrDefault(itemId) < required)
                throw new InvalidOperationException(
                    $"Character {characterId} does not own required quantity of item {itemId}."
                );
        }

        foreach (var change in removed)
        {
            if (inventoryByItemId.TryGetValue(change.ItemId, out var existingInventory))
            {
                existingInventory.Quantity += 1;
            }
            else
            {
                existingInventory = new CharacterInventory
                {
                    CharacterId = characterId,
                    ItemId = change.ItemId,
                    Quantity = 1,
                };
                db.CharacterInventories.Add(existingInventory);
                inventoryByItemId[change.ItemId] = existingInventory;
            }
        }

        var updatedRoboIds = new HashSet<uint>();
        foreach (var (itemId, required) in requiredByItemId)
        {
            var stillNeeded = required;
            if (inventoryByItemId.TryGetValue(itemId, out var inventory))
            {
                var take = Math.Min(inventory.Quantity, stillNeeded);
                inventory.Quantity -= take;
                stillNeeded -= take;
                if (inventory.Quantity <= 0)
                {
                    db.CharacterInventories.Remove(inventory);
                    inventoryByItemId.Remove(itemId);
                }
            }

            while (stillNeeded > 0)
            {
                var roboRow = roboEquipment.FirstOrDefault(e => e.ItemId == (uint)itemId);
                if (roboRow is null)
                    throw new InvalidOperationException(
                        $"Character {characterId} does not own required quantity of item {itemId}."
                    );

                roboRow.ItemId = 0;
                roboRow.Socket = 0;
                updatedRoboIds.Add(roboRow.RoboId);
                roboEquipment.Remove(roboRow);
                stillNeeded--;
            }
        }

        if (existing.Count > 0)
            db.CharacterEquipments.RemoveRange(existing);

        foreach (var (slotIndex, equip) in newBySlot)
        {
            db.CharacterEquipments.Add(
                new CharacterEquipment
                {
                    CharacterId = characterId,
                    SlotIndex = slotIndex,
                    ItemId = (int)equip.ItemId,
                }
            );
        }

        await db.SaveChangesAsync(ct);
        var countsByItemId = await db
            .CharacterInventories.Where(i =>
                i.CharacterId == characterId && changedItemIds.Contains(i.ItemId)
            )
            .ToDictionaryAsync(i => i.ItemId, i => i.Quantity, ct);

        foreach (var itemId in changedItemIds)
        {
            if (!countsByItemId.ContainsKey(itemId))
                countsByItemId[itemId] = 0;
        }

        return new EquipReplaceResult(
            removed,
            added,
            countsByItemId,
            updatedRoboIds.Count > 0 ? updatedRoboIds.OrderBy(id => id).ToList() : null
        );
    }
}
