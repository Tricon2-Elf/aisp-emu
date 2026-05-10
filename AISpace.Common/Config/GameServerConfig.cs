namespace AISpace.Common.Config;

public sealed class GameServerConfig
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; }

    /// <summary>Maximum concurrent client handler tasks per TCP listener. Extra connections wait at accept (backpressure).</summary>
    public int MaxConcurrentClients { get; set; } = 1024;
}
