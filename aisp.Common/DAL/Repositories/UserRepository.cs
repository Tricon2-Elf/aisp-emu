using System.Data;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Localisation;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task AddAsync(string username, string password);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetById(int userId);
    Task SetBannedAsync(
        int userId,
        bool isBanned,
        string? reason = null,
        DateTime? bannedUntil = null
    );
    Task SetKickedUntilAsync(int userId, DateTime? kickedUntil, CancellationToken ct = default);
    Task SetRoleAsync(int userId, UserRole role, CancellationToken ct = default);
    Task ClearExpiredBanAsync(int userId, CancellationToken ct = default);
    Task PromoteToServerAdminIfBelowAsync(int userId, CancellationToken ct = default);
    Task TouchLastLoggedInAsync(int userId, CancellationToken ct = default);
    Task UpdatePasswordAsync(int userId, string newPassword);
    Task SetLanguageAsync(int userId, GameLanguage language, CancellationToken ct = default);
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
    /// Inventory→storage refuses quantities that would leave fewer owned copies than MyRoom placements.
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

    public async Task SetBannedAsync(
        int userId,
        bool isBanned,
        string? reason = null,
        DateTime? bannedUntil = null
    )
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return;

        user.IsBanned = isBanned;
        user.BanReason = isBanned ? reason : null;
        user.BannedAt = isBanned ? DateTime.UtcNow : null;
        user.BannedUntil = isBanned ? bannedUntil : null;
        await _db.SaveChangesAsync();
    }

    public async Task SetKickedUntilAsync(
        int userId,
        DateTime? kickedUntil,
        CancellationToken ct = default
    )
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
            return;

        user.KickedUntil = kickedUntil;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetRoleAsync(int userId, UserRole role, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
            return;

        user.Role = role;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearExpiredBanAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null || !UserModerationState.ShouldClearExpiredBan(user))
            return;

        user.IsBanned = false;
        user.BanReason = null;
        user.BannedAt = null;
        user.BannedUntil = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task PromoteToServerAdminIfBelowAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null || user.Role >= UserRole.ServerAdmin)
            return;

        user.Role = UserRole.ServerAdmin;
        await _db.SaveChangesAsync(ct);
    }

    public async Task TouchLastLoggedInAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
            return;

        user.LastLoggedInAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdatePasswordAsync(int userId, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return;

        user.SetPassword(newPassword);
        await _db.SaveChangesAsync();
    }

    public async Task SetLanguageAsync(
        int userId,
        GameLanguage language,
        CancellationToken ct = default
    )
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
            return;

        user.Language = language;
        await _db.SaveChangesAsync(ct);
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

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

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

            // Placed MyRoom furniture stays owned via inventory; do not warehouse those copies.
            var remainingQuantity = inventory.Quantity - quantity;
            var placedQuantity =
                itemId < 0
                    ? 0
                    : await _db.MyRoomFurniture.CountAsync(
                        x => x.Room.OwnerCharacterId == characterId && x.ItemId == itemId,
                        ct
                    );
            if (remainingQuantity < placedQuantity)
                return null;

            inventory.Quantity = remainingQuantity;
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
        await transaction.CommitAsync(ct);
        return (Math.Max(0, inventoryQuantity), Math.Max(0, storageQuantity));
    }
}
