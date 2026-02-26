using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;

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