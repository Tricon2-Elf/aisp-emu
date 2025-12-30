using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network;

public record AuthChannel(Channel<Packet> Channel);
public record MsgChannel(Channel<Packet> Channel);
public record AreaChannel(Channel<Packet> Channel);

public class TcpListenerService(ILogger<TcpListenerService> logger,
        Channel<Packet> channel, string Name,
        int port,
        ILoggerFactory loggerFactory) : BackgroundService
{
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
        if (n == 0) throw new EndOfStreamException();
        return buf[0];
    }


    private async Task HandleClientAsync(ClientConnection context)
    {
        logger.LogInformation("{name} Handling new Unencrypted client {Id}", Name, context.Id);
        using var stream = context.Stream;
        var buffer = new byte[4096];

        try
        {
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
                logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, BitConverter.ToString(payload));
                channel.Writer.TryWrite(new Packet(context, type, payload, typeShort));
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Client {Id} error: {Message}", context.Id, ex.Message);
        }

        logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
    }




    private async Task HandleCryptoClientAsync(ClientConnection context)
    {
        int headerSize = 4;
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

                // Loop through all messages in Packet
                int offset = 0;
                while (offset < msgSize)
                {
                    byte codecType = cipher[offset];
                    int msgLen = cipher[offset+1];
                    var typeRaw = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(offset + 2, 2));
                    var type = (PacketType)typeRaw;
                    ReadOnlySpan<byte> payload = cipher.AsSpan(offset + headerSize, msgLen - 2);

                    channel.Writer.TryWrite(new Packet(context, type, payload.ToArray(), typeRaw));

                    offset += headerSize + payload.Length;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Err {ex}", ex);
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], ct);
            if (read == 0) throw new IOException("Disconnected");
            totalRead += read;
        }
    }

}
