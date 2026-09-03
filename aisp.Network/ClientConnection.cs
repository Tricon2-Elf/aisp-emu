using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using aisp.Network.Crypto;
using Microsoft.Extensions.Logging;

namespace aisp.Network;

public class ClientConnection(
    Guid _Id,
    EndPoint _RemoteEndPoint,
    NetworkStream _ns,
    ILogger<ClientConnection> logger,
    TcpClient? _tcpClient = null,
    string? _serverType = null,
    Func<Guid, int?>? _userIdResolver = null,
    int sendQueueCapacity = 1024,
    int sendTimeoutSeconds = 30
) : IDisposable
{
    private static readonly HashSet<PacketType> DebugSendLogs =
    [
        PacketType.RoboAiscriptStartResponse,
    ];
    const int BlockSize = 16;
    public VCECamellia128 C2S = new();
    public VCECamellia128 S2C = new();
    public bool encrypted = true;
    public ClientState CurrentState;
    public Guid Id = _Id;
    public EndPoint RemoteEndPoint = _RemoteEndPoint;
    public NetworkStream Stream = _ns;
    public DateTimeOffset Connected { get; } = DateTimeOffset.UtcNow;

    private int _closed;
    private int _sendPumpStarted;

    public bool IsClosed => Volatile.Read(ref _closed) != 0;

    private readonly string _serverType = _serverType ?? "Unknown";
    private readonly TimeSpan _sendTimeout = TimeSpan.FromSeconds(Math.Max(1, sendTimeoutSeconds));
    private readonly CancellationTokenSource _sendCts = new();
    private readonly Channel<byte[]> _sendQueue = Channel.CreateBounded<byte[]>(
        new BoundedChannelOptions(Math.Max(1, sendQueueCapacity))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite,
        }
    );

    private int? ResolveUserIdForLog()
    {
        if (_userIdResolver is null)
            return null;

        try
        {
            return _userIdResolver.Invoke(Id);
        }
        catch
        {
            return null;
        }
    }

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default)
    {
        if (IsClosed)
            return;

        await Stream.WriteAsync(data, ct);
    }

    public void SetCamelliaKeys(byte[] s2cKey, byte[] c2sKey)
    {
        S2C.Init(s2cKey);
        C2S.Init(c2sKey);
    }

    public void DecryptBlock(Span<byte> data) => C2S.DecryptBlock(data);

    public void EncryptBlock(Span<byte> data) => S2C.EncryptBlock(data);

    public void DecryptBlocks(Span<byte> data)
    {
        for (int offset = 0; offset < data.Length; offset += 16)
            DecryptBlock(data[offset..(offset + 16)]);
    }

    public void EncryptBlocks(Span<byte> data)
    {
        for (int offset = 0; offset < data.Length; offset += 16)
            EncryptBlock(data[offset..(offset + 16)]);
    }

    static byte[] PrefixLengthUInt32Le(ReadOnlySpan<byte> cipher, int innerSize)
    {
        var outBuf = new byte[4 + cipher.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(outBuf.AsSpan(0, 4), (uint)innerSize);
        cipher.CopyTo(outBuf.AsSpan(4));
        return outBuf;
    }

    public Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default)
    {
        if (IsClosed)
            return Task.CompletedTask;

        LogOutbound(type, payload.Length);
        return EnqueueEncodedAsync(VceCodec.EncodePacketData(type, payload));
    }

    public Task SendAsync(
        IReadOnlyList<(PacketType Type, byte[] Payload)> packets,
        CancellationToken ct = default
    )
    {
        if (IsClosed || packets.Count == 0)
            return Task.CompletedTask;

        if (packets.Count == 1)
            return SendAsync(packets[0].Type, packets[0].Payload, ct);

        for (var i = 0; i < packets.Count; i++)
        {
            var (type, payload) = packets[i];
            LogOutbound(type, payload.Length);
        }

        var frames = VceCodec.EncodePacketDataFrames(packets);
        EnsureSendPump();
        foreach (var frame in frames)
        {
            if (!TryEnqueue(frame))
                return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    public Task SendAsync(
        PacketType type,
        IOutgoingPacket packet,
        CancellationToken ct = default
    ) => SendAsync(type, packet.ToBytes(), ct);

    void LogOutbound(PacketType type, int payloadLength)
    {
        if (type == PacketType.Ping || type == PacketType.TimeZoneGetResponse)
            return;

        var logLevel = DebugSendLogs.Contains(type) ? LogLevel.Debug : LogLevel.Information;
        logger.Log(
            logLevel,
            "Sending [{ServerType}] [UserId:{UserId}] {PacketType}, {Length}",
            _serverType,
            ResolveUserIdForLog()?.ToString() ?? "n/a",
            type,
            payloadLength
        );
    }

    Task EnqueueEncodedAsync(byte[] encodedPlaintext)
    {
        EnsureSendPump();
        TryEnqueue(encodedPlaintext);
        return Task.CompletedTask;
    }

    bool TryEnqueue(byte[] encodedPlaintext)
    {
        if (_sendQueue.Writer.TryWrite(encodedPlaintext))
            return true;

        if (!IsClosed)
        {
            logger.LogWarning(
                "Outbound send queue full for [{ServerType}] {Id}; closing slow connection",
                _serverType,
                Id
            );
            Dispose();
        }

        return false;
    }

    private void EnsureSendPump()
    {
        if (Interlocked.CompareExchange(ref _sendPumpStarted, 1, 0) != 0)
            return;

        _ = RunSendPumpAsync();
    }

    private async Task RunSendPumpAsync()
    {
        try
        {
            await foreach (var encoded in _sendQueue.Reader.ReadAllAsync(_sendCts.Token))
            {
                if (IsClosed)
                    break;

                await WriteEncodedAsync(encoded, _sendCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // connection closing
        }
        catch (ObjectDisposedException)
        {
            // connection closed while a send was in flight
        }
        catch (Exception ex)
        {
            logger.LogError("Err {ex}", ex);
        }
        finally
        {
            Dispose();
        }
    }

    private async Task WriteEncodedAsync(byte[] dataToSend, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_sendTimeout);
        var writeCt = timeoutCts.Token;

        try
        {
            if (!encrypted)
            {
                await SendRawAsync(dataToSend, writeCt);
                return;
            }

            int offset = 0;
            while (offset < dataToSend.Length)
            {
                int plainChunkSize = Math.Min(VceCodec.MaxChunkSize, dataToSend.Length - offset);
                ReadOnlySpan<byte> plainChunk = dataToSend.AsSpan(offset, plainChunkSize);
                byte[] padded = PadToBlock(plainChunk, BlockSize);
                EncryptBlocks(padded);
                byte[] framed = PrefixLengthUInt32Le(padded, plainChunkSize);
                await SendRawAsync(framed, writeCt);
                offset += plainChunkSize;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "Send timed out after {Timeout}s for [{ServerType}] {Id}; closing connection",
                _sendTimeout.TotalSeconds,
                _serverType,
                Id
            );
            throw;
        }
    }

    static byte[] PadToBlock(ReadOnlySpan<byte> input, int blockSize)
    {
        int paddedLength = (input.Length + blockSize - 1) / blockSize * blockSize;
        var buffer = new byte[paddedLength];
        input.CopyTo(buffer);
        return buffer;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
            return;

        try
        {
            _sendQueue.Writer.TryComplete();
        }
        catch
        { /* ignore */
        }

        try
        {
            _sendCts.Cancel();
        }
        catch
        { /* ignore */
        }

        try
        {
            if (_tcpClient is not null)
                _tcpClient.LingerState = new LingerOption(true, 0);
        }
        catch
        { /* ignore */
        }

        try
        {
            Stream.Dispose();
        }
        catch
        { /* ignore */
        }

        try
        {
            _tcpClient?.Dispose();
        }
        catch
        { /* ignore */
        }

        GC.SuppressFinalize(this);
    }
}
