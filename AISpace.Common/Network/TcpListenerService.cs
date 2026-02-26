using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using AISpace.Common.Network.Crypto;
using AISpace.Common.Network.Packets;
using AISpace.Common.Game;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network;

public record AuthChannel(Channel<Packet> Channel);
public record MsgChannel(Channel<Packet> Channel);
public record AreaChannel(Channel<Packet> Channel);

public class TcpListenerService(ILogger<TcpListenerService> logger, Channel<Packet> channel, string Name, int port, ILoggerFactory loggerFactory, SharedState sharedState) : BackgroundService
{
    private readonly TcpListener _tcpListener = new(System.Net.IPAddress.Parse("0.0.0.0"), port);
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<Guid, ClientConnection> _localClients = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _tcpListener.Start();
        logger.LogInformation("Server {name} started on port {port}", Name, port);

        while (!_cts.Token.IsCancellationRequested)
        {
            try 
            {
                var client = await _tcpListener.AcceptTcpClientAsync(_cts.Token);
                var context = new ClientConnection(Guid.NewGuid(), client.Client.RemoteEndPoint!, client.GetStream(), loggerFactory.CreateLogger<ClientConnection>());
                
                _localClients[context.Id] = context;
                sharedState.RegisterClient(Name, context);
                
                _ = HandleClientLifecycleAsync(client, context, ct);
            } 
            catch { }
        }
    }

    private async Task HandleClientLifecycleAsync(TcpClient client, ClientConnection context, CancellationToken ct)
    {
        try 
        {
            byte first = await PeekByteAsync(client.Client, ct);
            if (first != 0) await HandleCryptoClientAsync(context);
            else { context.encrypted = false; await HandleClientAsync(context); }
        }
        catch (Exception) { }
        finally 
        {
            CleanupClient(context); 
            client.Close();
        }
    }

    private void CleanupClient(ClientConnection context)
    {
        sharedState.UnregisterClient(Name, context.Id);
        _localClients.TryRemove(context.Id, out _);
    }

    private async Task HandleClientAsync(ClientConnection context)
    {
        var buffer = new byte[8192];
        while (!_cts.Token.IsCancellationRequested)
        {
            int read = await context.Stream.ReadAsync(buffer.AsMemory(0, 1), _cts.Token);
            if (read == 0) break;
            
            int pLen = buffer[0];
            await ReadExactAsync(context.Stream, buffer.AsMemory(0, 2), _cts.Token);
            ushort type = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2));
            
            byte[] payload = new byte[pLen - 2];
            await ReadExactAsync(context.Stream, payload, _cts.Token);
            
            channel.Writer.TryWrite(new Packet(context, (PacketType)type, payload, type));
        }
    }

    private async Task HandleCryptoClientAsync(ClientConnection context)
    {
        byte[] rsaN = new byte[16];
        await ReadExactAsync(context.Stream, rsaN, _cts.Token);
        var (s2cP, s2cE) = CryptoUtils.CreateEncryptedKey(rsaN);
        var (c2sP, c2sE) = CryptoUtils.CreateEncryptedKey(rsaN);
        context.SetCamelliaKeys(s2cP, c2sP);
        await context.SendRawAsync([.. s2cE, .. c2sE]);

        var header = new byte[4];
        while (!_cts.Token.IsCancellationRequested)
        {
            await ReadExactAsync(context.Stream, header, _cts.Token);
            int msgSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
            
            if (msgSize > 2000000 || msgSize < 0) throw new IOException("Packet too big");

            byte[] cipher = new byte[((msgSize + 15) / 16) * 16];
            await ReadExactAsync(context.Stream, cipher, _cts.Token);
            context.DecryptBlocks(cipher);

            int offset = 0;
            while (offset < msgSize)
            {
                if (offset + 2 > msgSize) break;
                byte codec = cipher[offset];
                int hType = (codec >> 4) & 0xF;
                
                if (hType != 0) { 
                    offset += (hType <= 2) ? 9 : 5; 
                    continue; 
                }
                
                int pStart = 2 + (codec & 0xF);
                int pLen = cipher[offset + 1];
                if ((codec & 0xF) >= 1) pLen |= cipher[offset + 2] << 8;
                if ((codec & 0xF) >= 2) pLen |= cipher[offset + 3] << 16;

                if (offset + pStart + 2 > msgSize) break; 

                ushort type = BinaryPrimitives.ReadUInt16LittleEndian(cipher.AsSpan(offset + pStart, 2));
                int payloadSize = pLen - 2;
                byte[] payload = payloadSize > 0 ? cipher.AsSpan(offset + pStart + 2, payloadSize).ToArray() : [];
                
                if ((PacketType)type != PacketType.Ping && (PacketType)type != PacketType.TimeZoneGetRequest) {
                    logger.LogInformation("[RECV] {Server} Type: {Type} (0x{RawType:X4}), Len: {Len}", 
                        Name, (PacketType)type, type, payload.Length);
                }

                channel.Writer.TryWrite(new Packet(context, (PacketType)type, payload, type));
                offset += pStart + pLen;
            }
        }
    }

    private async Task ReadExactAsync(NetworkStream s, Memory<byte> b, CancellationToken c) {
        int t = 0; while (t < b.Length) { int r = await s.ReadAsync(b[t..], c); if (r == 0) throw new IOException(); t += r; }
    }

    static async ValueTask<byte> PeekByteAsync(Socket s, CancellationToken c) {
        var b = new byte[1]; if (await s.ReceiveAsync(b, SocketFlags.Peek, c) == 0) throw new IOException(); return b[0];
    }
}
