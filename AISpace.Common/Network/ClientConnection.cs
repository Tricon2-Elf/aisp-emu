using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Network.Crypto;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;

namespace AISpace.Common.Network;

public enum ClientState
{
    Init = 1,
    ConnectedToAuth = 2,
    ConnectedToMsg = 3,
    ConnectedToArea=4,
}

public class ClientConnection(Guid _Id, EndPoint _RemoteEndPoint, NetworkStream _ns, ILogger<ClientConnection> logger)
{
    private const byte HeaderPrefix = 0x03;
    private const int HeaderSize = 2;
    public VCECamellia128 C2S = new();
    public VCECamellia128 S2C = new();
    public ClientState CurrentState;
    public Guid Id = _Id;
    public EndPoint RemoteEndPoint = _RemoteEndPoint;
    public NetworkStream Stream = _ns;
    public int connectedChannel = 0;
    public DateTimeOffset lastPing;
    private readonly ILogger<ClientConnection> _logger = logger;


    public bool IsAuthenticated => clientUser != null;
    public User? clientUser;
    public DateTimeOffset Connected { get; } = DateTimeOffset.UtcNow;

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default) => await Stream.WriteAsync(data, ct);

    public void SetCamelliaKeys(byte[] s2cKey, byte[] c2sKey)
    {
        S2C.Init(s2cKey);
        C2S.Init(c2sKey);
    }

    public void DecryptBlock(Span<byte> block16)
    {
        C2S.DecryptBlock(block16);
    }

    public void EncryptBlock(Span<byte> block16)
    {
        S2C.EncryptBlock(block16);
    }

    public void EncryptBlocks(Span<byte> data)
    {
        if (data.Length % 16 != 0)
            throw new ArgumentException("Data length not multiple of 16");
        for (int offset = 0; offset < data.Length; offset += 16)
        {
            EncryptBlock(data[offset..(offset + 16)]);
            S2C.IncK0();
        }
    }

    byte[] PrefixLengthUInt32Le(ReadOnlySpan<byte> cipher)
    {
        var outBuf = new byte[4 + cipher.Length];
        logger.LogInformation("Adding Length of : {size}", (uint)cipher.Length);
        // length of cipher only (not including the 4-byte prefix)
        BinaryPrimitives.WriteUInt32LittleEndian(outBuf.AsSpan(0, 4), (uint)cipher.Length);

        cipher.CopyTo(outBuf.AsSpan(4));
        return outBuf;
    }

    public async Task SendAsync(PacketType type, byte[] data, CancellationToken ct = default)
    {
        try
        {
            var writer = new PacketWriter();
            ushort packetType = (ushort)type;
            uint packetLength = (uint)data.Length + HeaderSize;
            writer.Write(HeaderPrefix);
            writer.Write(packetLength);
            writer.Write(packetType);
            writer.Write(data);
            _logger.LogInformation("DataLength: {len}", data.Length);
            byte[] dataToSend = writer.ToBytes();

            var hex = BitConverter.ToString(dataToSend).Replace("-", " ");

            _logger.LogInformation("Block1: {len}", dataToSend.Length);
            //Encrypt the fucker
            var cipher = PadZeros(dataToSend.AsSpan(), 16);
            _logger.LogInformation("Block2: {len}", cipher.Length);
            EncryptBlocks(cipher);

            byte[] framed = PrefixLengthUInt32Le(cipher);

            var hex2 = BitConverter.ToString(framed).Replace("-", " ");
            _logger.LogInformation("Sending packet {PacketType} ({Length} bytes): {Hex}", type, dataToSend.Length, hex);
            _logger.LogInformation("Sending ENCRPY {PacketType} ({Length} bytes): {Hex}", type, framed.Length, hex2);
            await SendRawAsync(framed, ct);
        }
        catch (Exception ex)
        {
            logger.LogError("Err {ex}", ex);
        }
    }

    static byte[] PadZeros(ReadOnlySpan<byte> data, int blockSize = 16)
    {
        int rem = data.Length % blockSize;
        int pad = rem == 0 ? 0 : blockSize - rem;

        var outBuf = new byte[data.Length + pad];
        data.CopyTo(outBuf);
        // new bytes already zero
        return outBuf;
    }

    public async Task SendAsync<T>(PacketType type, IPacket<T> packet, CancellationToken ct = default) where T : IPacket<T> => await SendAsync(type, packet.ToBytes(), ct);
}
