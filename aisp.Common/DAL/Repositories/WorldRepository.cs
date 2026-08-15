using System.Text.Json;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IWorldRepository
{
    Task AddAsync(string name, string description, string address, ushort port);
    Task<World?> GetByIdAsync(int id);

    Task<World?> GetByNameAsync(string name);
    Task<List<World>> GetAllAsync();
}

public class WorldRepository(MainContext db) : IWorldRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MainContext _db = db;

    public async Task AddAsync(string name, string description, string address, ushort port)
    {
        if ((await GetByNameAsync(name)) != null)
            return;
        var world = new World
        {
            Name = name,
            Description = description,
            Address = address,
            Port = port,
        };
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

    /// <summary>Seeds world data if the Worlds table is empty. Call on startup after EnsureCreated.</summary>
    /// <param name="ipOverride">When set (e.g. IP_OVERRIDE in Docker), used as the world address instead of "localhost".</param>
    public static async Task SeedWorldsIfEmptyAsync(
        MainContext db,
        string jsonPath,
        string? ipOverride = null,
        ushort msgPort = 50052,
        CancellationToken ct = default
    )
    {
        if (await db.Worlds.AnyAsync(ct))
            return;

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException(
                "World seed JSON not found (required for empty Worlds table).",
                jsonPath
            );

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<WorldSeedRow>>(json, JsonOptions) ?? [];
        string address = !string.IsNullOrWhiteSpace(ipOverride) ? ipOverride : "localhost";

        foreach (var row in rows)
        {
            db.Worlds.Add(
                new World
                {
                    Name = row.Name,
                    Description = row.Description,
                    Address = address,
                    Port = msgPort,
                }
            );
        }

        await db.SaveChangesAsync(ct);
    }

    private sealed class WorldSeedRow
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
