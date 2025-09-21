using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public class WorldRepository(MainContext db) : IWorldRepository
{
    private readonly MainContext _db = db;

    public async Task AddWorldAsync(string name, string description)
    {
        if ((await GetWorldByNameAsync(name)) != null)
            return;
        var world = new World { Name = name, Description = description };
        _db.Worlds.Add(world);
        await _db.SaveChangesAsync();
    }

    public async Task<List<World>> GetAllWorldsAsync()
    {
        return await _db.Worlds.ToListAsync();
    }

    public async Task<World?> GetWorldByIDAsync(int id)
    {
        return await _db.Worlds.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<World?> GetWorldByNameAsync(string name)
    {
        return await _db.Worlds.FirstOrDefaultAsync(w => w.Name == name);
    }
}
