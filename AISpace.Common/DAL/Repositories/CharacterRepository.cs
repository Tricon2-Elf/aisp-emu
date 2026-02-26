using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.DAL.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Character?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Character> CreateAsync(string name, int userId, uint modelId, BloodType bloodType, DateTime birthday, int Gender, uint faceType, uint hairStyle, CancellationToken ct = default);
    Task AddInventoryAsync(int characterId, int itemId, int quantity, CancellationToken ct = default);
    Task EquipAsync(int characterId, byte slotIndex, int itemId, CancellationToken ct = default);
    Task UnequipAsync(int characterId, byte slotIndex, CancellationToken ct = default);
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
}
