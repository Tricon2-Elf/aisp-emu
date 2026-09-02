using aisp.Common;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Network;
using aisp.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace aisp.Server.Services;

public class UserAdminService
{
    private readonly IUserRepository _userRepo;
    private readonly ModerationService _moderation;
    private readonly SharedState _state;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        IUserRepository userRepo,
        ModerationService moderation,
        SharedState state,
        ILogger<UserAdminService> logger
    )
    {
        _userRepo = userRepo;
        _moderation = moderation;
        _state = state;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, UserDetail? User)> CreateUserAsync(
        string username,
        string password,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "username is required", null);
        if (string.IsNullOrWhiteSpace(password))
            return (false, "password is required", null);

        var existing = await _userRepo.GetByUsernameAsync(username);
        if (existing != null)
            return (false, "username already exists", null);

        await _userRepo.AddAsync(username, password);
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "failed to create user", null);

        _logger.LogInformation(
            "API created user {Username} (ID: {UserId})",
            user.Username,
            user.Id
        );
        return (true, null, MapToDetail(user));
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(
        string username,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found");

        await _moderation.DisconnectUserAsync(user, ct);

        await _userRepo.DeleteAsync(user.Id);
        _logger.LogInformation(
            "API deleted user {Username} (ID: {UserId})",
            user.Username,
            user.Id
        );
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        string username,
        string newPassword,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            return (false, "newPassword is required");

        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found");

        await _userRepo.UpdatePasswordAsync(user.Id, newPassword);
        _logger.LogInformation("API reset password for {Username}", username);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int SessionsKicked)> BanUserAsync(
        string username,
        string? reason,
        int? days = null,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found", 0);

        var (error, sessionsKicked) = await _moderation.BanAsync(
            0,
            username,
            days,
            reason,
            bypassHierarchy: true,
            ct: ct
        );
        if (error != ModerationError.None)
            return (false, error.ToString(), 0);

        return (true, null, sessionsKicked);
    }

    public async Task<(bool Success, string? Error)> UnbanUserAsync(
        string username,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found");

        await _userRepo.SetBannedAsync(user.Id, false);
        _logger.LogInformation("API unbanned user {Username}", username);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int SessionsClosed)> KickUserAsync(
        string username,
        int? minutes = null,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found", 0);

        var kickMinutes = ModerationService.ClampKickMinutes(
            minutes ?? ModerationService.DefaultKickMinutes
        );
        var kickedUntil = DateTime.UtcNow.AddMinutes(kickMinutes);
        await _userRepo.SetKickedUntilAsync(user.Id, kickedUntil, ct);

        var sessionsClosed = await _moderation.DisconnectUserAsync(user, ct);
        _logger.LogInformation(
            "API kicked user {Username} until {KickedUntil}, closed {Count} sessions",
            username,
            kickedUntil,
            sessionsClosed
        );
        return (true, null, sessionsClosed);
    }

    public async Task<(bool Success, string? Error)> SetRoleAsync(
        string username,
        UserRole role,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found");

        await _userRepo.SetRoleAsync(user.Id, role, ct);
        await _moderation.SyncModeratorsCircleForUserAsync(user.Id, ct);
        _logger.LogInformation("API set role for {Username} to {Role}", username, role);
        return (true, null);
    }

    public async Task<(IReadOnlyList<UserSummary> Users, int Total)> ListUsersAsync(
        string? search = null,
        int? skip = null,
        int? take = null,
        CancellationToken ct = default
    )
    {
        var users = await _userRepo.GetAllAsync(search, skip, take);
        var total = await _userRepo.CountAsync(search);

        var summaries = users
            .Select(u => new UserSummary
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                IsBanned = UserModerationState.IsCurrentlyBanned(u),
                BannedUntil = u.BannedUntil,
                KickedUntil = u.KickedUntil,
                CreatedAt = u.CreatedAt,
                CharacterCount = u.Characters.Count,
            })
            .ToList();

        return (summaries, total);
    }

    public async Task<UserDetail?> GetUserDetailAsync(
        string username,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        return user == null ? null : MapToDetail(user);
    }

    public ConnectedClient[] GetConnectedClients()
    {
        var clients = new List<ConnectedClient>();

        foreach (var serverType in new[] { ServerType.Auth, ServerType.Msg, ServerType.Area })
        {
            var serverName = serverType.ToString();
            foreach (var session in _state.GetServerClients(serverType))
            {
                if (session.IsAuthenticated)
                {
                    clients.Add(
                        new ConnectedClient
                        {
                            Username = session.User?.Username ?? "",
                            Server = serverName,
                            CharacterName = session.Character?.Name,
                            MapId = session.MapId,
                            ChannelId = session.ChannelId,
                        }
                    );
                }
            }
        }

        return clients.ToArray();
    }

    public async Task<StatsResponse> GetStatsAsync(CancellationToken ct = default)
    {
        var totalUsers = await _userRepo.CountAsync();
        var onlineCount =
            _state.GetServerClients(ServerType.Auth).Count(s => s.IsAuthenticated)
            + _state.GetServerClients(ServerType.Msg).Count(s => s.IsAuthenticated)
            + _state.GetServerClients(ServerType.Area).Count(s => s.IsAuthenticated);

        var uptimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _state.StartTimeUnix;

        return new StatsResponse
        {
            TotalUsers = totalUsers,
            OnlineCount = onlineCount,
            UptimeSeconds = uptimeSeconds,
            ClientsPerServer = new Dictionary<string, int>
            {
                ["auth"] = _state.GetServerClients(ServerType.Auth).Count(s => s.IsAuthenticated),
                ["msg"] = _state.GetServerClients(ServerType.Msg).Count(s => s.IsAuthenticated),
                ["area"] = _state.GetServerClients(ServerType.Area).Count(s => s.IsAuthenticated),
            },
        };
    }

    private static UserDetail MapToDetail(User user)
    {
        return new UserDetail
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            IsBanned = UserModerationState.IsCurrentlyBanned(user),
            BanReason = user.BanReason,
            CreatedAt = user.CreatedAt,
            BannedAt = user.BannedAt,
            BannedUntil = user.BannedUntil,
            KickedUntil = user.KickedUntil,
            AiPoints = user.AiPoints,
            NicoPoints = user.NicoPoints,
            CharacterCount = user.Characters.Count,
            Characters = user
                .Characters.Select(c => new CharacterSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    ModelId = c.ModelId,
                })
                .ToList(),
        };
    }
}
