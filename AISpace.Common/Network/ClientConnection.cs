using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Network.Crypto;
using Microsoft.Extensions.Logging;

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
    const int MaxChunkSize = 1392; //Closest multiple of 16 to 1400 (Possible buffer for AI Sp@ce crypto?
    const int BlockSize = 16;
    private const byte HeaderPrefix = 0x03;
    private const int HeaderSize = 2;
    public VCECamellia128 C2S;
    public VCECamellia128 S2C;
    public bool encrypted = true;
    public ClientState CurrentState;
    public Guid Id = _Id;
    public EndPoint RemoteEndPoint = _RemoteEndPoint;
    public NetworkStream Stream = _ns;
    public int connectedChannel = 0;
    public DateTimeOffset lastPing;


    public bool IsAuthenticated => clientUser != null;
    public User? clientUser;
    public DateTimeOffset Connected { get; } = DateTimeOffset.UtcNow;

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default) => await Stream.WriteAsync(data, ct);

    public void SetCamelliaKeys(byte[] s2cKey, byte[] c2sKey)
    {
        C2S = new();
        S2C = new();
        S2C.Init(s2cKey);
        C2S.Init(c2sKey);
    }

    public void DecryptBlock(Span<byte> data)
    {
        if (data.Length % 16 != 0)
            throw new ArgumentException("Data length not multiple of 16");
        C2S.DecryptBlock(data);
    }

    public void DecryptBlocks(Span<byte> data)
    {
        if (data.Length % 16 != 0)
            throw new ArgumentException("Data length not multiple of 16");
        for (int offset = 0; offset < data.Length; offset += 16)
            DecryptBlock(data[offset..(offset + 16)]);
    }

    public void EncryptBlock(Span<byte> data)
    {
        if (data.Length % 16 != 0)
            throw new ArgumentException("Data length not multiple of 16");
        S2C.EncryptBlock(data);
    }

    public void EncryptBlocks(Span<byte> data)
    {
        if (data.Length % 16 != 0)
            throw new ArgumentException("Data length not multiple of 16");
        for (int offset = 0; offset < data.Length; offset += 16)
            EncryptBlock(data[offset..(offset + 16)]);

    }

    static byte[] PrefixLengthUInt32Le(ReadOnlySpan<byte> cipher, int innerSize)
    {
        var outBuf = new byte[4 + cipher.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(outBuf.AsSpan(0, 4), (uint)innerSize);
        cipher.CopyTo(outBuf.AsSpan(4));
        return outBuf;
    }

    public async Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default)
    {
        logger.LogInformation("Sending: {type}, {len}", type, payload.Length);
        try
        {
            var writer = new PacketWriter();
            ushort packetType = (ushort)type;
            uint packetLength = (uint)payload.Length + HeaderSize;
            writer.Write(HeaderPrefix); //1
            writer.Write(packetLength); //4
            writer.Write(packetType); //2
            writer.Write(payload);
            byte[] dataToSend = writer.ToBytes();

            if (!encrypted)
            {
                await SendRawAsync(dataToSend, ct);
                return;
            }


            int offset = 0;

            while (offset < dataToSend.Length)
            {
                int plainChunkSize = Math.Min(MaxChunkSize, dataToSend.Length - offset);

                ReadOnlySpan<byte> plainChunk =
                    dataToSend.AsSpan(offset, plainChunkSize);

                byte[] padded = PadToBlock(plainChunk, BlockSize);

                EncryptBlocks(padded);

                byte[] framed = PrefixLengthUInt32Le(padded, plainChunkSize);

                await SendRawAsync(framed, ct);

                offset += plainChunkSize;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Err {ex}", ex);
        }
    }

    static byte[] PadToBlock(ReadOnlySpan<byte> input, int blockSize)
    {
        int paddedLength = (input.Length + blockSize - 1) / blockSize * blockSize;
        var buffer = new byte[paddedLength];
        input.CopyTo(buffer);
        // zero padding is already there
        return buffer;
    }

    public async Task SendAsync<T>(PacketType type, IPacket<T> packet, CancellationToken ct = default) where T : IPacket<T> => await SendAsync(type, packet.ToBytes(), ct);
}
