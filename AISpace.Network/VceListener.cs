using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using AISpace.Network.Crypto;
using Microsoft.Extensions.Logging;

namespace AISpace.Network;

public class VceListener(ILogger<VceListener> logger, Channel<Packet> channel, string name, int port, ILoggerFactory loggerFactory, Action<Guid>? onDisconnect, Action<string, int>? onListeningStarted = null, int maxConcurrentClients = 1024, int maxReceiveFrameSize = 4096)
{
    private static readonly HashSet<PacketType> SuppressedReceiveLogs = [PacketType.Ping, PacketType.AvatarMoveRequest];
    private readonly SemaphoreSlim _clientGate = new(Math.Max(1, maxConcurrentClients), Math.Max(1, maxConcurrentClients));
    private readonly int _maxReceiveFrameSize = Math.Max(1, maxReceiveFrameSize);
    private TcpListener? _tcpListener;
    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();

    public ChannelReader<Packet> PacketReader => channel.Reader;

    public async Task RunAsync(CancellationToken ct = default)
    {
        _tcpListener = new TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), port);
        _tcpListener.Start();
        onListeningStarted?.Invoke(name, port);
        int handlerCap = Math.Max(1, maxConcurrentClients);
        logger.LogInformation("Server {Name} started on {LocalEP} (max concurrent client handlers: {MaxHandlers})", name, _tcpListener.LocalEndpoint, handlerCap);

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
                        var context = new ClientConnection(Guid.NewGuid(), tcpClient.Client.RemoteEndPoint!, tcpClient.GetStream(), loggerFactory.CreateLogger<ClientConnection>(), tcpClient);
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
                        logger.LogError(setupEx, "Failed to initialize client on {Name}: {Message}", name, setupEx.Message);
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
                    logger.LogError(ex, "Accept/peek failed on {Name}, continuing: {Message}", name, ex.Message);
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
        logger.LogInformation("{Name} Handling new Encrypted client {Id}", name, context.Id);
        byte[] rsaN = new byte[16];
        await ReadExactAsync(context.Stream, rsaN, ct);
        var (s2cPlain, s2cEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        var (c2sPlain, c2sEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        context.SetCamelliaKeys(s2cPlain, c2sPlain);
        await context.SendRawAsync([.. s2cEnc, .. c2sEnc], ct);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var header = new byte[4];
                await ReadExactAsync(context.Stream, header, ct);
                int msgSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);

                if (!VceFrameValidation.IsAcceptableFrameSize(msgSize, _maxReceiveFrameSize))
                {
                    logger.LogWarning("{Name} rejecting oversized frame from {RemoteEndPoint}: msgSize={MsgSize} max={MaxReceiveFrameSize}", name, context.RemoteEndPoint, msgSize, _maxReceiveFrameSize);
                    break;
                }

                int paddedSize = (msgSize + 15) / 16 * 16;
                byte[] cipher = new byte[paddedSize];
                await ReadExactAsync(context.Stream, cipher, ct);
                context.DecryptBlocks(cipher);

                int offset = 0;
                while (offset < msgSize)
                {
                    if (offset + 2 > msgSize)
                        break;

                    byte codecType = cipher[offset];
                    var headerType = (VceCodecHeaderType)((codecType >> 4) & 0xF);
                    int headerParam = codecType & 0xF;
                    if (headerType != VceCodecHeaderType.PacketData)
                    {
                        if ((headerType == VceCodecHeaderType.Ping || headerType == VceCodecHeaderType.Pong) && msgSize - offset >= 9)
                        {
                            offset += 9;
                            continue;
                        }
                        if (headerType == VceCodecHeaderType.Terminated && msgSize - offset >= 5)
                        {
                            offset += 5;
                            continue;
                        }
                        break;
                    }

                    int payloadStartOffset = 2 + headerParam;
                    if (offset + payloadStartOffset > msgSize)
                        break;
                    int payloadStart = offset + payloadStartOffset;
                    int payloadLen = CalculatePayloadLength(cipher, offset, headerParam);

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
                                logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", singleType, singlePayload.Length, BitConverter.ToString(singlePayload.ToArray()));
                            await channel.Writer.WriteAsync(new Packet(context, singleType, singlePayload.ToArray(), singleTypeRaw), ct);
                        }
                        else if (payloadLen >= 0)
                            logger.LogWarning("Encrypted packet: payload past msgSize (offset {Offset} packetSize {PacketSize} msgSize {MsgSize})", offset, payloadLen, msgSize);
                        break;
                    }

                    var typeRaw = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(payloadStart, 2));
                    var type = (PacketType)typeRaw;
                    int bodyLen = payloadLen - 2;
                    ReadOnlySpan<byte> payload = bodyLen > 0 ? cipher.AsSpan(payloadStart + 2, bodyLen) : [];
                    if (!SuppressedReceiveLogs.Contains(type))
                        logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, BitConverter.ToString(payload.ToArray()));
                    await channel.Writer.WriteAsync(new Packet(context, type, payload.ToArray(), typeRaw), ct);

                    offset = payloadEnd;
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
            _clients.TryRemove(context.Id, out _);
            onDisconnect?.Invoke(context.Id);
            logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
            context.Dispose();
        }
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
