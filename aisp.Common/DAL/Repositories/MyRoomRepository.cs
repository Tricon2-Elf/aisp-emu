using System.Data;
using System.Text.Json;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IMyRoomRepository
{
    Task<Room?> GetRoomAsync(int roomId, CancellationToken ct = default);
    Task<Room?> GetDefaultRoomAsync(int ownerCharacterId, CancellationToken ct = default);
    Task<Room?> GetOrCreateDefaultRoomAsync(int ownerCharacterId, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetRoomsAsync(int ownerCharacterId, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetCandidateVisitRoomsAsync(
        int excludeOwnerCharacterId,
        int take,
        CancellationToken ct = default
    );
    Task<Room?> CreateRoomAsync(
        int ownerCharacterId,
        MyRoomStage stage,
        string name,
        CancellationToken ct = default
    );
    Task<bool> IsOwnerAsync(int roomId, int characterId, CancellationToken ct = default);
    Task<IReadOnlyList<Furniture>> GetFurnitureCatalogAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MyRoomFurniture>> GetFurnitureAsync(
        int roomId,
        CancellationToken ct = default
    );
    Task<MyRoomFurniture?> GetFurnitureAsync(
        int roomId,
        uint furnitureId,
        CancellationToken ct = default
    );
    Task<IReadOnlyDictionary<int, int>> GetAvailableFurnitureInventoryAsync(
        int characterId,
        CancellationToken ct = default
    );
    Task<bool> CanPlaceFurnitureAsync(
        int characterId,
        int roomId,
        int itemId,
        uint placementLimit,
        CancellationToken ct = default
    );
    Task<MyRoomFurniture?> TryAddFurnitureAsync(
        int characterId,
        MyRoomFurniture furniture,
        uint placementLimit,
        CancellationToken ct = default
    );
    Task<bool> UpdateFurnitureAsync(
        int roomId,
        uint furnitureId,
        float x,
        float y,
        float z,
        byte directionX,
        byte directionY,
        CancellationToken ct = default
    );
    Task<MyRoomFurniture?> RemoveFurnitureAsync(
        int roomId,
        uint furnitureId,
        CancellationToken ct = default
    );
    Task<bool> UpdateNameAsync(
        int roomId,
        int ownerCharacterId,
        string name,
        CancellationToken ct = default
    );
    Task<bool> UpdateSecurityAsync(
        int roomId,
        int ownerCharacterId,
        MyRoomSecurity security,
        CancellationToken ct = default
    );
}

public sealed class MyRoomRepository(MainContext db) : IMyRoomRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SeedJson.Options;

    public Task<Room?> GetRoomAsync(int roomId, CancellationToken ct = default) =>
        db
            .Rooms.AsNoTracking()
            .Include(x => x.OwnerCharacter)
            .SingleOrDefaultAsync(x => x.Id == roomId, ct);

    public Task<Room?> GetDefaultRoomAsync(int ownerCharacterId, CancellationToken ct = default) =>
        db
            .Rooms.AsNoTracking()
            .Include(x => x.OwnerCharacter)
            .Where(x => x.OwnerCharacterId == ownerCharacterId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<Room?> GetOrCreateDefaultRoomAsync(
        int ownerCharacterId,
        CancellationToken ct = default
    )
    {
        var existing = await GetDefaultRoomAsync(ownerCharacterId, ct);
        if (existing is not null)
            return existing;

        if (!await db.Characters.AnyAsync(x => x.Id == ownerCharacterId, ct))
            return null;

        var room = new Room
        {
            OwnerCharacterId = ownerCharacterId,
            Name = "My Room",
            Stage = MyRoomStage.SixTatami,
            IsDefault = true,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return await GetRoomAsync(room.Id, ct);
    }

    public async Task<IReadOnlyList<Room>> GetRoomsAsync(
        int ownerCharacterId,
        CancellationToken ct = default
    ) =>
        await db
            .Rooms.AsNoTracking()
            .Where(x => x.OwnerCharacterId == ownerCharacterId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> GetCandidateVisitRoomsAsync(
        int excludeOwnerCharacterId,
        int take,
        CancellationToken ct = default
    )
    {
        if (take <= 0)
            return [];

        return await db
            .Rooms.AsNoTracking()
            .Include(x => x.OwnerCharacter)
            .Where(x =>
                x.OwnerCharacterId != excludeOwnerCharacterId
                && x.Security != MyRoomSecurity.Private
                && x.Security != MyRoomSecurity.FriendsOnly
            )
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<Room?> CreateRoomAsync(
        int ownerCharacterId,
        MyRoomStage stage,
        string name,
        CancellationToken ct = default
    )
    {
        if (
            !Enum.IsDefined(stage)
            || !await db.Characters.AnyAsync(x => x.Id == ownerCharacterId, ct)
        )
            return null;

        var hasRoom = await db.Rooms.AnyAsync(x => x.OwnerCharacterId == ownerCharacterId, ct);
        var room = new Room
        {
            OwnerCharacterId = ownerCharacterId,
            Name = string.IsNullOrWhiteSpace(name) ? "My Room" : name,
            Stage = stage,
            IsDefault = !hasRoom,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return await GetRoomAsync(room.Id, ct);
    }

    public Task<bool> IsOwnerAsync(int roomId, int characterId, CancellationToken ct = default) =>
        db.Rooms.AnyAsync(x => x.Id == roomId && x.OwnerCharacterId == characterId, ct);

    public async Task<IReadOnlyList<Furniture>> GetFurnitureCatalogAsync(
        CancellationToken ct = default
    ) => await db.Furniture.AsNoTracking().OrderBy(x => x.ItemId).ToListAsync(ct);

    public async Task<IReadOnlyList<MyRoomFurniture>> GetFurnitureAsync(
        int roomId,
        CancellationToken ct = default
    ) =>
        await db
            .MyRoomFurniture.AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.FurnitureId)
            .ToListAsync(ct);

    public Task<MyRoomFurniture?> GetFurnitureAsync(
        int roomId,
        uint furnitureId,
        CancellationToken ct = default
    ) =>
        db
            .MyRoomFurniture.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RoomId == roomId && x.FurnitureId == furnitureId, ct);

    public async Task<IReadOnlyDictionary<int, int>> GetAvailableFurnitureInventoryAsync(
        int characterId,
        CancellationToken ct = default
    )
    {
        var owned = await db
            .CharacterInventories.AsNoTracking()
            .Where(x =>
                x.CharacterId == characterId
                && db.Furniture.Any(furniture => furniture.ItemId == x.ItemId)
            )
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity, ct);
        var placed = await db
            .MyRoomFurniture.AsNoTracking()
            .Where(x => x.Room.OwnerCharacterId == characterId)
            .GroupBy(x => x.ItemId)
            .Select(group => new { ItemId = group.Key, Quantity = group.Count() })
            .ToListAsync(ct);

        foreach (var stack in placed)
        {
            owned.TryGetValue(stack.ItemId, out var ownedQuantity);
            owned[stack.ItemId] = Math.Max(0, ownedQuantity - stack.Quantity);
        }

        return owned;
    }

    public async Task<bool> CanPlaceFurnitureAsync(
        int characterId,
        int roomId,
        int itemId,
        uint placementLimit,
        CancellationToken ct = default
    )
    {
        if (
            !await db.Furniture.AnyAsync(x => x.ItemId == itemId, ct)
            || !await IsOwnerAsync(roomId, characterId, ct)
        )
            return false;

        var ownedQuantity =
            await db
                .CharacterInventories.Where(x => x.CharacterId == characterId && x.ItemId == itemId)
                .Select(x => (int?)x.Quantity)
                .SingleOrDefaultAsync(ct) ?? 0;
        if (ownedQuantity <= 0)
            return false;

        var roomFurniture = db.MyRoomFurniture.Where(x => x.RoomId == roomId);
        if ((uint)await roomFurniture.CountAsync(ct) >= placementLimit)
            return false;

        var placedByOwner = db.MyRoomFurniture.Where(x => x.Room.OwnerCharacterId == characterId);
        return await placedByOwner.CountAsync(x => x.ItemId == itemId, ct) < ownedQuantity;
    }

    public async Task<MyRoomFurniture?> TryAddFurnitureAsync(
        int characterId,
        MyRoomFurniture furniture,
        uint placementLimit,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        if (
            !await db.Furniture.AnyAsync(x => x.ItemId == furniture.ItemId, ct)
            || !await IsOwnerAsync(furniture.RoomId, characterId, ct)
        )
            return null;

        var ownedQuantity =
            await db
                .CharacterInventories.Where(x =>
                    x.CharacterId == characterId && x.ItemId == furniture.ItemId
                )
                .Select(x => (int?)x.Quantity)
                .SingleOrDefaultAsync(ct) ?? 0;
        if (ownedQuantity <= 0)
            return null;

        var roomFurniture = db.MyRoomFurniture.Where(x => x.RoomId == furniture.RoomId);
        if ((uint)await roomFurniture.CountAsync(ct) >= placementLimit)
            return null;

        var placedByOwner = db.MyRoomFurniture.Where(x => x.Room.OwnerCharacterId == characterId);
        if (await placedByOwner.CountAsync(x => x.ItemId == furniture.ItemId, ct) >= ownedQuantity)
            return null;

        var highestId = await roomFurniture.MaxAsync(x => (uint?)x.FurnitureId, ct) ?? 0;
        if (highestId == uint.MaxValue)
            throw new InvalidOperationException(
                $"MyRoom furniture ID space is exhausted for room {furniture.RoomId}."
            );

        furniture.FurnitureId = highestId + 1;
        db.MyRoomFurniture.Add(furniture);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return furniture;
    }

    public static async Task EnsureFurnitureCatalogPresentAsync(
        MainContext db,
        string jsonPath,
        CancellationToken ct = default
    )
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Furniture catalog seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = (JsonSerializer.Deserialize<List<FurnitureSeedRow>>(json, JsonOptions) ?? [])
            .DistinctBy(x => x.ItemId)
            .ToList();
        if (rows.Count > FurnitureGetBaseListResponse.MaximumEntryCount)
            throw new InvalidDataException(
                $"Furniture catalog contains {rows.Count} entries; the client accepts at most {FurnitureGetBaseListResponse.MaximumEntryCount}."
            );

        var validFlags =
            FurniturePlacementFlags.Floor
            | FurniturePlacementFlags.Wall
            | FurniturePlacementFlags.Ceiling;
        if (
            rows.Any(x =>
                x.ItemId <= 0
                || x.Name.IsEmpty
                || x.PlacementFlags == 0
                || (x.PlacementFlags & ~validFlags) != 0
            )
        )
            throw new InvalidDataException(
                "Furniture catalog contains an invalid item ID, name, or placement flag."
            );

        var existingItemIds = (await db.Items.Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        var existingFurnitureIds = (
            await db.Furniture.Select(x => x.ItemId).ToListAsync(ct)
        ).ToHashSet();

        db.Items.AddRange(
            rows.Where(x => !existingItemIds.Contains(x.ItemId))
                .Select(x =>
                {
                    var canonicalName = x.Name.Canonical;
                    return new Item
                    {
                        Id = x.ItemId,
                        Name = canonicalName,
                        Socket = 0,
                        IconId = x.ItemId,
                        CatalogCategory = (int)
                            ItemEntityMapper.ResolvePersistedCatalogCategory(
                                x.ItemId,
                                canonicalName,
                                x.PlacementFlags
                            ),
                    };
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

    public async Task<bool> UpdateFurnitureAsync(
        int roomId,
        uint furnitureId,
        float x,
        float y,
        float z,
        byte directionX,
        byte directionY,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var furniture = await db.MyRoomFurniture.SingleOrDefaultAsync(
            entry => entry.RoomId == roomId && entry.FurnitureId == furnitureId,
            ct
        );
        if (furniture is null)
            return false;

        furniture.PositionX = x;
        furniture.PositionY = y;
        furniture.PositionZ = z;
        furniture.DirectionX = directionX;
        furniture.DirectionY = directionY;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<MyRoomFurniture?> RemoveFurnitureAsync(
        int roomId,
        uint furnitureId,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var furniture = await db.MyRoomFurniture.SingleOrDefaultAsync(
            entry => entry.RoomId == roomId && entry.FurnitureId == furnitureId,
            ct
        );
        if (furniture is null)
            return null;

        db.MyRoomFurniture.Remove(furniture);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return furniture;
    }

    public async Task<bool> UpdateNameAsync(
        int roomId,
        int ownerCharacterId,
        string name,
        CancellationToken ct = default
    )
    {
        var room = await db.Rooms.SingleOrDefaultAsync(
            entry => entry.Id == roomId && entry.OwnerCharacterId == ownerCharacterId,
            ct
        );
        if (room is null)
            return false;

        room.Name = name;
        room.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateSecurityAsync(
        int roomId,
        int ownerCharacterId,
        MyRoomSecurity security,
        CancellationToken ct = default
    )
    {
        if (!Enum.IsDefined(security))
            return false;

        var room = await db.Rooms.SingleOrDefaultAsync(
            entry => entry.Id == roomId && entry.OwnerCharacterId == ownerCharacterId,
            ct
        );
        if (room is null)
            return false;

        room.Security = security;
        room.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private sealed class FurnitureSeedRow
    {
        public int ItemId { get; set; }
        public LocalisedString Name { get; set; } = new();
        public uint Type { get; set; }
        public FurniturePlacementFlags PlacementFlags { get; set; }
    }
}
