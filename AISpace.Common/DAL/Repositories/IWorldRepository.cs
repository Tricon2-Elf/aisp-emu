using AISpace.Common.DAL.Entities;

namespace AISpace.Common.DAL.Repositories;

public interface IWorldRepository
{
    Task AddAsync(string name, string description, string address, ushort port);
    Task<World?> GetByIdAsync(int id);

    Task<World?> GetByNameAsync(string name);
    Task<List<World>> GetAllAsync();
}
