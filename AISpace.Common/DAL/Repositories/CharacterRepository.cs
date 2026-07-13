using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.DAL.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Character?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Character> CreateAsync(string name, int userId, uint modelId, BloodType bloodType, DateTime birthday, int Gender, uint faceType, uint hairStyle, CancellationToken ct = default);
    Task<Character?> UpdateCurrentMapAsync(int characterId, uint mapId, CancellationToken ct = default);
    Task<Character?> UpdateHomeIslandAsync(int characterId, uint homeIslandId, CancellationToken ct = default);
    Task<Character?> CompleteHomeRegistrationAsync(int characterId, uint homeIslandId, uint modelId, CancellationToken ct = default);
    Task AddInventoryAsync(int characterId, int itemId, int quantity, CancellationToken ct = default);
    Task EquipAsync(int characterId, byte slotIndex, int itemId, CancellationToken ct = default);
    Task UnequipAsync(int characterId, byte slotIndex, CancellationToken ct = default);
    Task RemoveInventoryAsync(int characterId, int itemId, int quantity, CancellationToken ct = default);
    Task<EquipReplaceResult> ReplaceEquipmentAsync(int characterId, IEnumerable<ItemEquipEntry> equips, CancellationToken ct = default);
}

public sealed class CharacterRepository(MainContext db, ILogger<CharacterRepository> _logger) : ICharacterRepository
{
    public async Task<Character?> GetByIdAsync(int id, CancellationToken ct = default) => await db.Characters.Include(c => c.Inventory).ThenInclude(ci => ci.Item).Include(c => c.Equipment).ThenInclude(ce => ce.Item).SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Character?> GetByNameAsync(string name, CancellationToken ct = default) => await db.Characters.Include(c => c.Inventory).ThenInclude(ci => ci.Item).Include(c => c.Equipment).ThenInclude(ce => ce.Item).SingleOrDefaultAsync(c => c.Name == name, ct);

    public async Task<Character> CreateAsync(string name, int userId, uint modelId, BloodType bloodType, DateTime birthday, int Gender, uint faceType, uint hairStyle, CancellationToken ct = default)
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
        db.Characters.Add(c);
        await db.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Character?> UpdateCurrentMapAsync(int characterId, uint mapId, CancellationToken ct = default)
    {
        var character = await db.Characters.Include(c => c.Inventory).ThenInclude(ci => ci.Item).Include(c => c.Equipment).ThenInclude(ce => ce.Item).SingleOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return null;

        character.CurrentMapId = mapId;
        await db.SaveChangesAsync(ct);
        return character;
    }

    public async Task<Character?> UpdateHomeIslandAsync(int characterId, uint homeIslandId, CancellationToken ct = default)
    {
        var character = await db.Characters.Include(c => c.Inventory).ThenInclude(ci => ci.Item).Include(c => c.Equipment).ThenInclude(ce => ce.Item).SingleOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return null;

        character.HomeIslandId = homeIslandId;
        await db.SaveChangesAsync(ct);
        return character;
    }

    public async Task<Character?> CompleteHomeRegistrationAsync(int characterId, uint homeIslandId, uint modelId, CancellationToken ct = default)
    {
        var character = await db.Characters.Include(c => c.Inventory).ThenInclude(ci => ci.Item).Include(c => c.Equipment).ThenInclude(ce => ce.Item).SingleOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return null;

        character.HomeIslandId = homeIslandId;
        character.ModelId = modelId;
        await db.SaveChangesAsync(ct);
        return character;
    }

    public async Task AddInventoryAsync(int characterId, int itemId, int quantity, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var existing = await db.CharacterInventories.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.ItemId == itemId, ct);

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

    public async Task EquipAsync(int characterId, byte slotIndex, int itemId, CancellationToken ct = default)
    {
        _logger.LogInformation("Equipping item {ItemId} to character {CharacterId} in slot {SlotIndex}", itemId, characterId, slotIndex);
        if (slotIndex > 29)
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "0..29 only");

        // Ensure the character has the item in inventory
        //var hasItem = await db.CharacterInventories
        //    .AnyAsync(x => x.CharacterId == characterId && x.ItemId == itemId, ct);
        //if (!hasItem)
        //    throw new InvalidOperationException("Character does not own this item.");

