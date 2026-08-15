using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>recv_notify_nicotv_set_movie (0x8E2A): nicotvid + null-terminated movie id.</summary>
public sealed class NotifyNicotvSetMovie(uint nicotvId, string movieId) : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;
    public string MovieId { get; } = movieId;

    public byte[] ToBytes()
    {
        if (MovieId.Length > NicotvData.MovieIdLength - 1)
            throw new InvalidOperationException(
                $"{nameof(MovieId)} cannot exceed {NicotvData.MovieIdLength - 1} characters."
            );

        var writer = new PacketWriter();
        writer.Write(NicotvId);
        writer.Write(MovieId);
        return writer.ToBytes();
    }
}
