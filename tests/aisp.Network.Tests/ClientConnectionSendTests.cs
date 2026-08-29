using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Network.Tests;

public class ClientConnectionSendTests
{
    [Fact]
    public async Task SendAsync_ReturnsBeforePeerReads()
    {
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.encrypted = false;
            var payload = new byte[1024];

            var elapsed = Stopwatch.StartNew();
            for (var i = 0; i < 64; i++)
                await context.SendAsync(
                    PacketType.TalkForwardNotify,
                    payload,
                    TestContext.Current.CancellationToken
                );

            Assert.True(
                elapsed.Elapsed < TimeSpan.FromSeconds(1),
                $"SendAsync blocked for {elapsed.Elapsed}; outbound writes must not stall the caller"
            );
        }
    }

    [Fact]
    public async Task SendAsync_DeliversUnencryptedPacket()
    {
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.encrypted = false;
            var payload = new byte[] { 0x11, 0x22, 0x33, 0x44 };
            await context.SendAsync(
                PacketType.TalkForwardNotify,
                payload,
                TestContext.Current.CancellationToken
            );

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken
            );
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            var buffer = new byte[32];
            var read = await peer.GetStream().ReadAsync(buffer, timeout.Token);
            Assert.True(read >= 7);
            Assert.Equal(0x03, buffer[0]);
            Assert.Equal(
                (uint)(payload.Length + 2),
                BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(1, 4))
            );
            Assert.Equal(
                (ushort)PacketType.TalkForwardNotify,
                BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(5, 2))
            );
        }
    }

    private static async Task<(ClientConnection context, TcpClient peer)> CreateClientContextAsync()
    {
        var acceptor = new TcpListener(IPAddress.Loopback, 0);
        acceptor.Start();
        try
        {
            var peer = new TcpClient();
            await peer.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)acceptor.LocalEndpoint).Port);
            var serverSide = await acceptor.AcceptTcpClientAsync(
                TestContext.Current.CancellationToken
            );
            var context = new ClientConnection(
                Guid.NewGuid(),
                serverSide.Client.RemoteEndPoint!,
                serverSide.GetStream(),
                NullLogger<ClientConnection>.Instance,
                serverSide
            );
            return (context, peer);
        }
        finally
        {
            acceptor.Stop();
        }
    }
}
