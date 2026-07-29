using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Network.Tests;

public class VceListenerParserTests
{
    [Fact]
    public async Task ParseAndDispatchFrameAsync_FirstPacketNotVersionCheck_Disconnects()
    {
        var channel = Channel.CreateUnbounded<Packet>();
        var listener = new VceListener(
            NullLogger<VceListener>.Instance,
            channel,
            "Test",
            0,
            NullLoggerFactory.Instance,
            _ => { }
        );
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
        var listener = new VceListener(
            NullLogger<VceListener>.Instance,
            channel,
            "Test",
            0,
            NullLoggerFactory.Instance,
            _ => { }
        );
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

    [Theory]
    [InlineData(PacketType.RoboAiscriptStartRequest)]
    [InlineData(PacketType.RoboAiscriptEndRequest)]
    public async Task ParseAndDispatchFrameAsync_RoboAiLifecyclePacket_LogsAtDebug(
        PacketType packetType
    )
    {
        var logger = new RecordingLogger<VceListener>();
        var channel = Channel.CreateUnbounded<Packet>();
        var listener = new VceListener(
            logger,
            channel,
            "Area",
            0,
            NullLoggerFactory.Instance,
            _ => { }
        );
        var (context, peer) = await CreateClientContextAsync();
        using (peer)
        using (context)
        {
            context.CurrentState = ClientState.Connected;
            var frame = BuildSinglePacketFrame(packetType, [1, 0, 0, 0]);
            await InvokeParseAndDispatchFrameAsync(listener, context, frame, frame.Length);

            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Debug && entry.Message.Contains(packetType.ToString())
            );
            Assert.DoesNotContain(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Information
                    && entry.Message.Contains(packetType.ToString())
            );
        }
    }

    [Fact]
    public async Task SendAsync_RoboAiStartResponse_LogsAtDebug()
    {
        var logger = new RecordingLogger<ClientConnection>();
        var (context, peer) = await CreateClientContextAsync(logger);
        using (peer)
        using (context)
        {
            context.encrypted = false;
            await context.SendAsync(
                PacketType.RoboAiscriptStartResponse,
                new byte[8],
                TestContext.Current.CancellationToken
            );

            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Debug
                    && entry.Message.Contains(nameof(PacketType.RoboAiscriptStartResponse))
            );
            Assert.DoesNotContain(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Information
                    && entry.Message.Contains(nameof(PacketType.RoboAiscriptStartResponse))
            );
        }
    }

    private static byte[] BuildSinglePacketFrame(PacketType packetType, byte[]? payload = null)
    {
        payload ??= [];
        // PacketData header with headerParam=0 => payload size is one byte at offset+1.
        var frame = new byte[4 + payload.Length];
        frame[0] = 0x00; // PacketData, headerParam=0
        frame[1] = checked((byte)(2 + payload.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2, 2), (ushort)packetType);
        payload.CopyTo(frame.AsSpan(4));
        return frame;
    }

    private static byte[] BuildVersionCheckThenMalformedFrame()
    {
        // First packet: valid VersionCheckRequest with zero body (4 bytes total).
        // Second packet: declared payload length 10, but frame ends before that.
        var frame = new byte[8];
        frame[0] = 0x00;
        frame[1] = 0x02;
        BinaryPrimitives.WriteUInt16LittleEndian(
            frame.AsSpan(2, 2),
            (ushort)PacketType.VersionCheckRequest
        );

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
        var method = typeof(VceListener).GetMethod(
            "ParseAndDispatchFrameAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.NotNull(method);

        var taskObj = method!.Invoke(
            listener,
            [context, cipher, msgSize, TestContext.Current.CancellationToken]
        );
        Assert.NotNull(taskObj);

        await (Task)taskObj!;
    }

    private static async Task<(ClientConnection context, TcpClient peer)> CreateClientContextAsync(
        ILogger<ClientConnection>? logger = null
    )
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
                logger ?? NullLogger<ClientConnection>.Instance,
                serverSide
            );
            return (context, peer);
        }
        finally
        {
            acceptor.Stop();
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
