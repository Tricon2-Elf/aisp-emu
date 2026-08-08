using System.Data;
using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public enum CircleResult : uint
{
    Ok = 0,
    Failed = 1,
    AlreadyInCircle = 2,
    NotFound = 3,
    NotMember = 4,
    NotAuthorized = 5,
    LimitReached = 6,
    InvalidTarget = 7,
    PendingExists = 8,
    NoPending = 9,
}

public sealed class CircleOperationResult
{
    public CircleResult Result { get; init; } = CircleResult.Failed;
    public Circle? Circle { get; init; }
    public CircleMember? Member { get; init; }
    public CircleJoinRequest? JoinRequest { get; init; }
    public IReadOnlyList<CircleMember> Members { get; init; } = [];
    public int? PreviousLeaderCharacterId { get; init; }
    public int? NewLeaderCharacterId { get; init; }
    public bool CircleDeleted { get; init; }

    public static CircleOperationResult Fail(CircleResult result) => new() { Result = result };

    public static CircleOperationResult Success(
        Circle? circle = null,
        CircleMember? member = null,
        CircleJoinRequest? joinRequest = null,
        IReadOnlyList<CircleMember>? members = null,
        int? previousLeaderCharacterId = null,
        int? newLeaderCharacterId = null,
        bool circleDeleted = false
    ) =>
        new()
        {
            Result = CircleResult.Ok,
            Circle = circle,
            Member = member,
            JoinRequest = joinRequest,
            Members = members ?? [],
            PreviousLeaderCharacterId = previousLeaderCharacterId,
            NewLeaderCharacterId = newLeaderCharacterId,
            CircleDeleted = circleDeleted,
        };
}

