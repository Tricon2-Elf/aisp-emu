namespace aisp.Network;

public sealed record TcpSocketOptions
{
    public static TcpSocketOptions Default { get; } = new();

    public bool NoDelay { get; init; } = true;
    public bool KeepAlive { get; init; } = true;
    public int KeepAliveIdleSeconds { get; init; } = 45;
    public int KeepAliveIntervalSeconds { get; init; } = 10;
    public int KeepAliveRetryCount { get; init; } = 3;
}
