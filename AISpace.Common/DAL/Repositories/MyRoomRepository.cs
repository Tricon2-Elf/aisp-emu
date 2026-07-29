using System.Data;
using System.Text.Json;
using AISpace.Common.DAL.Entities;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IMyRoomRepository
{
    Task<IReadOnlyList<Furniture>> GetFurnitureCatalogAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MyRoomFurniture>> GetFurnitureAsync(int characterId, CancellationToken ct = default);
    Task<MyRoomFurniture?> GetFurnitureAsync(int characterId, uint furnitureId, CancellationToken ct = default);
    Task<bool> CanPlaceFurnitureAsync(int characterId, int itemId, uint placementLimit, CancellationToken ct = default);
    Task<MyRoomFurniture?> TryAddFurnitureAsync(MyRoomFurniture furniture, uint placementLimit, CancellationToken ct = default);
    Task<bool> UpdateFurnitureAsync(int characterId, uint furnitureId, float x, float y, float z, byte directionX, byte directionY, CancellationToken ct = default);
    Task<MyRoomFurniture?> RemoveFurnitureAsync(int characterId, uint furnitureId, CancellationToken ct = default);
    Task<bool> UpdateNameAsync(int characterId, string name, CancellationToken ct = default);
    Task<bool> UpdateSecurityAsync(int characterId, uint security, CancellationToken ct = default);
}