        // Upsert the equipment for this slot
        var existing = await db.CharacterEquipments.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.SlotIndex == slotIndex, ct);

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
        var existing = await db.CharacterEquipments.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.SlotIndex == slotIndex, ct);

        if (existing is null)
            return;

        db.CharacterEquipments.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveInventoryAsync(int characterId, int itemId, int quantity, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var existing = await db.CharacterInventories.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.ItemId == itemId, ct);
        if (existing is null)
            return;

        existing.Quantity -= quantity;
        if (existing.Quantity <= 0)
            db.CharacterInventories.Remove(existing);

        await db.SaveChangesAsync(ct);
    }

    public async Task<EquipReplaceResult> ReplaceEquipmentAsync(int characterId, IEnumerable<ItemEquipEntry> equips, CancellationToken ct = default)
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

            if (!EquipSlotMapper.TryResolveSlotIndex(equip.ItemId, equip.SocketBit, out var slotIndex))
            {
                _logger.LogWarning("Skipping unmapped wardrobe equip item {ItemId} socket {Socket} for character {CharacterId}", equip.ItemId, equip.SocketBit, characterId);
                continue;
            }

            newBySlot[slotIndex] = equip;
        }

        var removed = new List<EquippedItemChange>();
        var pendingAdds = new List<(byte SlotIndex, ItemEquipEntry Equip)>();

        foreach (var old in existing)
        {
            if (newBySlot.TryGetValue(old.SlotIndex, out var replacement) && replacement.ItemId == (uint)old.ItemId)
                continue;

            removed.Add(
                new EquippedItemChange(
                    old.ItemId,
                    old.Item?.Name,
                    ItemEntityMapper.ResolveBodyspot(old.ItemId, name: old.Item?.Name)
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
        var addedItemsById = await db.Items.Where(i => addedItemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var added = new List<EquippedItemChange>(pendingAdds.Count);
        foreach (var (_, equip) in pendingAdds)
        {
            var equipItemId = (int)equip.ItemId;
            addedItemsById.TryGetValue(equipItemId, out var item);
            // Treat incoming socket bits as advisory; derive canonical bodyspot from item metadata
            // so mis-categorized UI tabs cannot force wrong slots (e.g. hats showing as coat).
            var socket = ItemEntityMapper.ResolveBodyspot(equipItemId, name: item?.Name);
            if (socket == 0)
                socket = equip.SocketBit;
            added.Add(new EquippedItemChange(equipItemId, item?.Name, socket));
        }

        var changedItemIds = removed.Select(x => x.ItemId).Concat(added.Select(x => x.ItemId)).Distinct().ToList();
        var inventoryByItemId = await db
            .CharacterInventories.Where(i => i.CharacterId == characterId && changedItemIds.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, ct);

        // Validate ownership before mutating: newly equipped items must be owned, accounting for
        // items that are unequipped in this same replacement and returned to inventory first.
        var availableByItemId = inventoryByItemId.ToDictionary(x => x.Key, x => x.Value.Quantity);
        foreach (var change in removed)
            availableByItemId[change.ItemId] = availableByItemId.GetValueOrDefault(change.ItemId) + 1;

        var requiredByItemId = pendingAdds
            .GroupBy(x => (int)x.Equip.ItemId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (itemId, required) in requiredByItemId)
        {
            if (availableByItemId.GetValueOrDefault(itemId) < required)
                throw new InvalidOperationException($"Character {characterId} does not own required quantity of item {itemId}.");
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

        foreach (var (itemId, required) in requiredByItemId)
        {
            var inventory = inventoryByItemId[itemId];
            inventory.Quantity -= required;
            if (inventory.Quantity <= 0)
            {
                db.CharacterInventories.Remove(inventory);
                inventoryByItemId.Remove(itemId);
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
            .CharacterInventories.Where(i => i.CharacterId == characterId && changedItemIds.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, i => i.Quantity, ct);

        foreach (var itemId in changedItemIds)
        {
            if (!countsByItemId.ContainsKey(itemId))
                countsByItemId[itemId] = 0;
        }

        return new EquipReplaceResult(removed, added, countsByItemId);
    }
}
