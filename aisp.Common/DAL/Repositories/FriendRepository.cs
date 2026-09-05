using System.Data;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public enum FriendResult
{
    Ok,
    InvalidTarget,
    AlreadyFriends,
    PendingExists,
    LimitReached,
    NoPendingRequest,
}

public sealed record FriendOperationResult(
    FriendResult Result,
    FriendRequest? Request = null,
    Character? OtherCharacter = null
);

public interface IFriendRepository
{
    Task<FriendOperationResult> RequestAsync(
        int requesterCharacterId,
        int targetCharacterId,
        CancellationToken ct = default
    );
    Task<FriendOperationResult> AnswerAsync(
        int targetCharacterId,
        bool accept,
        CancellationToken ct = default
    );
    Task<FriendOperationResult> CancelAsync(
        int requesterCharacterId,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<Character>> GetFriendsAsync(int characterId, CancellationToken ct = default);
    Task<IReadOnlyList<FriendLinkTag>> GetLinkTagsAsync(int characterId, CancellationToken ct = default);
    Task<FriendResult> SetLinkTagAsync(int characterId, uint slot, string name, CancellationToken ct = default);
    Task<FriendOperationResult> DeleteAsync(int characterId, int targetCharacterId, CancellationToken ct = default);
}

public sealed class FriendRepository(MainContext db) : IFriendRepository
{
    public const int MaxFriends = 250;

