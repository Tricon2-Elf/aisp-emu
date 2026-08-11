namespace AISpace.Portal;

public sealed class PortalBackendOptions
{
    public const string SectionName = "PortalBackend";

    public string ApiBaseUrl { get; set; } = "http://127.0.0.1:8080";
    public string ServiceToken { get; set; } = string.Empty;
}

public sealed class UserPortalOptions
{
    public const string SectionName = "UserPortal";

    public bool Enabled { get; set; }
}

public sealed class AdminPortalOptions
{
    public const string SectionName = "AdminPortal";

    public bool Enabled { get; set; }
    public string[] AdminUsernames { get; set; } = [];

    public bool IsAdmin(string username) =>
        AdminUsernames.Any(candidate => string.Equals(candidate, username, StringComparison.Ordinal));
}
