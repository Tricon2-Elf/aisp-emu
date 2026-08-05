namespace AISpace.Network.Packets.Area;

/// <summary>recv_nicotv_set_movie_r (0x31B0): result + nicotvid.</summary>
public sealed class NicotvSetMovieResponse(uint result, uint nicotvId) : IOutgoingPacket
{
    public uint Result { get; } = result;
    public uint NicotvId { get; } = nicotvId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(NicotvId);
        return writer.ToBytes();
    }
}
