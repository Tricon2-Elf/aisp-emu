using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using AISpace.Network.Crypto;
using Microsoft.Extensions.Logging;

namespace AISpace.Network;

public class VceListener
{
    private static readonly HashSet<PacketType> SuppressedReceiveLogs = [PacketType.Ping, PacketType.AvatarMoveRequest];
    private readonly ILogger<VceListener> _logger;
    private readonly Channel<Packet> _channel;
    private readonly string _name;
    private readonly int _port;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Action<Guid>? _onDisconnect;
    private TcpListener? _tcpListener;
    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();

    public VceListener(ILogger<VceListener> logger, Channel<Packet> channel, string name, int port, ILoggerFactory loggerFactory, Action<Guid>? onDisconnect)
    {
        _logger = logger;
        _channel = channel;
        _name = name;
        _port = port;
        _loggerFactory = loggerFactory;
        _onDisconnect = onDisconnect;
    }

    public ChannelReader<Packet> PacketReader => _channel.Reader;

    /// <summary>
    /// Runs the accept loop until cancellation. Call this from the server's ExecuteAsync together with the packet loop.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _tcpListener = new TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), _port);
        _tcpListener.Start();
        _logger.LogInformation("Server {Name} started on {LocalEP}", _name, _tcpListener.LocalEndpoint);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync(ct);
                    var context = new ClientConnection(Guid.NewGuid(), client.Client.RemoteEndPoint!, client.GetStream(), _loggerFactory.CreateLogger<ClientConnection>());
                    _clients[context.Id] = context;
                    byte first = await PeekByteAsync(client.Client, ct);
                    _logger.LogInformation("First Byte! {b}", first);
                    if (first != 0)
                        _ = HandleCryptoClientAsync(context, ct);
                    else
                    {
                        context.encrypted = false;
                        _ = HandleClientAsync(context, ct);
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
                    _logger.LogError(ex, "Accept/peek failed on {Name}, continuing: {Message}", _name, ex.Message);
                }
            }
        }
        finally
        {
            try
            {
                _tcpListener?.Stop();
            }
            catch
            { /* ignore */
            }
            _channel.Writer.Complete();
        }
    }

    static async ValueTask<byte> PeekByteAsync(Socket s, CancellationToken ct = default)
    {
        var buf = new byte[1];
        int n = await s.ReceiveAsync(buf, SocketFlags.Peek, ct);
        if (n == 0)
            throw new EndOfStreamException();
        return buf[0];
    }

    private async Task HandleClientAsync(ClientConnection context, CancellationToken ct)
    {
        _logger.LogInformation("{Name} Handling new Unencrypted client {Id}", _name, context.Id);
        try
        {
            using var stream = context.Stream;
            var buffer = new byte[4096];

            while (!ct.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(0, 1), ct);
                if (read == 0)
                    break;

                int packetLength = buffer[0];
                if (packetLength < 2)
                    continue;

                await ReadExactAsync(stream, buffer.AsMemory(0, 2), ct);
                ushort typeShort = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2));
                var type = (PacketType)typeShort;
                int payloadLength = packetLength - 2;
                byte[] payload = new byte[payloadLength];
                await ReadExactAsync(stream, payload, ct);
                if (!SuppressedReceiveLogs.Contains(type))
                    _logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, BitConverter.ToString(payload));
                _channel.Writer.TryWrite(new Packet(context, type, payload, typeShort));
            }
        }
        catch (Exception ex)
        {
            if (!IsExpectedDisconnect(ex))
                _logger.LogError("Client {Id} error: {Message}", context.Id, ex.Message);
        }
        finally
        {
            _clients.TryRemove(context.Id, out _);
            _onDisconnect?.Invoke(context.Id);
        }
        _logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
    }

    private async Task HandleCryptoClientAsync(ClientConnection context, CancellationToken ct)
    {
        _logger.LogInformation("{Name} Handling new Encrypted client {Id}", _name, context.Id);
        byte[] rsaN = new byte[16];
        await ReadExactAsync(context.Stream, rsaN, ct);
        var (s2cPlain, s2cEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        var (c2sPlain, c2sEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        context.SetCamelliaKeys(s2cPlain, c2sPlain);
        await context.SendRawAsync([.. s2cEnc, .. c2sEnc]);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var header = new byte[4];
                await ReadExactAsync(context.Stream, header, ct);
                int msgSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);

                int paddedSize = ((msgSize + 15) / 16) * 16;
                byte[] cipher = new byte[paddedSize];
                await ReadExactAsync(context.Stream, cipher, ct);
                context.DecryptBlocks(cipher);

                int offset = 0;
                while (offset < msgSize)
                {
                    if (offset + 2 > msgSize)
                        break;

                    byte codecType = cipher[offset];
                    int headerType = (codecType >> 4) & 0xF;
                    int headerParam = codecType & 0xF;
                    if (headerType != 0)
                    {
                        if ((headerType == 1 || headerType == 2) && msgSize - offset >= 9)
                        {
                            offset += 9;
                            continue;
                        }
                        if (headerType == 3 && msgSize - offset >= 5)
                        {
                            offset += 5;
                            continue;
                        }
                        break;
                    }

                    int sizeBytes = 1 + headerParam;
                    if (sizeBytes > 4)
                        sizeBytes = 4;
                    int payloadStartOffset = 2 + headerParam;
                    if (offset + payloadStartOffset > msgSize)
                        break;
                    int packetSize = cipher[offset + 1];
                    if (sizeBytes >= 2)
                        packetSize |= cipher[offset + 2] << 8;
                    if (sizeBytes >= 3)
                        packetSize |= cipher[offset + 3] << 16;
                    if (sizeBytes >= 4)
                        packetSize |= cipher[offset + 4] << 24;
                    int payloadLen = packetSize;
                    int payloadStart = offset + payloadStartOffset;
                    int payloadEnd = payloadStart + payloadLen;

                    if (payloadLen < 0 || payloadEnd > msgSize)
                    {
                        if (offset == 0 && msgSize >= 2)
                        {
                            var singleTypeRaw = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(0, 2));
                            var singleType = (PacketType)singleTypeRaw;
                            int singleBodyLen = msgSize - 2;
                            ReadOnlySpan<byte> singlePayload = singleBodyLen > 0 ? cipher.AsSpan(2, singleBodyLen) : [];
                            if (!SuppressedReceiveLogs.Contains(singleType))
                                _logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", singleType, singlePayload.Length, BitConverter.ToString(singlePayload.ToArray()));
                            _channel.Writer.TryWrite(new Packet(context, singleType, singlePayload.ToArray(), singleTypeRaw));
                        }
                        else if (payloadLen >= 0)
                            _logger.LogWarning("Encrypted packet: payload past msgSize (offset {Offset} packetSize {PacketSize} msgSize {MsgSize})", offset, packetSize, msgSize);
                        break;
                    }

                    var typeRaw = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(payloadStart, 2));
                    var type = (PacketType)typeRaw;
                    int bodyLen = payloadLen - 2;
                    ReadOnlySpan<byte> payload = bodyLen > 0 ? cipher.AsSpan(payloadStart + 2, bodyLen) : [];
                    if (!SuppressedReceiveLogs.Contains(type))
                        _logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, BitConverter.ToString(payload.ToArray()));
                    _channel.Writer.TryWrite(new Packet(context, type, payload.ToArray(), typeRaw));

                    offset = payloadEnd;
                }
            }
        }
        catch (Exception ex)
        {
            if (!IsExpectedDisconnect(ex))
                _logger.LogError("Err {ex}", ex);
        }
        finally
        {
            _clients.TryRemove(context.Id, out _);
            _onDisconnect?.Invoke(context.Id);
            _logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
        }
    }

    private static bool IsExpectedDisconnect(Exception ex)
    {
        if (ex is IOException io && io.Message is "Disconnected" or "The client closed the connection.")
            return true;
        if (ex is ObjectDisposedException)
            return true;
        if (ex is SocketException se && (se.SocketErrorCode is SocketError.ConnectionReset or SocketError.Shutdown or SocketError.ConnectionAborted))
            return true;
        return false;
    }

    private static async Task ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], ct);
            if (read == 0)
                throw new IOException("Disconnected");
            totalRead += read;
        }
    }
}
