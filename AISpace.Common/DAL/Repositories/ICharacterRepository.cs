using AISpace.Common.DAL.Entities;

namespace AISpace.Common.DAL.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetCharaByIdAsync(int characterId);
    Task<ICollection<Character>?> GetCharactersByUserAsync(int userId);
}
