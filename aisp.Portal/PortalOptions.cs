namespace aisp.Portal;

public sealed class PortalBackendOptions
{
    public const string SectionName = "PortalBackend";

    public string ApiBaseUrl { get; set; } = "http://127.0.0.1:8080";
    public string ServiceToken { get; set; } = string.Empty;
}

public sealed class PortalOptions
{
    public const string SectionName = "Portal";

    public bool Enabled { get; set; }
    public bool AllowRegistration { get; set; } = true;
    public string[] AdminUsernames { get; set; } = [];

    public bool IsAdmin(string username) =>
        AdminUsernames.Any(candidate =>
            string.Equals(candidate, username, StringComparison.Ordinal)
        );
}
