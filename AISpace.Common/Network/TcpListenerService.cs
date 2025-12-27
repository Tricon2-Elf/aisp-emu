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

public class TcpListenerService : BackgroundService
{
    private readonly ILogger<TcpListenerService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TcpListener _tcpListener;
    private readonly Channel<Packet> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool Encrypted = false;

    public ChannelReader<Packet> PacketReader => _channel.Reader;

    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();
    private readonly string name;

    public TcpListenerService(ILogger<TcpListenerService> logger,
            Channel<Packet> channel, string Name,
            int port,
            ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        name = Name;
        _tcpListener = new(System.Net.IPAddress.Parse("0.0.0.0"), port);
        _channel = channel;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _tcpListener.Stop();
        _channel.Writer.Complete();
        return base.StopAsync(cancellationToken);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _tcpListener.Start();
        _logger.LogInformation("Server {name} started on {LocalEP}", name, _tcpListener.LocalEndpoint);


        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await _tcpListener.AcceptTcpClientAsync(_cts.Token);
            var context = new ClientConnection(Guid.NewGuid(), client.Client.RemoteEndPoint!, client.GetStream(), _loggerFactory.CreateLogger<ClientConnection>());
            _clients[context.Id] = context;
            if (Encrypted)
                _ = HandleClientKeyExchangeAsync(context);
            else
                _ = HandleClientAsync(context);

        }
    }

    private async Task HandleClientKeyExchangeAsync(ClientConnection context)
    {
        int RsaSize = 16;
        context.CurrentState = ClientState.Init;
        _logger.LogInformation("New Client. Starting Key Exchange");
        //Do key stuff
        using var stream = context.Stream;
        var buffer = new byte[4096];
        _logger.LogInformation("Reading client RSA public key");
        // 1) read 16-byte RSA modulus from client
        int read = 0;
        while (read < RsaSize)
        {
            int r = await stream.ReadAsync(buffer.AsMemory(read, RsaSize - read), _cts.Token);
            if (r == 0) return; // client closed
            read += r;
        }
        byte[] camelliaKey = CryptoUtils.CreateCamelliaKey(buffer);
        context.SetCamelliaKey(camelliaKey);
        _logger.LogInformation("Sending new Camellia key back to client");
        //Move to CryptoUtils
        await context.SendRawAsync(camelliaKey);
        await context.SendRawAsync(camelliaKey);
        _logger.LogInformation("Handing over to normal HandleClient");
        _ = HandleClientAsync(context);
    }

    private async Task HandleClientAsync(ClientConnection context)
    {
        _logger.LogInformation("{name} Handling new client {Id}", name, context.Id);
        using var stream = context.Stream;
        var buffer = new byte[4096];

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                //try
                //{
                    int read = await stream.ReadAsync(buffer.AsMemory(0, 1), _cts.Token);

                    //If data is empty then break
                    if (read == 0)
                        break;


                    int packetLength = buffer[0];
                    if (packetLength < 2)
                        continue;

                    await ReadExactAsync(stream, buffer.AsMemory(0, 2), _cts.Token);
                    ushort typeShort = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2));
                    _logger.LogInformation("TypeShort: {type}", typeShort);
                    var type = (PacketType)typeShort;
                    _logger.LogInformation("EnumType: {type}", type);
                    int payloadLength = packetLength - 2;//2 due to packettype being 2 bytes

                    byte[] payload = new byte[payloadLength];

                    if (payloadLength > 0)
                        await ReadExactAsync(stream, payload, _cts.Token);

                var hex = BitConverter.ToString(payload).Replace("-", " ");
                _logger.LogInformation("Recieving packet {PacketType} ({Length} bytes): {Hex}", type, payload.Length, hex);
                //_logger.LogInformation("{name} Writing message to Channel {Id}", name, context.Id);
                //Need to check if PacketType is supported. If not send a logout?
                _channel.Writer.TryWrite(new Packet(context, type, payload, typeShort));
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError("Error in processing packet {msg}, {type}", ex.Message, typeof(Exception));
                //}
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Client {Id} error: {Message}", context.Id, ex.Message);
        }

        context.Stream.Close();
        _logger.LogInformation("Client disconnected: {RemoteEndPoint} ({Id})", context.RemoteEndPoint, context.Id);
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
