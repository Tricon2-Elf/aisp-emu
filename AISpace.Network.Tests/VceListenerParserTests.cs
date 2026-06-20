using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Network.Tests;

public class VceListenerParserTests
{
    [Fact]
    public async Task ParseAndDispatchFrameAsync_FirstPacketNotVersionCheck_Disconnects()
    {
        var channel = Channel.CreateUnbounded<Packet>();
        var listener = new VceListener(NullLogger<VceListener>.Instance, channel, "Test", 0, NullLoggerFactory.Instance, _ => { });
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.CurrentState = ClientState.WaitingForVersionCheck;
            var cipher = BuildSinglePacketFrame(PacketType.AuthenticateRequest);
            await InvokeParseAndDispatchFrameAsync(listener, context, cipher, cipher.Length);

            Assert.Equal(ClientState.ForceDisconnect, context.CurrentState);
            Assert.False(channel.Reader.TryRead(out _));
        }
    }

    [Fact]
    public async Task ParseAndDispatchFrameAsync_MalformedSecondPacket_Disconnects()
    {
        var channel = Channel.CreateUnbounded<Packet>();
        var listener = new VceListener(NullLogger<VceListener>.Instance, channel, "Test", 0, NullLoggerFactory.Instance, _ => { });
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.CurrentState = ClientState.WaitingForVersionCheck;
            var cipher = BuildVersionCheckThenMalformedFrame();
            await InvokeParseAndDispatchFrameAsync(listener, context, cipher, cipher.Length);

            Assert.Equal(ClientState.ForceDisconnect, context.CurrentState);
            Assert.True(channel.Reader.TryRead(out var first));
            Assert.Equal(PacketType.VersionCheckRequest, first.Type);
            Assert.False(channel.Reader.TryRead(out _));
        }
    }

    private static byte[] BuildSinglePacketFrame(PacketType packetType)
    {
        // PacketData header with headerParam=0 => payload size is one byte at offset+1.
        // payload size 2 means packet contains only PacketType and no body.
        var frame = new byte[4];
        frame[0] = 0x00; // PacketData, headerParam=0
        frame[1] = 0x02; // payload length
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2, 2), (ushort)packetType);
        return frame;
    }

    private static byte[] BuildVersionCheckThenMalformedFrame()
    {
        // First packet: valid VersionCheckRequest with zero body (4 bytes total).
        // Second packet: declared payload length 10, but frame ends before that.
        var frame = new byte[8];
        frame[0] = 0x00;
        frame[1] = 0x02;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2, 2), (ushort)PacketType.VersionCheckRequest);

        frame[4] = 0x00;
        frame[5] = 0x0A;
        frame[6] = 0x00;
        frame[7] = 0x00;
        return frame;
    }

    private static async Task InvokeParseAndDispatchFrameAsync(
        VceListener listener,
        ClientConnection context,
        byte[] cipher,
        int msgSize
    )
    {
        var method = typeof(VceListener).GetMethod("ParseAndDispatchFrameAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var taskObj = method!.Invoke(listener, [context, cipher, msgSize, TestContext.Current.CancellationToken]);
        Assert.NotNull(taskObj);

        await (Task)taskObj!;
    }

    private static async Task<(ClientConnection context, TcpClient peer)> CreateClientContextAsync()
    {
        var acceptor = new TcpListener(IPAddress.Loopback, 0);
        acceptor.Start();
        try
        {
            var peer = new TcpClient();
            await peer.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)acceptor.LocalEndpoint).Port);
            var serverSide = await acceptor.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
            var context = new ClientConnection(Guid.NewGuid(), serverSide.Client.RemoteEndPoint!, serverSide.GetStream(), NullLogger<ClientConnection>.Instance, serverSide);
            return (context, peer);
        }
        finally
        {
            acceptor.Stop();
        }
    }
}
