using AISpace.Common.DAL.Entities;

namespace AISpace.Common.DAL.Repositories;

internal interface IItemRepository
{
    Task CreateAsync(uint id, string name, CancellationToken ct = default);
    Task<Item?> GetByIdAsync(int id, CancellationToken ct = default);
}
