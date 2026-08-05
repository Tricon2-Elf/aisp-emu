namespace AISpace.Network.Packets.Area;

/// <summary>send_nicotv_play (0x90B9): nicotvid + playback status.</summary>
public sealed class NicotvPlayRequest(uint nicotvId, uint status)
    : IIncomingPacket<NicotvPlayRequest>
{
    public const int WireSize = sizeof(uint) * 2;

    public uint NicotvId { get; } = nicotvId;
    public uint Status { get; } = status;

    public static NicotvPlayRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvPlayRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new NicotvPlayRequest(reader.ReadUInt(), reader.ReadUInt());
    }
}
