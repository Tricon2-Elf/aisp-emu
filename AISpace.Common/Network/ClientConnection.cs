using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Network.Crypto;
using AISpace.Common.Network.Packets;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network;

public class ClientConnection(Guid _Id, EndPoint _RemoteEndPoint, NetworkStream _ns, ILogger<ClientConnection> logger)
{
    private const byte HeaderPrefix = 0x03;
    public VCECamellia128? C2S, S2C;
    public bool encrypted = true;
    public Guid Id = _Id;
    public NetworkStream Stream = _ns;
    public EndPoint RemoteEndPoint = _RemoteEndPoint;
    public ILogger Logger = logger;
    public User? User;

    public uint CharacterId { get; set; } = 0;
    public float X { get; set; } = 0f;
    public float Y { get; set; } = 0f;
    public float Z { get; set; } = 0f;
    public sbyte Rotation { get; set; } = 0;
    public MovementType CurrentAnimation { get; set; } = MovementType.Stopped;

    public bool IsAuthenticated => User != null;

    public void SetCamelliaKeys(byte[] s2c, byte[] c2s) {
        C2S = new(); S2C = new(); S2C.Init(s2c); C2S.Init(c2s);
    }

    public void DecryptBlocks(Span<byte> data) {
        if (C2S == null) return;
        for (int i = 0; i < data.Length; i += 16) C2S.DecryptBlock(data[i..(i + 16)]);
    }

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default) => await Stream.WriteAsync(data, ct);

    public async Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default) {
        try {
            var writer = new PacketWriter();
            writer.Write(HeaderPrefix);
            writer.Write((uint)payload.Length + 2);
            writer.Write((ushort)type);
            writer.Write(payload);
            byte[] data = writer.ToBytes();
            if (!encrypted) { await Stream.WriteAsync(data, ct); return; }

            int pSize = ((data.Length + 15) / 16) * 16;
            byte[] padded = new byte[pSize];
            Array.Copy(data, padded, data.Length);
            for (int i = 0; i < padded.Length; i += 16) S2C!.EncryptBlock(padded.AsSpan(i, 16));

            byte[] final = new byte[4 + padded.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(final, (uint)data.Length);
            Array.Copy(padded, 0, final, 4, padded.Length);
            await Stream.WriteAsync(final, ct);
            await Stream.FlushAsync(ct);
        } catch { }
    }

    public async Task SendAsync<T>(PacketType type, IPacket<T> packet, CancellationToken ct = default) 
        where T : IPacket<T> => await SendAsync(type, packet.ToBytes(), ct);
}