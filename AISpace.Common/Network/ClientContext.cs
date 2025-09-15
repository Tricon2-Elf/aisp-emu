using System.Net;
using System.Net.Sockets;
using NLog;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace AISpace.Common.Network;

public enum ClientState
{
    Init = 1,
}

public class ClientContext(Guid _Id, EndPoint _RemoteEndPoint, NetworkStream _ns, string _Version = "")
{
    private const byte HeaderPrefix = 0x03;
    private const int HeaderSize = 2;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public CamelliaEngine Camellia = new();
    public KeyParameter? CamelliaKey;
    public ClientState CurrentState;
    public Guid Id = _Id;
    public EndPoint RemoteEndPoint = _RemoteEndPoint;
    public NetworkStream Stream = _ns;
    public string Version = _Version;
    public DateTimeOffset lastPing;

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default)
    {
        await Stream.WriteAsync(data, ct);
    }

    public async Task SendAsync(PacketType type, byte[] data, CancellationToken ct = default)
    {
        var writer = new PacketWriter();
        ushort packetType = (ushort)type;

        writer.Write(HeaderPrefix);
        writer.Write((uint)data.Length + HeaderSize);
        writer.Write(packetType);
        writer.Write(data);
        byte[] dataToSend = writer.ToBytes();
        await Stream.WriteAsync(dataToSend, ct);
    }
}