public sealed class MyRoomRepository(MainContext db) : IMyRoomRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Furniture>> GetFurnitureCatalogAsync(CancellationToken ct = default) => await db.Furniture.AsNoTracking().OrderBy(x => x.ItemId).ToListAsync(ct);

    public async Task<IReadOnlyList<MyRoomFurniture>> GetFurnitureAsync(int characterId, CancellationToken ct = default) => await db.MyRoomFurniture.AsNoTracking().Where(x => x.CharacterId == characterId).OrderBy(x => x.FurnitureId).ToListAsync(ct);

    public Task<MyRoomFurniture?> GetFurnitureAsync(int characterId, uint furnitureId, CancellationToken ct = default) => db.MyRoomFurniture.AsNoTracking().SingleOrDefaultAsync(x => x.CharacterId == characterId && x.FurnitureId == furnitureId, ct);

    public async Task<bool> CanPlaceFurnitureAsync(int characterId, int itemId, uint placementLimit, CancellationToken ct = default)
    {
        if (!await db.Furniture.AnyAsync(x => x.ItemId == itemId, ct))
            return false;

        var ownedQuantity = await db.CharacterInventories.Where(x => x.CharacterId == characterId && x.ItemId == itemId).Select(x => (int?)x.Quantity).SingleOrDefaultAsync(ct) ?? 0;
        if (ownedQuantity <= 0)
            return false;

        var placedFurniture = db.MyRoomFurniture.Where(x => x.CharacterId == characterId);
        if ((uint)await placedFurniture.CountAsync(ct) >= placementLimit)
            return false;

        return await placedFurniture.CountAsync(x => x.ItemId == itemId, ct) < ownedQuantity;
    }

    public async Task<MyRoomFurniture?> TryAddFurnitureAsync(MyRoomFurniture furniture, uint placementLimit, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var isFurniture = await db.Furniture.AnyAsync(x => x.ItemId == furniture.ItemId, ct);
        if (!isFurniture)
            return null;

        var ownedQuantity = await db.CharacterInventories.Where(x => x.CharacterId == furniture.CharacterId && x.ItemId == furniture.ItemId).Select(x => (int?)x.Quantity).SingleOrDefaultAsync(ct) ?? 0;
        if (ownedQuantity <= 0)
            return null;

        var placedFurniture = db.MyRoomFurniture.Where(x => x.CharacterId == furniture.CharacterId);
        if ((uint)await placedFurniture.CountAsync(ct) >= placementLimit)
            return null;

        if (await placedFurniture.CountAsync(x => x.ItemId == furniture.ItemId, ct) >= ownedQuantity)
            return null;

        var highestId = await db.MyRoomFurniture.Where(x => x.CharacterId == furniture.CharacterId).MaxAsync(x => (uint?)x.FurnitureId, ct) ?? 0;
        if (highestId == uint.MaxValue)
            throw new InvalidOperationException($"MyRoom furniture ID space is exhausted for character {furniture.CharacterId}.");

        furniture.FurnitureId = highestId + 1;
        db.MyRoomFurniture.Add(furniture);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return furniture;
    }

    public static async Task EnsureFurnitureCatalogPresentAsync(MainContext db, string jsonPath, CancellationToken ct = default)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Furniture catalog seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = (JsonSerializer.Deserialize<List<FurnitureSeedRow>>(json, JsonOptions) ?? []).DistinctBy(x => x.ItemId).ToList();
        if (rows.Count > FurnitureGetBaseListResponse.MaximumEntryCount)
            throw new InvalidDataException($"Furniture catalog contains {rows.Count} entries; the client accepts at most {FurnitureGetBaseListResponse.MaximumEntryCount}.");

        var validFlags = FurniturePlacementFlags.Floor | FurniturePlacementFlags.Wall | FurniturePlacementFlags.Ceiling;
        if (rows.Any(x => x.ItemId <= 0 || x.Name.Length == 0 || x.PlacementFlags == 0 || (x.PlacementFlags & ~validFlags) != 0))
            throw new InvalidDataException("Furniture catalog contains an invalid item ID, name, or placement flag.");

        var existingItemIds = (await db.Items.Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        var existingFurnitureIds = (await db.Furniture.Select(x => x.ItemId).ToListAsync(ct)).ToHashSet();

        db.Items.AddRange(
            rows.Where(x => !existingItemIds.Contains(x.ItemId))
                .Select(x => new Item
                {
                    Id = x.ItemId,
                    Name = x.Name,
                    Socket = 0,
                    IconId = x.ItemId,
                })
        );
        db.Furniture.AddRange(
            rows.Where(x => !existingFurnitureIds.Contains(x.ItemId))
                .Select(x => new Furniture
                {
                    ItemId = x.ItemId,
                    Type = x.Type,
                    PlacementFlags = x.PlacementFlags,
                })
        );
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateFurnitureAsync(int characterId, uint furnitureId, float x, float y, float z, byte directionX, byte directionY, CancellationToken ct = default)
    {
        var furniture = await db.MyRoomFurniture.SingleOrDefaultAsync(entry => entry.CharacterId == characterId && entry.FurnitureId == furnitureId, ct);
        if (furniture is null)
            return false;

        furniture.PositionX = x;
        furniture.PositionY = y;
        furniture.PositionZ = z;
        furniture.DirectionX = directionX;
        furniture.DirectionY = directionY;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MyRoomFurniture?> RemoveFurnitureAsync(int characterId, uint furnitureId, CancellationToken ct = default)
    {
        var furniture = await db.MyRoomFurniture.SingleOrDefaultAsync(entry => entry.CharacterId == characterId && entry.FurnitureId == furnitureId, ct);
        if (furniture is null)
            return null;

        db.MyRoomFurniture.Remove(furniture);
        await db.SaveChangesAsync(ct);
        return furniture;
    }

    public async Task<bool> UpdateNameAsync(int characterId, string name, CancellationToken ct = default)
    {
        var character = await db.Characters.SingleOrDefaultAsync(entry => entry.Id == characterId, ct);
        if (character is null)
            return false;

        character.MyRoomName = name;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateSecurityAsync(int characterId, uint security, CancellationToken ct = default)
    {
        var character = await db.Characters.SingleOrDefaultAsync(entry => entry.Id == characterId, ct);
        if (character is null)
            return false;

        character.MyRoomSecurity = security;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private sealed class FurnitureSeedRow
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public uint Type { get; set; }
        public FurniturePlacementFlags PlacementFlags { get; set; }
    }
}
