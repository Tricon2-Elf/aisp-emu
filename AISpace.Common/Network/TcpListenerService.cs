using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using AISpace.Common.Game;
using AISpace.Common.Network.Crypto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network;

public record AuthChannel(Channel<Packet> Channel);

public record MsgChannel(Channel<Packet> Channel);

public record AreaChannel(Channel<Packet> Channel);

public class TcpListenerService(ILogger<TcpListenerService> logger, Channel<Packet> channel, string Name, int port, ILoggerFactory loggerFactory, SharedState state) : BackgroundService
{
    private static readonly HashSet<PacketType> SuppressedReceiveLogs = [PacketType.Ping, PacketType.AvatarMoveRequest];
    private readonly TcpListener _tcpListener = new(System.Net.IPAddress.Parse("0.0.0.0"), port);
    private readonly CancellationTokenSource _cts = new();

    public ChannelReader<Packet> PacketReader => channel.Reader;

    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();

    public override Task StopAsync(CancellationToken ct)
    {
        _cts.Cancel();
        _tcpListener.Stop();
        channel.Writer.Complete();
        return base.StopAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _tcpListener.Start();
        logger.LogInformation("Server {name} started on {LocalEP}", Name, _tcpListener.LocalEndpoint);

        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await _tcpListener.AcceptTcpClientAsync(_cts.Token);
            var context = new ClientConnection(Guid.NewGuid(), client.Client.RemoteEndPoint!, client.GetStream(), loggerFactory.CreateLogger<ClientConnection>());
            _clients[context.Id] = context;
            // Crappy encryption auto detection
            byte first = await PeekByteAsync(client.Client, ct);
            logger.LogInformation("First Byte! {b}", first);
            if (first != 0)
            {
                _ = HandleCryptoClientAsync(context);
            }
            else
            {
                context.encrypted = false;
                _ = HandleClientAsync(context);
            }
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

    private async Task HandleClientAsync(ClientConnection context)
    {
        logger.LogInformation("{name} Handling new Unencrypted client {Id}", Name, context.Id);
        try
        {
            using var stream = context.Stream;
            var buffer = new byte[4096];

            while (!_cts.Token.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(0, 1), _cts.Token);

                if (read == 0) //Client has disconnected
                    break;

                int packetLength = buffer[0];
                if (packetLength < 2)
                    continue;

                await ReadExactAsync(stream, buffer.AsMemory(0, 2), _cts.Token);
                ushort typeShort = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2));
                var type = (PacketType)typeShort;
                int payloadLength = packetLength - 2;
                byte[] payload = new byte[payloadLength];
                await ReadExactAsync(stream, payload, _cts.Token);
                if (!SuppressedReceiveLogs.Contains(type))
                    logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, BitConverter.ToString(payload));
                channel.Writer.TryWrite(new Packet(context, type, payload, typeShort));
            }
        }
        catch (Exception ex)
        {
            if (!IsExpectedDisconnect(ex))
                logger.LogError("Client {Id} error: {Message}", context.Id, ex.Message);
        }
        finally
        {
            _clients.TryRemove(context.Id, out _);
            state.UnregisterClient(Name, context.Id);
        }

        logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
    }

    private async Task HandleCryptoClientAsync(ClientConnection context)
    {
        logger.LogInformation("{name} Handling new Encrypted client {Id}", Name, context.Id);
        // Read RSA N value
        byte[] rsaN = new byte[16];
        await ReadExactAsync(context.Stream, rsaN, _cts.Token);
        // Create Server->Client key
        var (s2cPlain, s2cEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        // Create Client->Server key
        var (c2sPlain, c2sEnc) = CryptoUtils.CreateEncryptedKey(rsaN);
        // Set keys in Client context
        context.SetCamelliaKeys(s2cPlain, c2sPlain);
        // Send 'encrypted' keys back to client
        await context.SendRawAsync([.. s2cEnc, .. c2sEnc]);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var header = new byte[4];
                await ReadExactAsync(context.Stream, header, _cts.Token);
                int msgSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);

                int paddedSize = ((msgSize + 15) / 16) * 16;
                byte[] cipher = new byte[paddedSize];

                // Capture while packet
                await ReadExactAsync(context.Stream, cipher, _cts.Token);
                // Decrypt all packet
                context.DecryptBlocks(cipher);

                // Loop through all messages in Packet (VCE codec - see txtaisp/crates/aisp_server/src/net/vce_codec.rs).
                // First byte (codec): high nibble = type (0=PacketData, 1=Ping, 2=Pong, 3=Terminated, 4=DirectContact),
                //                   low nibble = size bytes for type 0 (0=1 byte, 1=2 bytes, 2=3, 3=4) so large payloads (e.g. CmdExec 3944) fit.
                // PacketData: [codec 1][len 1..4 bytes LE][payload] where payload = [type 2][body]; data_start = 2 + header_param.
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
                            //Ping or Pong
                            offset += 9;
                            continue;
                        }
                        if (headerType == 3 && msgSize - offset >= 5)
                        {
                            //Terminated
                            offset += 5;
                            continue;
                        }
                        break;
                    }

                    int sizeBytes = 1 + headerParam;
                    if (sizeBytes > 4)
                        sizeBytes = 4;
                    int payloadStartOffset = 2 + headerParam; // codec(1) + size(bytes 1..1+param); payload = [type 2][body] starts here
                    if (offset + payloadStartOffset > msgSize)
                        break;
                    int packetSize = cipher[offset + 1];
                    if (sizeBytes >= 2)
                        packetSize |= cipher[offset + 2] << 8;
                    if (sizeBytes >= 3)
                        packetSize |= cipher[offset + 3] << 16;
                    if (sizeBytes >= 4)
                        packetSize |= cipher[offset + 4] << 24;
                    int payloadLen = packetSize; // full payload = [type 2][body]
                    int payloadStart = offset + payloadStartOffset;
                    int payloadEnd = payloadStart + payloadLen;

                    if (payloadLen < 0 || payloadEnd > msgSize)
                    {
                        // Single-message block (no codec): [type 2][payload]
                        if (offset == 0 && msgSize >= 2)
                        {
                            var singleTypeRaw = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(0, 2));
                            var singleType = (PacketType)singleTypeRaw;
                            int singleBodyLen = msgSize - 2;
                            ReadOnlySpan<byte> singlePayload = singleBodyLen > 0 ? cipher.AsSpan(2, singleBodyLen) : [];
                            if (!SuppressedReceiveLogs.Contains(singleType))
                                logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", singleType, singlePayload.Length, BitConverter.ToString(singlePayload.ToArray()));
                            channel.Writer.TryWrite(new Packet(context, singleType, singlePayload.ToArray(), singleTypeRaw));
                        }
                        else if (payloadLen >= 0)
                            logger.LogWarning("Encrypted packet: payload past msgSize (offset {Offset} packetSize {PacketSize} msgSize {MsgSize})", offset, packetSize, msgSize);
                        break;
                    }

                    // Payload = [type 2][body]; we pass body to handler (same as before: packet payload does not include type)
                    var typeRaw = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(payloadStart, 2));
                    var type = (PacketType)typeRaw;
                    int bodyLen = payloadLen - 2;
                    ReadOnlySpan<byte> payload = bodyLen > 0 ? cipher.AsSpan(payloadStart + 2, bodyLen) : [];
                    if (!SuppressedReceiveLogs.Contains(type))
                        logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, BitConverter.ToString(payload.ToArray()));
                    channel.Writer.TryWrite(new Packet(context, type, payload.ToArray(), typeRaw));

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
            state.UnregisterClient(Name, context.Id);
            logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
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
