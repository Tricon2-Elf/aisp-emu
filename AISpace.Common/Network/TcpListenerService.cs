using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network;

public class AuthChannel
{
    public Channel<Packet> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<Packet>();
}

public class AreaChannel
{
    public Channel<Packet> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<Packet>();
}

public class MsgChannel
{
    public Channel<Packet> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<Packet>();
}


public sealed class TCPServerConfig
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5005;
}
public class TcpListenerService<TChannel>(ILogger<TcpListenerService<TChannel>> logger,
                              Channel<Packet> channel,
                              TCPServerConfig config) : BackgroundService
{
    private readonly ILogger<TcpListenerService<TChannel>> _logger = logger;
    private readonly TcpListener _tcpListener = new(System.Net.IPAddress.Parse(config.BindAddress), config.Port);
    private readonly Channel<Packet> _channel = channel; //Channel.CreateBounded<Packet>(1000);
    private readonly CancellationTokenSource _cts = new();
    private readonly bool Encrypted = false;

    public ChannelReader<Packet> PacketReader => _channel.Reader;

    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();

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
        _logger.LogInformation("Server started on {LocalEP}", _tcpListener.LocalEndpoint);


        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await _tcpListener.AcceptTcpClientAsync(_cts.Token);
            var context = new ClientConnection(Guid.NewGuid(), client.Client.RemoteEndPoint!, client.GetStream());
            _clients[context.Id] = context;
            if (Encrypted)
                _ = HandleClientKeyExchangeAsync(context); // fire and forget
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
        _logger.LogInformation("Handling new client {Id}", context.Id);
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
                _channel.Writer.TryWrite(new Packet(context, type, payload, typeShort));
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