    public async Task<FriendOperationResult> RequestAsync(
        int requesterCharacterId,
        int targetCharacterId,
        CancellationToken ct = default
    )
    {
        if (requesterCharacterId == targetCharacterId)
            return new FriendOperationResult(FriendResult.InvalidTarget);

        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var characters = await db
            .Characters.Where(x => x.Id == requesterCharacterId || x.Id == targetCharacterId)
            .ToDictionaryAsync(x => x.Id, ct);
        if (
            !characters.ContainsKey(requesterCharacterId)
            || !characters.TryGetValue(targetCharacterId, out var target)
        )
            return new FriendOperationResult(FriendResult.InvalidTarget);

        var low = Math.Min(requesterCharacterId, targetCharacterId);
        var high = Math.Max(requesterCharacterId, targetCharacterId);
        if (
            await db.Friendships.AnyAsync(
                x => x.CharacterIdLow == low && x.CharacterIdHigh == high,
                ct
            )
        )
            return new FriendOperationResult(FriendResult.AlreadyFriends);

        if (
            await db.FriendRequests.AnyAsync(
                x =>
                    x.Status == FriendRequestStatus.Pending
                    && (
                        (
                            x.RequesterCharacterId == requesterCharacterId
                            && x.TargetCharacterId == targetCharacterId
                        )
                        || (
                            x.RequesterCharacterId == targetCharacterId
                            && x.TargetCharacterId == requesterCharacterId
                        )
                    ),
                ct
            )
        )
            return new FriendOperationResult(FriendResult.PendingExists);

        if (
            await CountFriendsAsync(requesterCharacterId, ct) >= MaxFriends
            || await CountFriendsAsync(targetCharacterId, ct) >= MaxFriends
        )
            return new FriendOperationResult(FriendResult.LimitReached);

        var request = new FriendRequest
        {
            RequesterCharacterId = requesterCharacterId,
            TargetCharacterId = targetCharacterId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        db.FriendRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new FriendOperationResult(FriendResult.Ok, request, target);
    }

    public async Task<FriendOperationResult> AnswerAsync(
        int targetCharacterId,
        bool accept,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var request = await db
            .FriendRequests.Include(x => x.RequesterCharacter)
            .Where(x =>
                x.TargetCharacterId == targetCharacterId && x.Status == FriendRequestStatus.Pending
            )
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (request is null)
            return new FriendOperationResult(FriendResult.NoPendingRequest);

        if (accept)
        {
            var low = Math.Min(request.RequesterCharacterId, targetCharacterId);
            var high = Math.Max(request.RequesterCharacterId, targetCharacterId);
            var alreadyFriends = await db.Friendships.AnyAsync(
                x => x.CharacterIdLow == low && x.CharacterIdHigh == high,
                ct
            );
            if (!alreadyFriends)
            {
                if (
                    await CountFriendsAsync(request.RequesterCharacterId, ct) >= MaxFriends
                    || await CountFriendsAsync(targetCharacterId, ct) >= MaxFriends
                )
                {
                    request.Status = FriendRequestStatus.Rejected;
                    request.ResolvedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return new FriendOperationResult(
                        FriendResult.LimitReached,
                        request,
                        request.RequesterCharacter
                    );
                }

                db.Friendships.Add(
                    new Friendship
                    {
                        CharacterIdLow = low,
                        CharacterIdHigh = high,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }
        }

        request.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;
        request.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new FriendOperationResult(FriendResult.Ok, request, request.RequesterCharacter);
    }

    public async Task<FriendOperationResult> CancelAsync(
        int requesterCharacterId,
        CancellationToken ct = default
    )
    {
        var request = await db
            .FriendRequests.Where(x =>
                x.RequesterCharacterId == requesterCharacterId
                && x.Status == FriendRequestStatus.Pending
            )
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (request is null)
            return new FriendOperationResult(FriendResult.NoPendingRequest);

        request.Status = FriendRequestStatus.Rejected;
        request.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new FriendOperationResult(FriendResult.Ok, request);
    }

    public async Task<IReadOnlyList<Character>> GetFriendsAsync(
        int characterId,
        CancellationToken ct = default
    )
    {
        var ids = await db
            .Friendships.Where(x =>
                x.CharacterIdLow == characterId || x.CharacterIdHigh == characterId
            )
            .Select(x => x.CharacterIdLow == characterId ? x.CharacterIdHigh : x.CharacterIdLow)
            .Take(MaxFriends)
            .ToListAsync(ct);
        return await db
            .Characters.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FriendLinkTag>> GetLinkTagsAsync(
        int characterId,
        CancellationToken ct = default
    ) => await db.FriendLinkTags.AsNoTracking().Where(x => x.CharacterId == characterId).OrderBy(x => x.Slot).ToListAsync(ct);

    public async Task<FriendResult> SetLinkTagAsync(
        int characterId,
        uint slot,
        string name,
        CancellationToken ct = default
    )
    {
        // The client has five visible tag positions (0 through 4).
        if (slot > 4 || !await db.Characters.AnyAsync(x => x.Id == characterId, ct))
            return FriendResult.InvalidTarget;

        var tag = await db.FriendLinkTags.FindAsync([characterId, slot], ct);
        if (string.IsNullOrWhiteSpace(name))
        {
            if (tag is not null)
                db.FriendLinkTags.Remove(tag);
        }
        else if (tag is null)
        {
            db.FriendLinkTags.Add(new FriendLinkTag { CharacterId = characterId, Slot = slot, Name = name });
        }
        else
        {
            tag.Name = name;
        }

        await db.SaveChangesAsync(ct);
        return FriendResult.Ok;
    }

    public async Task<FriendOperationResult> DeleteAsync(
        int characterId,
        int targetCharacterId,
        CancellationToken ct = default
    )
    {
        var low = Math.Min(characterId, targetCharacterId);
        var high = Math.Max(characterId, targetCharacterId);
        var friendship = await db.Friendships.SingleOrDefaultAsync(
            x => x.CharacterIdLow == low && x.CharacterIdHigh == high,
            ct
        );
        if (friendship is null)
            return new FriendOperationResult(FriendResult.InvalidTarget);

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync(ct);
        return new FriendOperationResult(FriendResult.Ok);
    }

    private Task<int> CountFriendsAsync(int characterId, CancellationToken ct) =>
        db.Friendships.CountAsync(
            x => x.CharacterIdLow == characterId || x.CharacterIdHigh == characterId,
            ct
        );
}
