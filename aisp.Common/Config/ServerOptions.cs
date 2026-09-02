namespace aisp.Common.Config;

public class ServerOptions
{
    /// <summary>When set (e.g. via IP_OVERRIDE in Docker), replaces "localhost" and "127.0.0.1" in all addresses sent to clients.</summary>
    public string? IPOverride { get; set; }
    public NetworkOptions NetworkOptions { get; set; } = new();
    public DbOptions DbOptions { get; set; } = new();
    public NicoLiveOptions NicoLive { get; set; } = new();

    /// <summary>Bounded capacity for each game server's packet dispatch channel (Auth, Msg, Area). Producers wait when full.</summary>
    public int PacketChannelCapacity { get; set; } = 512;

    /// <summary>Maximum concurrent client handler tasks per TCP listener for Auth, Msg, and Area. Extra connections wait at accept (backpressure).</summary>
    public int MaxConcurrentClients { get; set; } = 32;

    /// <summary>Maximum encrypted receive frame plaintext size in bytes. Frames larger than this are rejected before allocation.</summary>
    public int MaxReceiveFrameSize { get; set; } = 4096;

    /// <summary>Idle read timeout in seconds for client TCP connections. Handlers are released when no data arrives within this window.</summary>
    public int ClientReadTimeoutSeconds { get; set; } = 300;

    /// <summary>Per-packet outbound write timeout in seconds. Slow clients that cannot accept a packet (including the ~2MB item catalog) within this window are disconnected.</summary>
    public int ClientSendTimeoutSeconds { get; set; } = 30;

    /// <summary>When true, TCP_NODELAY is set on accepted clients (disables Nagle).</summary>
    public bool TcpNoDelay { get; set; } = true;

    /// <summary>When true, TCP keepalive probes are enabled on accepted clients.</summary>
    public bool TcpKeepAlive { get; set; } = true;

    /// <summary>Idle seconds before the first keepalive probe.</summary>
    public int TcpKeepAliveIdleSeconds { get; set; } = 45;

    /// <summary>Seconds between keepalive probes.</summary>
    public int TcpKeepAliveIntervalSeconds { get; set; } = 10;

    /// <summary>Failed keepalive probes before the kernel drops the socket.</summary>
    public int TcpKeepAliveRetryCount { get; set; } = 3;

    /// <summary>When false, session presence is kept in-memory (single-node VPS). When true, SessionPresences table is used (multi-instance).</summary>
    public bool UseDistributedSessionPresence { get; set; } = false;

    /// <summary>Answered to get_ai_upload_rate. On the original service this was the author's share, in percent of the sale price in デレ (the in-game currency), for user-made aiちゅーん (AI tune) uploaded to the shop. The client shows sale price * rate / 100 as 「1冊あたりの収益」.</summary>
    public int AiUploadRatePercent { get; set; } = 70;

    /// <summary>Answered to get_adventure_upload_rate. On the original service this was the author's share, in percent of the sale price in デレ (the in-game currency), for user-made drama (adventure) discs uploaded to the shop. The client shows sale price * rate / 100 as 「1冊あたりの収益」.</summary>
    public int AdventureUploadRatePercent { get; set; } = 70;

    /// <summary>Price of one 原稿用紙 (manuscript sheet) in デレ (the in-game currency) at the sheet shop the drama editor's 通販 button opens.</summary>
    public long AdventureSheetPriceAi { get; set; } = 10;

    public GameServerConfig AuthServer { get; set; } = new() { Port = 50050 };
    public GameServerConfig MsgServer { get; set; } = new() { Port = 50052 };
    public GameServerConfig AreaServer { get; set; } = new() { Port = 50054 };

    public HealthCheckOptions HealthCheck { get; set; } = new();

    /// <summary>Returns the address to use for clients: if IPOverride is set and address is localhost/127.0.0.1, returns IPOverride; otherwise returns address.</summary>
    public string ResolveAddress(string address)
    {
        if (string.IsNullOrEmpty(IPOverride))
            return address;
        if (address == "localhost" || address == "127.0.0.1")
            return IPOverride;
        return address;
    }

    public aisp.Network.TcpSocketOptions ToTcpSocketOptions() =>
        new()
        {
            NoDelay = TcpNoDelay,
            KeepAlive = TcpKeepAlive,
            KeepAliveIdleSeconds = Math.Max(1, TcpKeepAliveIdleSeconds),
            KeepAliveIntervalSeconds = Math.Max(1, TcpKeepAliveIntervalSeconds),
            KeepAliveRetryCount = Math.Max(1, TcpKeepAliveRetryCount),
        };

    public bool AuthServerEnabled => AuthServer.Enabled;
    public bool MsgServerEnabled => MsgServer.Enabled;
    public bool AreaServerEnabled => AreaServer.Enabled;
}
