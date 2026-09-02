using System.Net.Sockets;

namespace aisp.Network;

public static class TcpSocketTuning
{
    public static void Apply(Socket socket, TcpSocketOptions options)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(options);

        socket.NoDelay = options.NoDelay;
        socket.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            options.KeepAlive
        );

        if (!options.KeepAlive)
            return;

        TrySetTcpOption(
            socket,
            SocketOptionName.TcpKeepAliveTime,
            Math.Max(1, options.KeepAliveIdleSeconds)
        );
        TrySetTcpOption(
            socket,
            SocketOptionName.TcpKeepAliveInterval,
            Math.Max(1, options.KeepAliveIntervalSeconds)
        );
        TrySetTcpOption(
            socket,
            SocketOptionName.TcpKeepAliveRetryCount,
            Math.Max(1, options.KeepAliveRetryCount)
        );
    }

    static void TrySetTcpOption(Socket socket, SocketOptionName name, int value)
    {
        try
        {
            socket.SetSocketOption(SocketOptionLevel.Tcp, name, value);
        }
        catch (SocketException)
        {
            // not supported on this platform (e.g. TcpKeepAliveRetryCount on some Windows versions)
        }
    }
}
