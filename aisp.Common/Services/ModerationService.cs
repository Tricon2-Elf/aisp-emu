using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Services;

public enum ModerationError
{
    None,
    TargetNotFound,
    PermissionDenied,
    CannotTargetSelf,
    InvalidDuration,
    AlreadyModerator,
    NotModerator,
    InvalidRoleChange,
}

public sealed class ModerationService(
    IUserRepository userRepo,
    ICharacterRepository characterRepo,
    ICircleRepository circles,
    MainContext db,
    SharedState state,
    ILogger<ModerationService> logger
)
{
    public const string ModeratorsCircleName = "Moderators";
    public const int DefaultKickMinutes = 5;
    public const int MaxKickMinutes = 15;
    public const int DefaultBanDays = 1;
    public const int MaxModeratorBanDays = 30;

    private static readonly ServerType[] AllServerTypes =
    [
        ServerType.Auth,
        ServerType.Msg,
        ServerType.Area,
    ];

    public readonly record struct BanDurationResult(DateTime? BannedUntil, ModerationError Error)
    {
        public bool IsPermanent => Error == ModerationError.None && BannedUntil is null;
    }

    public static BanDurationResult ResolveBanDuration(
        UserRole actorRole,
        int? days,
        bool bypassLimits = false
    )
    {
        if (bypassLimits)
            return ResolveBanDurationUnlimited(days);

        if (days == 0)
        {
            if (actorRole < UserRole.Admin)
                return new BanDurationResult(null, ModerationError.InvalidDuration);

            return new BanDurationResult(null, ModerationError.None);
        }

        var banDays = days ?? DefaultBanDays;
        if (actorRole == UserRole.Moderator)
            banDays = Math.Clamp(banDays, 1, MaxModeratorBanDays);
        else if (banDays < 1)
            return new BanDurationResult(null, ModerationError.InvalidDuration);

        return new BanDurationResult(DateTime.UtcNow.AddDays(banDays), ModerationError.None);
    }

    private static BanDurationResult ResolveBanDurationUnlimited(int? days)
    {
        if (days == 0)
            return new BanDurationResult(null, ModerationError.None);

        var banDays = days ?? DefaultBanDays;
        if (banDays < 1)
            return new BanDurationResult(null, ModerationError.InvalidDuration);

        return new BanDurationResult(DateTime.UtcNow.AddDays(banDays), ModerationError.None);
    }

    public async Task<User?> ResolveTargetUserAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmed = name.Trim();
        if (int.TryParse(trimmed, out var userId) && userId > 0)
        {
            var user = await userRepo.GetById(userId);
            if (user is not null)
                return user;
        }

        var character = await characterRepo.GetByNameAsync(trimmed, ct);
        if (character is not null)
            return await userRepo.GetById(character.UserId);

        return await userRepo.GetByUsernameAsync(trimmed);
    }

    public async Task<(ModerationError Error, User? Actor, User? Target)> ValidateStaffActionAsync(
        int actorUserId,
        string targetName,
        bool requireKickOrBan,
        CancellationToken ct = default
    )
    {
        var actor = await userRepo.GetById(actorUserId);
        if (actor is null)
            return (ModerationError.PermissionDenied, null, null);

        if (!requireKickOrBan && !actor.Role.CanAssignModerator())
            return (ModerationError.PermissionDenied, actor, null);

        if (requireKickOrBan && !actor.Role.CanKickOrBan())
            return (ModerationError.PermissionDenied, actor, null);

        var target = await ResolveTargetUserAsync(targetName, ct);
        if (target is null)
            return (ModerationError.TargetNotFound, actor, null);

        if (target.Id == actor.Id)
            return (ModerationError.CannotTargetSelf, actor, target);

        if (!actor.Role.CanActOn(target.Role))
            return (ModerationError.PermissionDenied, actor, target);

        return (ModerationError.None, actor, target);
    }

    public async Task<(ModerationError Error, int SessionsClosed)> KickAsync(
        int actorUserId,
        string targetName,
        int? minutes = null,
        string? reason = null,
        bool bypassHierarchy = false,
        CancellationToken ct = default
    )
    {
        User? target;
        if (bypassHierarchy)
        {
            target = await ResolveTargetUserAsync(targetName, ct);
            if (target is null)
                return (ModerationError.TargetNotFound, 0);
        }
        else
        {
            var (error, _, resolvedTarget) = await ValidateStaffActionAsync(
                actorUserId,
                targetName,
                requireKickOrBan: true,
                ct
            );
            if (error != ModerationError.None)
                return (error, 0);
            target = resolvedTarget!;
        }

        var kickMinutes = ClampKickMinutes(minutes ?? DefaultKickMinutes);
        var kickedUntil = DateTime.UtcNow.AddMinutes(kickMinutes);
        await userRepo.SetKickedUntilAsync(target.Id, kickedUntil, ct);

        logger.LogInformation(
            "User {TargetUsername} kicked until {KickedUntil}. Actor: {ActorUserId}. Reason: {Reason}",
            target.Username,
            kickedUntil,
            actorUserId,
            reason ?? "No reason provided"
        );

        var sessionsClosed = await DisconnectUserAsync(target, ct);
        return (ModerationError.None, sessionsClosed);
    }

    public async Task<(ModerationError Error, int SessionsClosed)> BanAsync(
        int actorUserId,
        string targetName,
        int? days = null,
        string? reason = null,
        bool bypassHierarchy = false,
        CancellationToken ct = default
    )
    {
        User? target;
        UserRole actorRole = UserRole.ServerAdmin;
        if (bypassHierarchy)
        {
            target = await ResolveTargetUserAsync(targetName, ct);
            if (target is null)
                return (ModerationError.TargetNotFound, 0);
        }
        else
        {
            var (error, actor, resolvedTarget) = await ValidateStaffActionAsync(
                actorUserId,
                targetName,
                requireKickOrBan: true,
                ct
            );
            if (error != ModerationError.None)
                return (error, 0);
            actorRole = actor!.Role;
            target = resolvedTarget!;
        }

        var duration = ResolveBanDuration(actorRole, days, bypassHierarchy);
        if (duration.Error != ModerationError.None)
            return (duration.Error, 0);

        await userRepo.SetBannedAsync(target.Id, true, reason, duration.BannedUntil);

        if (duration.IsPermanent)
        {
            logger.LogInformation(
                "User {TargetUsername} permanently banned. Actor: {ActorUserId}. Reason: {Reason}",
                target.Username,
                actorUserId,
                reason ?? "No reason provided"
            );
        }
        else
        {
            logger.LogInformation(
                "User {TargetUsername} banned until {BannedUntil}. Actor: {ActorUserId}. Reason: {Reason}",
                target.Username,
                duration.BannedUntil,
                actorUserId,
                reason ?? "No reason provided"
            );
        }

        var sessionsClosed = await DisconnectUserAsync(target, ct);
        return (ModerationError.None, sessionsClosed);
    }

    public async Task<ModerationError> UnbanAsync(
        int actorUserId,
        string targetName,
        bool bypassHierarchy = false,
        CancellationToken ct = default
    )
    {
        if (!bypassHierarchy)
        {
            var (error, _, _) = await ValidateStaffActionAsync(
                actorUserId,
                targetName,
                requireKickOrBan: true,
                ct
            );
            if (error != ModerationError.None)
                return error;
        }

        var target = await ResolveTargetUserAsync(targetName, ct);
        if (target is null)
            return ModerationError.TargetNotFound;

        if (!bypassHierarchy && actorUserId == target.Id)
            return ModerationError.CannotTargetSelf;

        await userRepo.SetBannedAsync(target.Id, false);
        logger.LogInformation(
            "User {TargetUsername} unbanned by actor {ActorUserId}",
            target.Username,
            actorUserId
        );
        return ModerationError.None;
    }

    public async Task<ModerationError> PromoteToModeratorAsync(
        int actorUserId,
        string targetName,
        CancellationToken ct = default
    )
    {
        var (error, _, target) = await ValidateStaffActionAsync(
            actorUserId,
            targetName,
            requireKickOrBan: false,
            ct
        );
        if (error != ModerationError.None)
            return error;

        if (target!.Role >= UserRole.Moderator)
            return ModerationError.AlreadyModerator;

        await userRepo.SetRoleAsync(target.Id, UserRole.Moderator, ct);
        await SyncModeratorsCircleForUserAsync(target.Id, ct);
        logger.LogInformation(
            "User {TargetUsername} promoted to Moderator by actor {ActorUserId}",
            target.Username,
            actorUserId
        );
        return ModerationError.None;
    }

    public async Task<ModerationError> DemoteFromModeratorAsync(
        int actorUserId,
        string targetName,
        CancellationToken ct = default
    )
    {
        var (error, _, target) = await ValidateStaffActionAsync(
            actorUserId,
            targetName,
            requireKickOrBan: false,
            ct
        );
        if (error != ModerationError.None)
            return error;

        if (target!.Role != UserRole.Moderator)
            return ModerationError.NotModerator;

        await userRepo.SetRoleAsync(target.Id, UserRole.User, ct);
        await SyncModeratorsCircleForUserAsync(target.Id, ct);
        logger.LogInformation(
            "User {TargetUsername} demoted from Moderator by actor {ActorUserId}",
            target.Username,
            actorUserId
        );
        return ModerationError.None;
    }

    public async Task<ModerationError> ResetPasswordAsync(
        int actorUserId,
        int targetUserId,
        string newPassword,
        CancellationToken ct = default
    )
    {
        var actor = await userRepo.GetById(actorUserId);
        if (actor is null)
            return ModerationError.PermissionDenied;

        if (!actor.Role.CanKickOrBan())
            return ModerationError.PermissionDenied;

        var target = await userRepo.GetById(targetUserId);
        if (target is null)
            return ModerationError.TargetNotFound;

        if (target.Id == actor.Id)
            return ModerationError.CannotTargetSelf;

        if (!actor.Role.CanActOn(target.Role))
            return ModerationError.PermissionDenied;

        await userRepo.UpdatePasswordAsync(targetUserId, newPassword);
        logger.LogInformation(
            "Password reset for user {TargetUsername} by actor {ActorUserId}",
            target.Username,
            actorUserId
        );
        return ModerationError.None;
    }

    public async Task<ModerationError> SetRoleAsync(
        int actorUserId,
        int targetUserId,
        UserRole newRole,
        CancellationToken ct = default
    )
    {
        var actor = await userRepo.GetById(actorUserId);
        var target = await userRepo.GetById(targetUserId);
        if (actor is null || target is null)
            return ModerationError.TargetNotFound;

        if (target.Id == actor.Id)
            return ModerationError.CannotTargetSelf;

        if (!actor.Role.CanSetRole(target.Role, newRole))
            return ModerationError.InvalidRoleChange;

        await userRepo.SetRoleAsync(target.Id, newRole, ct);
        await SyncModeratorsCircleForUserAsync(target.Id, ct);
        logger.LogInformation(
            "User {TargetUsername} role changed to {NewRole} by actor {ActorUserId}",
            target.Username,
            newRole,
            actorUserId
        );
        return ModerationError.None;
    }

    public async Task<int> DisconnectUserAsync(User user, CancellationToken ct = default)
    {
        var matchingSessions = new List<IPlayerSession>();

        foreach (var serverType in AllServerTypes)
        {
            foreach (var session in state.GetServerClients(serverType))
            {
                if (session.UserId == user.Id)
                    matchingSessions.Add(session);
            }
        }

        var logoutData = new LogoutResponse().ToBytes();

        foreach (var session in matchingSessions)
        {
            try
            {
                await session.SendAsync(PacketType.LogoutResponse, logoutData, ct);
                await Task.Delay(500, ct);

                if (session is PlayerSession ps)
                    ps.ClientConnection.Stream.Close();
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Error disconnecting session {ConnectionId} for user {Username}",
                    session.ConnectionId,
                    user.Username
                );
            }
        }

        return matchingSessions.Count;
    }

    public async Task<int> DisconnectUserByIdAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetById(userId);
        if (user is null)
            return 0;

        return await DisconnectUserAsync(user, ct);
    }

    public static bool IsModeratorsCircle(string? name) =>
        string.Equals(name, ModeratorsCircleName, StringComparison.Ordinal);

    public async Task SyncAllStaffCirclesAsync(CancellationToken ct = default)
    {
        var circle = await EnsureModeratorsCircleExistsAsync(ct);
        if (circle is null)
            return;

        var staffUsers = await db
            .Users.AsNoTracking()
            .Where(user => user.Role >= UserRole.Moderator)
            .Select(user => user.Id)
            .ToListAsync(ct);

        foreach (var userId in staffUsers)
            await EnsureUserInModeratorsCircleAsync(userId, circle.Id, ct);

        var memberCharacterIds = await db
            .CircleMembers.AsNoTracking()
            .Where(member => member.CircleId == circle.Id)
            .Select(member => member.CharacterId)
            .ToListAsync(ct);

        foreach (var characterId in memberCharacterIds)
        {
            var role = await db
                .Characters.AsNoTracking()
                .Where(character => character.Id == characterId)
                .Join(
                    db.Users.AsNoTracking(),
                    character => character.UserId,
                    user => user.Id,
                    (_, user) => user.Role
                )
                .FirstOrDefaultAsync(ct);

            if (role >= UserRole.Moderator)
                continue;

            await RemoveCharacterFromModeratorsCircleAsync(circle, characterId, ct);
        }
    }

    public async Task SyncModeratorsCircleForUserAsync(int userId, CancellationToken ct = default)
    {
        var role = await db
            .Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Role)
            .FirstOrDefaultAsync(ct);

        if (role >= UserRole.Moderator)
        {
            var circle = await EnsureModeratorsCircleExistsAsync(ct);
            if (circle is not null)
                await EnsureUserInModeratorsCircleAsync(userId, circle.Id, ct);
            return;
        }

        var moderatorsCircle = await circles.GetByNameAsync(ModeratorsCircleName, ct);
        if (moderatorsCircle is null)
            return;

        var characterIds = await db
            .Characters.AsNoTracking()
            .Where(character => character.UserId == userId)
            .Select(character => character.Id)
            .ToListAsync(ct);

        foreach (var characterId in characterIds)
            await RemoveCharacterFromModeratorsCircleAsync(moderatorsCircle, characterId, ct);
    }

    public static int ClampKickMinutes(int minutes) => Math.Clamp(minutes, 1, MaxKickMinutes);

    public static int ClampModeratorBanDays(int days) =>
        Math.Clamp(days, 1, MaxModeratorBanDays);

    public static bool IsPermanentBanToken(string token) =>
        token.Equals("perma", StringComparison.OrdinalIgnoreCase)
        || token.Equals("permanent", StringComparison.OrdinalIgnoreCase)
        || token == "0";

    private async Task<Circle?> EnsureModeratorsCircleExistsAsync(CancellationToken ct = default)
    {
        var existing = await circles.GetByNameAsync(ModeratorsCircleName, ct);
        if (existing is not null)
            return existing;

        var leaderCharacterId = await FindFirstSystemAdminCharacterIdAsync(ct);
        if (leaderCharacterId is null)
        {
            logger.LogDebug(
                "Moderators circle not created yet: no system admin with a character exists"
            );
            return null;
        }

        var result = await circles.CreateAsync(leaderCharacterId.Value, ModeratorsCircleName, 0, ct);
        if (result.Result != CircleResult.Ok || result.Circle is null)
        {
            logger.LogWarning(
                "Failed to create Moderators circle for leader character {CharacterId}: {Result}",
                leaderCharacterId.Value,
                result.Result
            );
            return null;
        }

        logger.LogInformation(
            "Created Moderators circle {CircleId} with leader character {CharacterId}",
            result.Circle.Id,
            leaderCharacterId.Value
        );
        return result.Circle;
    }

    private async Task<int?> FindFirstSystemAdminCharacterIdAsync(CancellationToken ct = default)
    {
        var userId = await db
            .Users.AsNoTracking()
            .Where(user => user.Role >= UserRole.ServerAdmin)
            .OrderBy(user => user.Id)
            .Select(user => (int?)user.Id)
            .FirstOrDefaultAsync(ct);

        if (userId is null)
        {
            userId = await db
                .Users.AsNoTracking()
                .Where(user => user.Role >= UserRole.Admin)
                .OrderBy(user => user.Id)
                .Select(user => (int?)user.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (userId is null)
            return null;

        return await db
            .Characters.AsNoTracking()
            .Where(character => character.UserId == userId.Value)
            .OrderBy(character => character.Id)
            .Select(character => (int?)character.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task EnsureUserInModeratorsCircleAsync(
        int userId,
        int circleId,
        CancellationToken ct
    )
    {
        var characterIds = await db
            .Characters.AsNoTracking()
            .Where(character => character.UserId == userId)
            .Select(character => character.Id)
            .ToListAsync(ct);

        foreach (var characterId in characterIds)
        {
            var result = await circles.EnsureMemberDirectAsync(
                circleId,
                characterId,
                bypassMembershipLimit: true,
                ct: ct
            );
            if (result.Result != CircleResult.Ok)
            {
                logger.LogWarning(
                    "Failed to add character {CharacterId} to Moderators circle: {Result}",
                    characterId,
                    result.Result
                );
            }
        }
    }

    private async Task RemoveCharacterFromModeratorsCircleAsync(
        Circle circle,
        int characterId,
        CancellationToken ct
    )
    {
        if (circle.LeaderCharacterId == characterId)
        {
            var successorId = await FindModeratorsCircleLeadershipSuccessorAsync(
                circle.Id,
                characterId,
                ct
            );
            if (successorId is null)
            {
                logger.LogWarning(
                    "Cannot remove character {CharacterId} from Moderators circle: no successor leader",
                    characterId
                );
                return;
            }

            var transfer = await circles.TransferLeadershipAsync(circle.Id, successorId.Value, ct);
            if (transfer.Result != CircleResult.Ok)
            {
                logger.LogWarning(
                    "Failed to transfer Moderators circle leadership to {CharacterId}: {Result}",
                    successorId.Value,
                    transfer.Result
                );
                return;
            }
        }

        var result = await circles.RemoveMemberDirectAsync(circle.Id, characterId, ct);
        if (result.Result != CircleResult.Ok)
        {
            logger.LogWarning(
                "Failed to remove character {CharacterId} from Moderators circle: {Result}",
                characterId,
                result.Result
            );
        }
    }

    private async Task<int?> FindModeratorsCircleLeadershipSuccessorAsync(
        int circleId,
        int currentLeaderCharacterId,
        CancellationToken ct
    )
    {
        var preferredLeader = await FindFirstSystemAdminCharacterIdAsync(ct);
        if (
            preferredLeader is not null
            && preferredLeader.Value != currentLeaderCharacterId
            && await db.CircleMembers.AnyAsync(
                member =>
                    member.CircleId == circleId && member.CharacterId == preferredLeader.Value,
                ct
            )
        )
            return preferredLeader;

        return await db
            .CircleMembers.AsNoTracking()
            .Where(member =>
                member.CircleId == circleId && member.CharacterId != currentLeaderCharacterId
            )
            .Join(
                db.Characters.AsNoTracking(),
                member => member.CharacterId,
                character => character.Id,
                (member, character) => new { member, character.UserId }
            )
            .Join(
                db.Users.AsNoTracking(),
                row => row.UserId,
                user => user.Id,
                (row, user) =>
                    new
                    {
                        row.member.CharacterId,
                        row.member.JoinedAt,
                        user.Role,
                    }
            )
            .Where(row => row.Role >= UserRole.Moderator)
            .OrderByDescending(row => row.Role)
            .ThenBy(row => row.JoinedAt)
            .ThenBy(row => row.CharacterId)
            .Select(row => (int?)row.CharacterId)
            .FirstOrDefaultAsync(ct);
    }
}
