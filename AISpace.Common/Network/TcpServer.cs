using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using NLog;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace AISpace.Common.Network;
public class TcpServer(string IPAddress, int Port, bool Encrypted)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly TcpListener _listener = new(System.Net.IPAddress.Parse(IPAddress), Port);
    private readonly Channel<Packet> _packetChannel = Channel.CreateBounded<Packet>(1000);
    private readonly CancellationTokenSource _cts = new();

    public ChannelReader<Packet> PacketReader => _packetChannel.Reader;

    private readonly ConcurrentDictionary<Guid, ClientContext> _clients = new();

    public void Start()
    {
        _listener.Start();
        _logger.Info($"Server started on {_listener.LocalEndpoint}");

        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                var context = new ClientContext(Guid.NewGuid(), client.Client.RemoteEndPoint!, client.GetStream());
                _clients[context.Id] = context;
                if (Encrypted)
                    _ = HandleClientKeyExchangeAsync(context); // fire and forget
                else
                    _ = HandleClientAsync(context);

            }
        });
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
        _packetChannel.Writer.Complete();
    }

    private async Task HandleClientKeyExchangeAsync(ClientContext context)
    {
        context.CurrentState = ClientState.Init;
        _logger.Info("New Client. Starting Key Exchange");
        //Do key stuff
        using var stream = context.Stream;
        var buffer = new byte[4096];
        _logger.Info("Reading client RSA public key");
        // 1) read 16-byte RSA modulus from client
        int read = 0;
        while (read < 16)
        {
            int r = await stream.ReadAsync(buffer, read, 16 - read, _cts.Token);
            if (r == 0) return; // client closed
            read += r;
        }
        byte[] nLength = new byte[16];
        
        Array.Copy(buffer, nLength, 16);
        Array.Reverse(nLength);
        var n = new BigInteger(1, nLength.Reverse().ToArray());
        _logger.Info("Generating new Camellia key");
        var key = CryptoUtils.CreateKey(16, n);

        context.CamelliaKey = new KeyParameter(key);

        var camelliaInt = new BigInteger(1, key.Reverse().ToArray());
        BigInteger E = new BigInteger("65537"); // RSA e
        var encS2C = camelliaInt.ModPow(E, n);

        byte[] camelliaKey = ToFixedLe(encS2C, 16);
        _logger.Info("Sending new Camellia key back to client");
        await context.SendRawAsync(camelliaKey);
        await context.SendRawAsync(camelliaKey);
        context.CurrentState = ClientState.ServerReady;

        if (context.CurrentState == ClientState.ServerReady)
        {
            _logger.Info("Handing over to normal HandleClient");
            _ = HandleClientAsync(context);
        }
    }

    // Helper: encode BigInteger -> fixed-size LE
    private static byte[] ToFixedLe(BigInteger bi, int size)
    {
        var be = bi.ToByteArrayUnsigned(); // big-endian, no sign
        var le = new byte[size];
        int copy = Math.Min(be.Length, size);
        for (int i = 0; i < copy; i++)
            le[i] = be[be.Length - 1 - i]; // reverse
        return le;
    }

    private async Task HandleClientAsync(ClientContext context)
    {
        _logger.Info($"Handling new client {context.Id}");
        using var stream = context.Stream;
        var buffer = new byte[4096];

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // read length (1 byte)
                int read = await stream.ReadAsync(buffer.AsMemory(0, 1), _cts.Token);

                if (read == 0) break;

                int packetLength = buffer[0];
                if (packetLength < 2) continue;

                await ReadExactAsync(stream, buffer.AsMemory(0, 2), _cts.Token);
                ushort typeShort = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2));
                var type = (PacketType)typeShort;
                int payloadLength = packetLength - 2;//2 due to packettype being 2 bytes

                byte[] payload = new byte[payloadLength];

                if (payloadLength > 0)
                    await ReadExactAsync(stream, payload, _cts.Token);

                //Need to check if PacketType is supported. If not send a logout?
                _packetChannel.Writer.TryWrite(new Packet(context, type, payload, typeShort));
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Client {context.Id} error: {ex.Message}");
        }

        context.Stream.Close();
        _logger.Info($"Client disconnected: {context.RemoteEndPoint} ({context.Id})");
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
