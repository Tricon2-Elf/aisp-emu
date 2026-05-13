namespace AISpace.Common.Config;

public class ServerOptions
{
    /// <summary>When set (e.g. via IP_OVERRIDE in Docker), replaces "localhost" and "127.0.0.1" in all addresses sent to clients.</summary>
    public string? IPOverride { get; set; }
    public NetworkOptions NetworkOptions { get; set; } = new();
    public DbOptions DbOptions { get; set; } = new();

    /// <summary>Bounded capacity for each game server's packet dispatch channel (Auth, Msg, Area). Producers wait when full.</summary>
    public int PacketChannelCapacity { get; set; } = 10_000;

    /// <summary>Maximum concurrent client handler tasks per TCP listener for Auth, Msg, and Area. Extra connections wait at accept (backpressure).</summary>
    public int MaxConcurrentClients { get; set; } = 1024;

    public GameServerConfig AuthServer { get; set; } = new() { Port = 50050 };
    public GameServerConfig MsgServer { get; set; } = new() { Port = 50052 };
    public GameServerConfig AreaServer { get; set; } = new() { Port = 50054 };

    /// <summary>Returns the address to use for clients: if IPOverride is set and address is localhost/127.0.0.1, returns IPOverride; otherwise returns address.</summary>
    public string ResolveAddress(string address)
    {
        if (string.IsNullOrEmpty(IPOverride))
            return address;
        if (address == "localhost" || address == "127.0.0.1")
            return IPOverride;
        return address;
    }

    public bool AuthServerEnabled => AuthServer.Enabled;
    public bool MsgServerEnabled => MsgServer.Enabled;
    public bool AreaServerEnabled => AreaServer.Enabled;
}
