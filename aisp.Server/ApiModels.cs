namespace aisp.Server;

public record BroadcastRequest
{
    public string Message { get; init; } = "";
}

public record CreateUserRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; } = "";
}

public record BanRequest
{
    public string? Reason { get; init; }
    public int? Days { get; init; }
}

public record KickRequest
{
    public int? Minutes { get; init; }
}

public record SetRoleRequest
{
    public aisp.Common.Game.UserRole Role { get; init; }
}

public record UserSummary
{
    public int Id { get; init; }
    public string Username { get; init; } = "";
    public aisp.Common.Game.UserRole Role { get; init; }
    public bool IsBanned { get; init; }
    public DateTime? BannedUntil { get; init; }
    public DateTime? KickedUntil { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CharacterCount { get; init; }
}

public record CharacterSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public uint ModelId { get; init; }
}

public record UserDetail
{
    public int Id { get; init; }
    public string Username { get; init; } = "";
    public aisp.Common.Game.UserRole Role { get; init; }
    public bool IsBanned { get; init; }
    public string? BanReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? BannedAt { get; init; }
    public DateTime? BannedUntil { get; init; }
    public DateTime? KickedUntil { get; init; }
    public long AiPoints { get; init; }
    public long NicoPoints { get; init; }
    public int CharacterCount { get; init; }
    public List<CharacterSummary> Characters { get; init; } = [];
}

public record ConnectedClient
{
    public string Username { get; init; } = "";
    public string Server { get; init; } = "";
    public string? CharacterName { get; init; }
    public uint MapId { get; init; }
    public int ChannelId { get; init; }
}

public record StatsResponse
{
    public int TotalUsers { get; init; }
    public int OnlineCount { get; init; }
    public long UptimeSeconds { get; init; }
    public Dictionary<string, int> ClientsPerServer { get; init; } = [];
}

public record ChatLogEntryDto
{
    public long Id { get; init; }
    public string Kind { get; init; } = "";
    public int UserId { get; init; }
    public int CharacterId { get; init; }
    public string CharacterName { get; init; } = "";
    public string Message { get; init; } = "";
    public uint DistId { get; init; }
    public uint BalloonId { get; init; }
    public int? CircleId { get; init; }
    public uint? MapId { get; init; }
    public int? ChannelId { get; init; }
    public bool Rejected { get; init; }
    public DateTime CreatedAt { get; init; }
}