public interface ICircleRepository
{
    Task<Circle?> GetByIdAsync(int circleId, CancellationToken ct = default);
    Task<IReadOnlyList<CircleMember>> GetMembersAsync(int circleId, CancellationToken ct = default);
    Task<IReadOnlyList<(Circle Circle, uint AuthLevel)>> GetMembershipsForCharacterAsync(
        int characterId,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<int>> GetSharedCircleIdsAsync(
        int characterIdA,
        int characterIdB,
        CancellationToken ct = default
    );
    Task<bool> SharesAnyCircleAsync(
        int characterIdA,
        int characterIdB,
        CancellationToken ct = default
    );
    Task<CircleMember?> GetMembershipAsync(
        int circleId,
        int characterId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> CreateAsync(
        int leaderCharacterId,
        string name,
        uint markId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> InviteAsync(
        int requesterCharacterId,
        int targetCharacterId,
        int circleId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> AnswerInviteAsync(
        int targetCharacterId,
        bool accept,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> CancelInviteAsync(
        int requesterCharacterId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> KickAsync(
        int actorCharacterId,
        int circleId,
        int targetCharacterId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> ResignAsync(
        int characterId,
        int circleId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> SetCoreAuthorityAsync(
        int actorCharacterId,
        int circleId,
        int targetCharacterId,
        uint auth,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> UpdateMarkAsync(
        int actorCharacterId,
        int circleId,
        uint markId,
        CancellationToken ct = default
    );
    Task<CircleOperationResult> UpdateMessageAsync(
        int actorCharacterId,
        int circleId,
        string message,
        CancellationToken ct = default
    );
    CircleData ToCircleData(Circle circle);
}

public sealed class CircleRepository(MainContext db) : ICircleRepository
{
    public const int MaxMembershipsPerCharacter = CircleData.MaxCirclesPerCharacter;
    public const int MaxMembersPerCircle = CircleMemberData.MaxMembers;

    public Task<Circle?> GetByIdAsync(int circleId, CancellationToken ct = default) =>
        db.Circles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == circleId, ct);

    public async Task<IReadOnlyList<CircleMember>> GetMembersAsync(
        int circleId,
        CancellationToken ct = default
    ) =>
        await db
            .CircleMembers.AsNoTracking()
            .Include(x => x.Character)
            .Where(x => x.CircleId == circleId)
            .OrderBy(x => x.JoinedAt)
            .ThenBy(x => x.CharacterId)
            .ToListAsync(ct);

    public async Task<
        IReadOnlyList<(Circle Circle, uint AuthLevel)>
    > GetMembershipsForCharacterAsync(int characterId, CancellationToken ct = default)
    {
        var rows = await db
            .CircleMembers.AsNoTracking()
            .Include(x => x.Circle)
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.JoinedAt)
            .Take(MaxMembershipsPerCharacter)
            .ToListAsync(ct);
        return [.. rows.Select(x => (x.Circle, x.AuthLevel))];
    }

    public async Task<IReadOnlyList<int>> GetSharedCircleIdsAsync(
        int characterIdA,
        int characterIdB,
        CancellationToken ct = default
    )
    {
        var a = db.CircleMembers.Where(x => x.CharacterId == characterIdA).Select(x => x.CircleId);
        var b = db.CircleMembers.Where(x => x.CharacterId == characterIdB).Select(x => x.CircleId);
        return await a.Intersect(b).ToListAsync(ct);
    }

    public async Task<bool> SharesAnyCircleAsync(
        int characterIdA,
        int characterIdB,
        CancellationToken ct = default
    ) => (await GetSharedCircleIdsAsync(characterIdA, characterIdB, ct)).Count > 0;

    public Task<CircleMember?> GetMembershipAsync(
        int circleId,
        int characterId,
        CancellationToken ct = default
    ) =>
        db
            .CircleMembers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CircleId == circleId && x.CharacterId == characterId, ct);

    public async Task<CircleOperationResult> CreateAsync(
        int leaderCharacterId,
        string name,
        uint markId,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var membershipCount = await db.CircleMembers.CountAsync(
            x => x.CharacterId == leaderCharacterId,
            ct
        );
        if (membershipCount >= MaxMembershipsPerCharacter)
            return CircleOperationResult.Fail(CircleResult.LimitReached);

        var now = DateTime.UtcNow;
        var circle = new Circle
        {
            Name = Truncate(name, 46),
            Status = 1,
            MarkId = markId,
            Mark = string.Empty, // last message author name (not the icon)
            Message = string.Empty,
            MessageDate = string.Empty,
            LeaderCharacterId = leaderCharacterId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Circles.Add(circle);
        await db.SaveChangesAsync(ct);

        var member = new CircleMember
        {
            CircleId = circle.Id,
            CharacterId = leaderCharacterId,
            AuthLevel = CircleMemberData.RoleLeader,
            JoinedAt = now,
        };
        db.CircleMembers.Add(member);

        // Keep legacy Character.CircleId populated for transitional ACL consumers.
        var character = await db.Characters.FirstOrDefaultAsync(x => x.Id == leaderCharacterId, ct);
        if (character is not null && character.CircleId is null)
            character.CircleId = circle.Id;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(circle, member, members: new[] { member });
    }

    public async Task<CircleOperationResult> InviteAsync(
        int requesterCharacterId,
        int targetCharacterId,
        int circleId,
        CancellationToken ct = default
    )
    {
        if (requesterCharacterId == targetCharacterId)
            return CircleOperationResult.Fail(CircleResult.InvalidTarget);

        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var circle = await db.Circles.FirstOrDefaultAsync(x => x.Id == circleId, ct);
        if (circle is null)
            return CircleOperationResult.Fail(CircleResult.NotFound);

        var requester = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == requesterCharacterId,
            ct
        );
        if (requester is null)
            return CircleOperationResult.Fail(CircleResult.NotMember);
        if (requester.AuthLevel < CircleMemberData.RoleCore)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        if (!await db.Characters.AnyAsync(x => x.Id == targetCharacterId, ct))
            return CircleOperationResult.Fail(CircleResult.InvalidTarget);

        if (
            await db.CircleMembers.AnyAsync(
                x => x.CircleId == circleId && x.CharacterId == targetCharacterId,
                ct
            )
        )
            return CircleOperationResult.Fail(CircleResult.AlreadyInCircle);

        var memberCount = await db.CircleMembers.CountAsync(x => x.CircleId == circleId, ct);
        if (memberCount >= MaxMembersPerCircle)
            return CircleOperationResult.Fail(CircleResult.LimitReached);

        var targetCount = await db.CircleMembers.CountAsync(
            x => x.CharacterId == targetCharacterId,
            ct
        );
        if (targetCount >= MaxMembershipsPerCharacter)
            return CircleOperationResult.Fail(CircleResult.LimitReached);

        if (
            await db.CircleJoinRequests.AnyAsync(
                x =>
                    x.Status == CircleJoinRequestStatus.Pending
                    && (
                        x.TargetCharacterId == targetCharacterId
                        || x.RequesterCharacterId == requesterCharacterId
                    ),
                ct
            )
        )
            return CircleOperationResult.Fail(CircleResult.PendingExists);

        var request = new CircleJoinRequest
        {
            CircleId = circleId,
            RequesterCharacterId = requesterCharacterId,
            TargetCharacterId = targetCharacterId,
            Status = CircleJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        db.CircleJoinRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(circle, joinRequest: request);
    }

    public async Task<CircleOperationResult> AnswerInviteAsync(
        int targetCharacterId,
        bool accept,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var pending = await db
            .CircleJoinRequests.Include(x => x.Circle)
            .Where(x =>
                x.TargetCharacterId == targetCharacterId
                && x.Status == CircleJoinRequestStatus.Pending
            )
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (pending is null)
            return CircleOperationResult.Fail(CircleResult.NoPending);

        if (!accept)
        {
            pending.Status = CircleJoinRequestStatus.Rejected;
            pending.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return CircleOperationResult.Success(pending.Circle, joinRequest: pending);
        }

        var targetCount = await db.CircleMembers.CountAsync(
            x => x.CharacterId == targetCharacterId,
            ct
        );
        if (targetCount >= MaxMembershipsPerCharacter)
            return CircleOperationResult.Fail(CircleResult.LimitReached);

        if (
            await db.CircleMembers.AnyAsync(
                x => x.CircleId == pending.CircleId && x.CharacterId == targetCharacterId,
                ct
            )
        )
        {
            pending.Status = CircleJoinRequestStatus.Cancelled;
            pending.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return CircleOperationResult.Fail(CircleResult.AlreadyInCircle);
        }

        var memberCount = await db.CircleMembers.CountAsync(
            x => x.CircleId == pending.CircleId,
            ct
        );
        if (memberCount >= MaxMembersPerCircle)
            return CircleOperationResult.Fail(CircleResult.LimitReached);

        var now = DateTime.UtcNow;
        var member = new CircleMember
        {
            CircleId = pending.CircleId,
            CharacterId = targetCharacterId,
            AuthLevel = CircleMemberData.RoleMember,
            JoinedAt = now,
        };
        db.CircleMembers.Add(member);
        pending.Status = CircleJoinRequestStatus.Accepted;
        pending.ResolvedAt = now;

        var character = await db.Characters.FirstOrDefaultAsync(x => x.Id == targetCharacterId, ct);
        if (character is not null && character.CircleId is null)
            character.CircleId = pending.CircleId;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(pending.Circle, member, pending);
    }

    public async Task<CircleOperationResult> CancelInviteAsync(
        int requesterCharacterId,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var pending = await db
            .CircleJoinRequests.Include(x => x.Circle)
            .Where(x =>
                x.RequesterCharacterId == requesterCharacterId
                && x.Status == CircleJoinRequestStatus.Pending
            )
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (pending is null)
            return CircleOperationResult.Fail(CircleResult.NoPending);

        pending.Status = CircleJoinRequestStatus.Cancelled;
        pending.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(pending.Circle, joinRequest: pending);
    }

    public async Task<CircleOperationResult> KickAsync(
        int actorCharacterId,
        int circleId,
        int targetCharacterId,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var actor = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == actorCharacterId,
            ct
        );
        if (actor is null)
            return CircleOperationResult.Fail(CircleResult.NotMember);
        if (actor.AuthLevel < CircleMemberData.RoleCore)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        var target = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == targetCharacterId,
            ct
        );
        if (target is null)
            return CircleOperationResult.Fail(CircleResult.InvalidTarget);
        if (target.AuthLevel == CircleMemberData.RoleLeader)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);
        if (
            actor.AuthLevel == CircleMemberData.RoleCore
            && target.AuthLevel >= CircleMemberData.RoleCore
        )
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        db.CircleMembers.Remove(target);
        await ClearLegacyCircleIdIfNeededAsync(targetCharacterId, circleId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        var circle = await db.Circles.AsNoTracking().FirstAsync(x => x.Id == circleId, ct);
        return CircleOperationResult.Success(circle, target);
    }

    public async Task<CircleOperationResult> ResignAsync(
        int characterId,
        int circleId,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var circle = await db.Circles.FirstOrDefaultAsync(x => x.Id == circleId, ct);
        if (circle is null)
            return CircleOperationResult.Fail(CircleResult.NotFound);

        var member = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == characterId,
            ct
        );
        if (member is null)
            return CircleOperationResult.Fail(CircleResult.NotMember);

        int? previousLeader = null;
        int? newLeader = null;
        var deleted = false;

        db.CircleMembers.Remove(member);
        await ClearLegacyCircleIdIfNeededAsync(characterId, circleId, ct);

        if (
            member.AuthLevel == CircleMemberData.RoleLeader
            || circle.LeaderCharacterId == characterId
        )
        {
            previousLeader = characterId;
            var successor = await db
                .CircleMembers.Where(x => x.CircleId == circleId && x.CharacterId != characterId)
                .OrderByDescending(x => x.AuthLevel)
                .ThenBy(x => x.JoinedAt)
                .ThenBy(x => x.CharacterId)
                .FirstOrDefaultAsync(ct);

            if (successor is null)
            {
                var pending = await db
                    .CircleJoinRequests.Where(x => x.CircleId == circleId)
                    .ToListAsync(ct);
                db.CircleJoinRequests.RemoveRange(pending);
                db.Circles.Remove(circle);
                deleted = true;
            }
            else
            {
                successor.AuthLevel = CircleMemberData.RoleLeader;
                circle.LeaderCharacterId = successor.CharacterId;
                circle.UpdatedAt = DateTime.UtcNow;
                newLeader = successor.CharacterId;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(
            deleted ? null : circle,
            member,
            previousLeaderCharacterId: previousLeader,
            newLeaderCharacterId: newLeader,
            circleDeleted: deleted
        );
    }

    public async Task<CircleOperationResult> SetCoreAuthorityAsync(
        int actorCharacterId,
        int circleId,
        int targetCharacterId,
        uint auth,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var actor = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == actorCharacterId,
            ct
        );
        if (actor is null)
            return CircleOperationResult.Fail(CircleResult.NotMember);
        if (actor.AuthLevel != CircleMemberData.RoleLeader)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        var target = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == targetCharacterId,
            ct
        );
        if (target is null)
            return CircleOperationResult.Fail(CircleResult.InvalidTarget);
        if (target.AuthLevel == CircleMemberData.RoleLeader)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        target.AuthLevel = auth != 0 ? CircleMemberData.RoleCore : CircleMemberData.RoleMember;
        var circle = await db.Circles.FirstAsync(x => x.Id == circleId, ct);
        circle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(circle, target);
    }

    public async Task<CircleOperationResult> UpdateMarkAsync(
        int actorCharacterId,
        int circleId,
        uint markId,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var actor = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == actorCharacterId,
            ct
        );
        if (actor is null)
            return CircleOperationResult.Fail(CircleResult.NotMember);
        if (actor.AuthLevel != CircleMemberData.RoleLeader)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        var circle = await db.Circles.FirstOrDefaultAsync(x => x.Id == circleId, ct);
        if (circle is null)
            return CircleOperationResult.Fail(CircleResult.NotFound);

        circle.MarkId = markId;
        circle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(circle);
    }

    public async Task<CircleOperationResult> UpdateMessageAsync(
        int actorCharacterId,
        int circleId,
        string message,
        CancellationToken ct = default
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var actor = await db.CircleMembers.FirstOrDefaultAsync(
            x => x.CircleId == circleId && x.CharacterId == actorCharacterId,
            ct
        );
        if (actor is null)
            return CircleOperationResult.Fail(CircleResult.NotMember);
        if (actor.AuthLevel < CircleMemberData.RoleCore)
            return CircleOperationResult.Fail(CircleResult.NotAuthorized);

        var circle = await db.Circles.FirstOrDefaultAsync(x => x.Id == circleId, ct);
        if (circle is null)
            return CircleOperationResult.Fail(CircleResult.NotFound);

        var author =
            (
                await db
                    .Characters.AsNoTracking()
                    .Where(x => x.Id == actorCharacterId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(ct)
            ) ?? string.Empty;

        circle.Message = Truncate(message, 751);
        circle.Mark = Truncate(author, 37);
        circle.MessageDate = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
        if (circle.MessageDate.Length > 20)
            circle.MessageDate = circle.MessageDate[..20];
        circle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CircleOperationResult.Success(circle);
    }

    public CircleData ToCircleData(Circle circle)
    {
        // Older rows stored MarkId.ToString() in Mark; treat that as empty author.
        var author = circle.Mark;
        if (author == circle.MarkId.ToString())
            author = string.Empty;

        return new(checked((ulong)circle.Id), circle.Name, circle.MarkId)
        {
            AuthorName = author,
            Date = circle.MessageDate,
            Message = circle.Message,
        };
    }

    private async Task ClearLegacyCircleIdIfNeededAsync(
        int characterId,
        int circleId,
        CancellationToken ct
    )
    {
        var character = await db.Characters.FirstOrDefaultAsync(x => x.Id == characterId, ct);
        if (character is null || character.CircleId != circleId)
            return;

        var next = await db
            .CircleMembers.Where(x => x.CharacterId == characterId && x.CircleId != circleId)
            .OrderBy(x => x.JoinedAt)
            .Select(x => (int?)x.CircleId)
            .FirstOrDefaultAsync(ct);
        character.CircleId = next;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
