using System.Net;
using System.Net.Sockets;

namespace aisp.Network.Tests;

public class TcpSocketTuningTests
{
    [Fact]
    public async Task Apply_EnablesNoDelayAndKeepAlive()
    {
        var (accepted, peer) = await AcceptPairAsync();
        using (accepted)
        using (peer)
        {
            TcpSocketTuning.Apply(accepted.Client, TcpSocketOptions.Default);

            Assert.True(accepted.Client.NoDelay);
            Assert.NotEqual(
                0,
                ToInt(
                    accepted.Client.GetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.KeepAlive
                    )
                )
            );
        }
    }

    [Fact]
    public async Task Apply_CanDisableNoDelayAndKeepAlive()
    {
        var (accepted, peer) = await AcceptPairAsync();
        using (accepted)
        using (peer)
        {
            TcpSocketTuning.Apply(
                accepted.Client,
                new TcpSocketOptions { NoDelay = false, KeepAlive = false }
            );

            Assert.False(accepted.Client.NoDelay);
            Assert.Equal(
                0,
                ToInt(
                    accepted.Client.GetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.KeepAlive
                    )
                )
            );
        }
    }

    private static int ToInt(object? value) =>
        value is bool flag ? (flag ? 1 : 0) : Convert.ToInt32(value);

    private static async Task<(TcpClient Accepted, TcpClient Peer)> AcceptPairAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var peer = new TcpClient();
            var connect = peer.ConnectAsync(
                IPAddress.Loopback,
                ((IPEndPoint)listener.LocalEndpoint).Port
            );
            var accepted = await listener.AcceptTcpClientAsync(
                TestContext.Current.CancellationToken
            );
            await connect.WaitAsync(TestContext.Current.CancellationToken);
            return (accepted, peer);
        }
        finally
        {
            listener.Stop();
        }
    }
}
