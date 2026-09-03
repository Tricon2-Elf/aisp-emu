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

    [Fact]
    public async Task SendAsync_List_PacksSmallPacketsIntoOneUnencryptedFrame()
    {
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.encrypted = false;
            var packets = new List<(PacketType Type, byte[] Payload)>();
            for (var i = 0; i < 30; i++)
                packets.Add((PacketType.ItemCreateNotify, new byte[22]));

            await context.SendAsync(packets, TestContext.Current.CancellationToken);

            var expected = VceCodec.EncodePacketDataFrames(packets);
            Assert.Single(expected);
            Assert.True(expected[0].Length < VceCodec.MaxChunkSize);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken
            );
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            var buffer = new byte[expected[0].Length + 64];
            var total = 0;
            while (total < expected[0].Length)
            {
                var read = await peer.GetStream().ReadAsync(buffer.AsMemory(total), timeout.Token);
                Assert.True(read > 0);
                total += read;
            }

            Assert.Equal(expected[0].Length, total);
            Assert.Equal(expected[0], buffer.AsSpan(0, total).ToArray());

            var offset = 0;
            for (var i = 0; i < 30; i++)
            {
                Assert.Equal(0x03, buffer[offset]);
                var len = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 1, 4));
                Assert.Equal(24u, len);
                Assert.Equal(
                    (ushort)PacketType.ItemCreateNotify,
                    BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 5, 2))
                );
                offset += 7 + 22;
            }

            Assert.Equal(expected[0].Length, offset);
        }
    }

    [Fact]
    public async Task SendAsync_List_OversizedPacketIsChunkedWhenEncrypted()
    {
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.encrypted = true;
            context.SetCamelliaKeys(new byte[16], new byte[16]);

            var payload = new byte[VceCodec.MaxChunkSize + 100];
            await context.SendAsync(
                [(PacketType.ItemGetBaseListResponse, payload)],
                TestContext.Current.CancellationToken
            );

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken
            );
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            var encodedLen = 7 + payload.Length;
            var expectedChunks = (encodedLen + VceCodec.MaxChunkSize - 1) / VceCodec.MaxChunkSize;
            Assert.True(expectedChunks >= 2);

            var stream = peer.GetStream();
            for (var chunk = 0; chunk < expectedChunks; chunk++)
            {
                var header = new byte[4];
                await ReadExactAsync(stream, header, timeout.Token);
                var plainLen = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
                Assert.True(plainLen > 0);
                Assert.True(plainLen <= VceCodec.MaxChunkSize);

                var paddedLen = (plainLen + 15) / 16 * 16;
                var cipher = new byte[paddedLen];
                await ReadExactAsync(stream, cipher, timeout.Token);
            }
        }
    }

    private static async Task ReadExactAsync(
        NetworkStream stream,
        byte[] buffer,
        CancellationToken ct
    )
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), ct);
            Assert.True(read > 0);
            offset += read;
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
