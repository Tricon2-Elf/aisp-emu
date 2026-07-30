using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task AddAsync(string username, string password);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetById(int userId);
    Task SetBannedAsync(int userId, bool isBanned, string? reason = null);
    Task UpdatePasswordAsync(int userId, string newPassword);
    Task DeleteAsync(int userId);
    Task<IReadOnlyList<User>> GetAllAsync(
        string? search = null,
        int? skip = null,
        int? take = null
    );
    Task<int> CountAsync(string? search = null);
    Task<User?> AddMoneyAsync(
        int userId,
        long aiDelta,
        long nicoDelta,
        CancellationToken ct = default
    );

    /// <summary>
    /// Move AI points between purse and wardrobe deposit.
    /// Positive <paramref name="depositDelta"/> deposits from purse; negative withdraws to purse.
    /// </summary>
    Task<User?> TransferStorageDepositAsync(
        int userId,
        long depositDelta,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<UserStorageItem>> GetStorageItemsAsync(
        int userId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Move item stacks between character inventory (place 0) and account warehouse (place 1).
    /// Returns null on failure; otherwise the new inventory and storage quantities for the item.
    /// </summary>
    Task<(int InventoryQuantity, int StorageQuantity)?> TransferStorageItemAsync(
        int userId,
        int characterId,
        int itemId,
        int quantity,
        bool toStorage,
        CancellationToken ct = default
    );
}

public class UserRepository(MainContext db) : IUserRepository
{
    private readonly MainContext _db = db;

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _db
            .Users.Include(u => u.Characters)
                .ThenInclude(c => c.Inventory)
                    .ThenInclude(i => i.Item)
            .Include(u => u.Characters)
                .ThenInclude(c => c.Equipment)
                    .ThenInclude(e => e.Item)
            .SingleOrDefaultAsync(u => u.Username == username);
        if (user is null)
            return null;

        return user.VerifyPassword(password) ? user : null;
    }

    public async Task AddAsync(string username, string password)
    {
        var user = new User { Username = username };
        user.SetPassword(password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db
            .Users.Include(u => u.Characters)
                .ThenInclude(c => c.Inventory)
                    .ThenInclude(i => i.Item)
            .Include(u => u.Characters)
                .ThenInclude(c => c.Equipment)
                    .ThenInclude(e => e.Item)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetById(int userId)
    {
        return await _db
            .Users.Include(u => u.Characters)
                .ThenInclude(c => c.Inventory)
                    .ThenInclude(i => i.Item)
            .Include(u => u.Characters)
                .ThenInclude(c => c.Equipment)
                    .ThenInclude(e => e.Item)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task SetBannedAsync(int userId, bool isBanned, string? reason = null)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return;

        user.IsBanned = isBanned;
        user.BanReason = isBanned ? reason : null;
        user.BannedAt = isBanned ? DateTime.UtcNow : null;
        await _db.SaveChangesAsync();
    }

    public async Task UpdatePasswordAsync(int userId, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return;

        user.SetPassword(newPassword);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(
        string? search = null,
        int? skip = null,
        int? take = null
    )
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => EF.Functions.Like(u.Username, $"%{search}%"));

        query = query.OrderBy(u => u.Id);

        if (skip.HasValue)
            query = query.Skip(skip.Value);
        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.Include(u => u.Characters).ToListAsync();
    }

    public async Task<int> CountAsync(string? search = null)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => EF.Functions.Like(u.Username, $"%{search}%"));

        return await query.CountAsync();
    }

    public async Task<User?> AddMoneyAsync(
        int userId,
        long aiDelta,
        long nicoDelta,
        CancellationToken ct = default
    )
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;

        user.AiPoints = Math.Clamp(user.AiPoints + aiDelta, 0, long.MaxValue);
        user.NicoPoints = Math.Clamp(user.NicoPoints + nicoDelta, 0, long.MaxValue);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User?> TransferStorageDepositAsync(
        int userId,
        long depositDelta,
        CancellationToken ct = default
    )
    {
        if (depositDelta == 0)
            return await GetById(userId);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;

        var purseDelta = checked(-depositDelta);
        if (depositDelta > 0)
        {
            if (user.AiPoints < depositDelta)
                return null;
        }
        else if (user.StorageDeposit < -depositDelta)
        {
            return null;
        }

        user.AiPoints = checked(user.AiPoints + purseDelta);
        user.StorageDeposit = checked(user.StorageDeposit + depositDelta);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<IReadOnlyList<UserStorageItem>> GetStorageItemsAsync(
        int userId,
        CancellationToken ct = default
    ) =>
        await _db
            .UserStorageItems.AsNoTracking()
            .Where(x => x.UserId == userId && x.Quantity > 0)
            .OrderBy(x => x.ItemId)
            .ToListAsync(ct);

    public async Task<(int InventoryQuantity, int StorageQuantity)?> TransferStorageItemAsync(
        int userId,
        int characterId,
        int itemId,
        int quantity,
        bool toStorage,
        CancellationToken ct = default
    )
    {
        if (quantity <= 0)
            return null;

        var inventory = await _db.CharacterInventories.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.ItemId == itemId,
            ct
        );
        var storage = await _db.UserStorageItems.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ItemId == itemId,
            ct
        );

        int inventoryQuantity;
        int storageQuantity;

        if (toStorage)
        {
            if (inventory is null || inventory.Quantity < quantity)
                return null;

            inventory.Quantity -= quantity;
            inventoryQuantity = inventory.Quantity;
            if (inventory.Quantity == 0)
                _db.CharacterInventories.Remove(inventory);

            if (storage is null)
            {
                storage = new UserStorageItem
                {
                    UserId = userId,
                    ItemId = itemId,
                    Quantity = quantity,
                };
                _db.UserStorageItems.Add(storage);
            }
            else
            {
                storage.Quantity += quantity;
            }

            storageQuantity = storage.Quantity;
        }
        else
        {
            if (storage is null || storage.Quantity < quantity)
                return null;

            storage.Quantity -= quantity;
            storageQuantity = storage.Quantity;
            if (storage.Quantity == 0)
                _db.UserStorageItems.Remove(storage);

            if (inventory is null)
            {
                inventory = new CharacterInventory
                {
                    CharacterId = characterId,
                    ItemId = itemId,
                    Quantity = quantity,
                };
                _db.CharacterInventories.Add(inventory);
            }
            else
            {
                inventory.Quantity += quantity;
            }

            inventoryQuantity = inventory.Quantity;
        }

        await _db.SaveChangesAsync(ct);
        return (Math.Max(0, inventoryQuantity), Math.Max(0, storageQuantity));
    }
}
