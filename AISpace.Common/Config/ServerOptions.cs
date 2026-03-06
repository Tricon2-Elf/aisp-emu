namespace AISpace.Common.Config;

public class ServerOptions
{
    /// <summary>When set (e.g. via IP_OVERRIDE in Docker), replaces "localhost" and "127.0.0.1" in all addresses sent to clients.</summary>
    public string? IPOverride { get; set; }
    public required NetworkOptions NetworkOptions { get; set; }
    public required DbOptions DbOptions { get; set; }
    public bool AuthServerEnabled { get; set; } = true;
    public bool MsgServerEnabled { get; set; } = true;
    public bool AreaServerEnabled { get; set; } = true;

    /// <summary>Returns the address to use for clients: if IPOverride is set and address is localhost/127.0.0.1, returns IPOverride; otherwise returns address.</summary>
    public string ResolveAddress(string address)
    {
        if (string.IsNullOrEmpty(IPOverride)) return address;
        if (address == "localhost" || address == "127.0.0.1") return IPOverride;
        return address;
    }
}
