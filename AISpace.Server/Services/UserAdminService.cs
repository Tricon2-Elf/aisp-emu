using AISpace.Common;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Server.Services;

public class UserAdminService
{
    private readonly IUserRepository _userRepo;
    private readonly SharedState _state;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(IUserRepository userRepo, SharedState state, ILogger<UserAdminService> logger)
    {
        _userRepo = userRepo;
        _state = state;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, UserDetail? User)> CreateUserAsync(string username, string password, CancellationToken ct = default)
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

        _logger.LogInformation("API created user {Username} (ID: {UserId})", user.Username, user.Id);
        return (true, null, MapToDetail(user));
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(string username, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found");

        await KickUserAsync(user, ct);

        await _userRepo.DeleteAsync(user.Id);
        _logger.LogInformation("API deleted user {Username} (ID: {UserId})", user.Username, user.Id);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string username, string newPassword, CancellationToken ct = default)
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

    public async Task<(bool Success, string? Error, int SessionsKicked)> BanUserAsync(string username, string? reason, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found", 0);

        await _userRepo.SetBannedAsync(user.Id, true, reason);
        _logger.LogInformation("API banned user {Username}. Reason: {Reason}", username, reason);

        var sessionsKicked = await KickUserAsync(user, ct);
        return (true, null, sessionsKicked);
    }

    public async Task<(bool Success, string? Error)> UnbanUserAsync(string username, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found");

        await _userRepo.SetBannedAsync(user.Id, false);
        _logger.LogInformation("API unbanned user {Username}", username);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int SessionsClosed)> KickUserAsync(string username, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return (false, "user not found", 0);

        var sessionsClosed = await KickUserAsync(user, ct);
        _logger.LogInformation("API kicked user {Username}, closed {Count} sessions", username, sessionsClosed);
        return (true, null, sessionsClosed);
    }

    private async Task<int> KickUserAsync(User user, CancellationToken ct)
    {
        var serverTypes = new[] { ServerType.Auth, ServerType.Msg, ServerType.Area };
        var matchingSessions = new List<IPlayerSession>();

        foreach (var serverType in serverTypes)
        {
            foreach (var session in _state.GetServerClients(serverType))
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
                _logger.LogWarning(ex, "Error disconnecting session {ConnectionId} for user {Username}", session.ConnectionId, user.Username);
            }
        }

        return matchingSessions.Count;
    }

    public async Task<(IReadOnlyList<UserSummary> Users, int Total)> ListUsersAsync(string? search = null, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var users = await _userRepo.GetAllAsync(search, skip, take);
        var total = await _userRepo.CountAsync(search);

        var summaries = users
            .Select(u => new UserSummary
            {
                Id = u.Id,
                Username = u.Username,
                IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt,
                CharacterCount = u.Characters.Count,
            })
            .ToList();

        return (summaries, total);
    }

    public async Task<UserDetail?> GetUserDetailAsync(string username, CancellationToken ct = default)
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
        var onlineCount = _state.GetServerClients(ServerType.Auth).Count(s => s.IsAuthenticated) + _state.GetServerClients(ServerType.Msg).Count(s => s.IsAuthenticated) + _state.GetServerClients(ServerType.Area).Count(s => s.IsAuthenticated);

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
            IsBanned = user.IsBanned,
            BanReason = user.BanReason,
            CreatedAt = user.CreatedAt,
            BannedAt = user.BannedAt,
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
