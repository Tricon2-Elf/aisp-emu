using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public class WorldRepository(MainContext db) : IWorldRepository
{
    private readonly MainContext _db = db;

    public async Task AddAsync(string name, string description, string address, ushort port)
    {
        if ((await GetByNameAsync(name)) != null)
            return;
        var world = new World { Name = name, Description = description, Address = address, Port = port };
        _db.Worlds.Add(world);
        await _db.SaveChangesAsync();
    }

    public async Task<List<World>> GetAllAsync()
    {
        return await _db.Worlds.ToListAsync();
    }

    public async Task<World?> GetByIdAsync(int id)
    {
        return await _db.Worlds.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<World?> GetByNameAsync(string name)
    {
        return await _db.Worlds.FirstOrDefaultAsync(w => w.Name == name);
    }
}
