using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using aisp.Network.Crypto;
using Microsoft.Extensions.Logging;

namespace aisp.Network;

public class VceListener(
    ILogger<VceListener> logger,
    Channel<Packet> channel,
    string name,
    int port,
    ILoggerFactory loggerFactory,
    Action<Guid>? onDisconnect,
    Action<string, int>? onListeningStarted = null,
    int maxConcurrentClients = 1024,
    int maxReceiveFrameSize = 4096,
    int clientReadTimeoutSeconds = 300,
    int clientSendTimeoutSeconds = 30,
    Func<Guid, int?>? resolveUserId = null,
    TcpSocketOptions? tcpSocketOptions = null,
    Func<Packet, CancellationToken, ValueTask>? onInboundPacket = null
)
{
    private static readonly HashSet<PacketType> SuppressedReceiveLogs =
    [
        PacketType.Ping,
        PacketType.AvatarMoveRequest,
    ];
    private static readonly HashSet<PacketType> DebugReceiveLogs =
    [
        PacketType.RoboAiscriptStartRequest,
        PacketType.RoboAiscriptEndRequest,
    ];
    private static readonly TimeSpan IdleTimerRearmMinInterval = TimeSpan.FromMilliseconds(250);
    private readonly int _maxConcurrentClients = Math.Max(1, maxConcurrentClients);
    private readonly SemaphoreSlim _clientGate = new(
        Math.Max(1, maxConcurrentClients),
        Math.Max(1, maxConcurrentClients)
    );
    private readonly int _maxReceiveFrameSize = Math.Max(1, maxReceiveFrameSize);
    private readonly int _sendTimeoutSeconds = Math.Max(1, clientSendTimeoutSeconds);
    private readonly TcpSocketOptions _tcpSocketOptions =
        tcpSocketOptions ?? TcpSocketOptions.Default;
    private readonly TimeSpan _readTimeout =
        clientReadTimeoutSeconds > 0
            ? TimeSpan.FromSeconds(clientReadTimeoutSeconds)
            : TimeSpan.Zero;
    private TcpListener? _tcpListener;
    private volatile bool _isListening;
    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();

    private string ResolveUserIdForLog(ClientConnection context)
    {
        if (resolveUserId is null)
            return "n/a";

        try
        {
            return resolveUserId.Invoke(context.Id)?.ToString() ?? "n/a";
        }
        catch
        {
            return "n/a";
        }
    }

    public ChannelReader<Packet> PacketReader => channel.Reader;

    public bool IsListening => _isListening;

    public VceClientLoad GetClientLoad()
    {
        int active = _clients.Count;
        return new VceClientLoad(active, _maxConcurrentClients - active, _maxConcurrentClients);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _tcpListener = new TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), port);
        _tcpListener.Start();
        _isListening = true;
        onListeningStarted?.Invoke(name, port);
        int handlerCap = Math.Max(1, maxConcurrentClients);
        logger.LogInformation(
            "Server {Name} started on {LocalEP} (max concurrent client handlers: {MaxHandlers}, noDelay={NoDelay}, keepAlive={KeepAlive})",
            name,
            _tcpListener.LocalEndpoint,
            handlerCap,
            _tcpSocketOptions.NoDelay,
            _tcpSocketOptions.KeepAlive
        );

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _clientGate.WaitAsync(ct);
                    TcpClient tcpClient;
                    try
                    {
                        tcpClient = await _tcpListener.AcceptTcpClientAsync(ct);
                    }
                    catch
                    {
                        _clientGate.Release();
                        throw;
                    }

                    try
                    {
                        try
                        {
                            TcpSocketTuning.Apply(tcpClient.Client, _tcpSocketOptions);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(
                                ex,
                                "{Name} failed applying TCP socket options for {RemoteEndPoint}",
                                name,
                                tcpClient.Client.RemoteEndPoint
                            );
                        }

                        var context = new ClientConnection(
                            Guid.NewGuid(),
                            tcpClient.Client.RemoteEndPoint!,
                            tcpClient.GetStream(),
                            loggerFactory.CreateLogger<ClientConnection>(),
                            tcpClient,
                            name,
                            resolveUserId,
                            sendTimeoutSeconds: _sendTimeoutSeconds
                        );
                        _clients[context.Id] = context;
                        _ = RunClientWithGateAsync(context, ct);
                    }
                    catch (Exception setupEx)
                    {
                        _clientGate.Release();
                        try
                        {
                            tcpClient.Dispose();
                        }
                        catch
                        { /* ignore */
                        }

                        if (ct.IsCancellationRequested)
                            break;
                        logger.LogError(
                            setupEx,
                            "Failed to initialize client on {Name}: {Message}",
                            name,
                            setupEx.Message
                        );
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    logger.LogError(
                        ex,
                        "Accept/peek failed on {Name}, continuing: {Message}",
                        name,
                        ex.Message
                    );
                    try
                    {
                        await Task.Delay(250, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            logger.LogInformation(
                "Server {Name} accept loop stopped on {LocalEP}",
                name,
                _tcpListener?.LocalEndpoint
            );
        }
        finally
        {
            _isListening = false;
            try
            {
                _tcpListener?.Stop();
            }
            catch
            { /* ignore */
            }
            channel.Writer.Complete();
        }
    }

    private async Task RunClientWithGateAsync(ClientConnection context, CancellationToken ct)
    {
        try
        {
            await HandleClientAsync(context, ct);
        }
        finally
        {
            _clientGate.Release();
        }
    }

    private async Task HandleClientAsync(ClientConnection context, CancellationToken ct)
    {
        CancellationTokenSource? readCts = null;
        Timer? idleTimer = null;
        var readCt = ct;
        Action? armIdle = null;
        if (_readTimeout > TimeSpan.Zero)
        {
            readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCt = readCts.Token;
            idleTimer = new Timer(
                _ => readCts.Cancel(),
                null,
                _readTimeout,
                Timeout.InfiniteTimeSpan
            );
            long lastIdleArmAtMs = Environment.TickCount64;
            armIdle = () =>
            {
                var nowMs = Environment.TickCount64;
                if (nowMs - lastIdleArmAtMs < IdleTimerRearmMinInterval.TotalMilliseconds)
                    return;

                idleTimer.Change(_readTimeout, Timeout.InfiniteTimeSpan);
                lastIdleArmAtMs = nowMs;
            };
        }

        try
        {
            var remote = (IPEndPoint)context.RemoteEndPoint;
            logger.LogInformation(
                "{Name} Handling new Encrypted client {Id} [ServerType:{ServerType}] [UserId:{UserId}] from {RemoteAddress}:{RemotePort}",
                name,
                context.Id,
                name,
                ResolveUserIdForLog(context),
                remote.Address,
                remote.Port
            );
            context.CurrentState = ClientState.WaitingForHandshake;
            if (!await HandshakeAsync(context, ct))
                return;
            context.CurrentState = ClientState.WaitingForVersionCheck;

            while (!ct.IsCancellationRequested)
            {
                var frame = await ReceiveFrameAsync(context, ct, readCt, armIdle);
                if (frame is null)
                    return;

                try
                {
                    await ParseAndDispatchFrameAsync(
                        context,
                        frame.Value.Buffer,
                        frame.Value.MessageSize,
                        ct
                    );
                    if (context.CurrentState == ClientState.ForceDisconnect)
                        return;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(frame.Value.Buffer);
                }
            }
        }
        catch (Exception ex)
        {
            if (!IsExpectedDisconnect(ex))
                logger.LogError("Err {ex}", ex);
        }
        finally
        {
            idleTimer?.Dispose();
            readCts?.Dispose();
            context.Dispose();

            _clients.TryRemove(context.Id, out _);
            try
            {
                onDisconnect?.Invoke(context.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{Name} onDisconnect failed for {Id}", name, context.Id);
            }

            logger.LogInformation(
                "{Name} Client disconnected [ServerType:{ServerType}] [UserId:{UserId}]: {RemoteEndPoint} ({Id})",
                name,
                name,
                ResolveUserIdForLog(context),
                context.RemoteEndPoint,
                context.Id
            );
        }
    }

    private async Task<bool> HandshakeAsync(ClientConnection context, CancellationToken ct)
    {
        using var handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var handshakeTimeout = TimeSpan.FromSeconds(5);
        handshakeCts.CancelAfter(handshakeTimeout);
        var handshakeCt = handshakeCts.Token;

        byte[] rsaN = new byte[16];
        await ReadExactAsync(context.Stream, rsaN, ct, handshakeCt, null);
        if (!CryptoUtils.IsPlausibleClientRsaModulus(rsaN))
        {
            logger.LogDebug(
                "{Name} rejecting implausible RSA modulus from {RemoteEndPoint}",
                name,
                context.RemoteEndPoint
            );
            return false;
        }

        var (s2cPlain, s2cEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        var (c2sPlain, c2sEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        context.SetCamelliaKeys(s2cPlain, c2sPlain);

        if (context.Stream.DataAvailable)
        {
            logger.LogDebug(
                "{Name} disconnecting client {RemoteEndPoint}: unexpected bytes after RSA handshake",
                name,
                context.RemoteEndPoint
            );
            return false;
        }

        await context.SendRawAsync([.. s2cEnc, .. c2sEnc], handshakeCt);
        return true;
    }

    private async Task<ReceivedFrame?> ReceiveFrameAsync(
        ClientConnection context,
        CancellationToken ct,
        CancellationToken readCt,
        Action? armIdle
    )
    {
        var header = new byte[4];
        await ReadExactAsync(context.Stream, header, ct, readCt, armIdle);
        int msgSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);

        if (!VceFrameValidation.IsAcceptableFrameSize(msgSize, _maxReceiveFrameSize))
        {
            logger.LogDebug(
                "{Name} disconnecting client {RemoteEndPoint}: invalid frame size msgSize={MsgSize} max={MaxReceiveFrameSize}",
                name,
                context.RemoteEndPoint,
                msgSize,
                _maxReceiveFrameSize
            );
            return null;
        }

        int paddedSize = (msgSize + 15) / 16 * 16;
        var rented = ArrayPool<byte>.Shared.Rent(paddedSize);
        try
        {
            await ReadExactAsync(
                context.Stream,
                rented.AsMemory(0, paddedSize),
                ct,
                readCt,
                armIdle
            );
            context.DecryptBlocks(rented.AsSpan(0, paddedSize));
            return new ReceivedFrame(rented, msgSize);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(rented);
            throw;
        }
    }

    private async Task ParseAndDispatchFrameAsync(
        ClientConnection context,
        byte[] decryptedFrame,
        int msgSize,
        CancellationToken ct
    )
    {
        int offset = 0;
        while (offset < msgSize)
        {
            if (offset + 2 > msgSize)
            {
                logger.LogDebug(
                    "{Name} disconnecting client {RemoteEndPoint}: truncated packet header (offset={Offset} msgSize={MsgSize})",
                    name,
                    context.RemoteEndPoint,
                    offset,
                    msgSize
                );
                context.CurrentState = ClientState.ForceDisconnect;
                return;
            }

            byte codecType = decryptedFrame[offset];
            var headerType = (VceCodecHeaderType)((codecType >> 4) & 0xF);
            int headerParam = codecType & 0xF;
            if (headerType != VceCodecHeaderType.PacketData)
            {
                if (
                    (headerType == VceCodecHeaderType.Ping || headerType == VceCodecHeaderType.Pong)
                    && msgSize - offset >= 9
                )
                {
                    offset += 9;
                    continue;
                }
                if (headerType == VceCodecHeaderType.Terminated && msgSize - offset >= 5)
                {
                    offset += 5;
                    continue;
                }
                if (headerType == VceCodecHeaderType.DirectContact)
                {
                    logger.LogDebug(
                        "{Name} ignoring DirectContact control frame from {RemoteEndPoint}",
                        name,
                        context.RemoteEndPoint
                    );
                    break;
                }

                logger.LogDebug(
                    "{Name} disconnecting client {RemoteEndPoint}: unexpected codec header {HeaderType}",
                    name,
                    context.RemoteEndPoint,
                    headerType
                );
                context.CurrentState = ClientState.ForceDisconnect;
                return;
            }

            int payloadStartOffset = 2 + headerParam;
            if (offset + payloadStartOffset > msgSize)
            {
                logger.LogDebug(
                    "{Name} disconnecting client {RemoteEndPoint}: invalid payload start offset (offset={Offset} startOffset={PayloadStartOffset} msgSize={MsgSize})",
                    name,
                    context.RemoteEndPoint,
                    offset,
                    payloadStartOffset,
                    msgSize
                );
                context.CurrentState = ClientState.ForceDisconnect;
                return;
            }
            int payloadStart = offset + payloadStartOffset;
            int payloadLen = CalculatePayloadLength(decryptedFrame, offset, headerParam);

            int payloadEnd = payloadStart + payloadLen;

            if (payloadLen < 0 || payloadEnd > msgSize)
            {
                if (offset == 0 && msgSize >= 2)
                {
                    var singleTypeRaw = BinaryPrimitives.ReadUInt16LittleEndian(
                        decryptedFrame.AsSpan(0, 2)
                    );
                    var singleType = (PacketType)singleTypeRaw;
                    int singleBodyLen = msgSize - 2;
                    ReadOnlySpan<byte> singlePayload =
                        singleBodyLen > 0 ? decryptedFrame.AsSpan(2, singleBodyLen) : [];
                    if (
                        context.CurrentState == ClientState.WaitingForVersionCheck
                        && singleType != PacketType.VersionCheckRequest
                    )
                    {
                        logger.LogDebug(
                            "{Name} disconnecting client {RemoteEndPoint}: first packet was {PacketType}, expected VersionCheckRequest",
                            name,
                            context.RemoteEndPoint,
                            singleType
                        );
                        context.CurrentState = ClientState.ForceDisconnect;
                        return;
                    }

                    if (context.CurrentState == ClientState.WaitingForVersionCheck)
                        context.CurrentState = ClientState.Connected;
                    LogReceivedPacket(context, singleType, singlePayload);
                    await PublishPacketAsync(
                        new Packet(context, singleType, singlePayload.ToArray(), singleTypeRaw),
                        ct
                    );
                    break;
                }

                logger.LogDebug(
                    "{Name} disconnecting client {RemoteEndPoint}: invalid payload length (offset={Offset} payloadLen={PayloadLen} payloadEnd={PayloadEnd} msgSize={MsgSize})",
                    name,
                    context.RemoteEndPoint,
                    offset,
                    payloadLen,
                    payloadEnd,
                    msgSize
                );
                context.CurrentState = ClientState.ForceDisconnect;
                return;
            }

            var typeRaw = BinaryPrimitives.ReadUInt16LittleEndian(
                decryptedFrame.AsSpan(payloadStart, 2)
            );
            var type = (PacketType)typeRaw;
            int bodyLen = payloadLen - 2;
            if (
                context.CurrentState == ClientState.WaitingForVersionCheck
                && type != PacketType.VersionCheckRequest
            )
            {
                logger.LogDebug(
                    "{Name} disconnecting client {RemoteEndPoint}: first packet was {PacketType}, expected VersionCheckRequest",
                    name,
                    context.RemoteEndPoint,
                    type
                );
                context.CurrentState = ClientState.ForceDisconnect;
                return;
            }

            if (context.CurrentState == ClientState.WaitingForVersionCheck)
                context.CurrentState = ClientState.Connected;
            ReadOnlySpan<byte> payload =
                bodyLen > 0 ? decryptedFrame.AsSpan(payloadStart + 2, bodyLen) : [];
            LogReceivedPacket(context, type, payload);
            await PublishPacketAsync(new Packet(context, type, payload.ToArray(), typeRaw), ct);

            offset = payloadEnd;
        }
    }

    private ValueTask PublishPacketAsync(Packet packet, CancellationToken ct) =>
        onInboundPacket is not null
            ? onInboundPacket(packet, ct)
            : channel.Writer.WriteAsync(packet, ct);

    private void LogReceivedPacket(
        ClientConnection context,
        PacketType type,
        ReadOnlySpan<byte> payload
    )
    {
        if (SuppressedReceiveLogs.Contains(type))
            return;

        var logLevel = DebugReceiveLogs.Contains(type) ? LogLevel.Debug : LogLevel.Information;
        logger.Log(
            logLevel,
            "Recieving packet [{ServerType}] [UserId:{UserId}] {PacketType} ({Length} bytes): {Hex}",
            name,
            ResolveUserIdForLog(context),
            type,
            payload.Length,
            BitConverter.ToString(payload.ToArray())
        );
    }

    static int CalculatePayloadLength(ReadOnlySpan<byte> buffer, int offset, int headerParam)
    {
        int sizeBytes = 1 + headerParam;
        if (sizeBytes > 4)
            sizeBytes = 4;
        int packetSize = buffer[offset + 1];
        if (sizeBytes >= 2)
            packetSize |= buffer[offset + 2] << 8;
        if (sizeBytes >= 3)
            packetSize |= buffer[offset + 3] << 16;
        if (sizeBytes >= 4)
            packetSize |= buffer[offset + 4] << 24;
        return packetSize;
    }

    private static bool IsExpectedDisconnect(Exception ex)
    {
        if (
            ex is IOException io
            && io.Message
                is "Disconnected"
                    or "The client closed the connection."
                    or "Read timed out"
        )
            return true;
        if (ex is ObjectDisposedException)
            return true;
        if (
            ex is SocketException se
            && se.SocketErrorCode
                is SocketError.ConnectionReset
                    or SocketError.Shutdown
                    or SocketError.ConnectionAborted
                    or SocketError.TimedOut
        )
            return true;
        return false;
    }

    private async Task ReadExactAsync(
        NetworkStream stream,
        Memory<byte> buffer,
        CancellationToken serverCt,
        CancellationToken readCt,
        Action? armIdle
    )
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await ReadChunkAsync(stream, buffer[totalRead..], serverCt, readCt, armIdle);
            if (read == 0)
                throw new IOException("Disconnected");
            totalRead += read;
        }
    }

    private static async Task<int> ReadChunkAsync(
        NetworkStream stream,
        Memory<byte> buffer,
        CancellationToken serverCt,
        CancellationToken readCt,
        Action? armIdle
    )
    {
        try
        {
            int read = await stream.ReadAsync(buffer, readCt);
            if (read > 0)
                armIdle?.Invoke();
            return read;
        }
        catch (OperationCanceledException)
            when (readCt.IsCancellationRequested && !serverCt.IsCancellationRequested)
        {
            throw new IOException("Read timed out");
        }
    }

    private readonly record struct ReceivedFrame(byte[] Buffer, int MessageSize);
}
