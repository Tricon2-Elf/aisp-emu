using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_nicotv_set_movie (0xDDCA): nicotvid + null-terminated ASCII movie id (max 96 chars + NUL).
/// </summary>
public sealed class NicotvSetMovieRequest(uint nicotvId, string movieId)
    : IIncomingPacket<NicotvSetMovieRequest>
{
    public uint NicotvId { get; } = nicotvId;
    public string MovieId { get; } = movieId;

    public static NicotvSetMovieRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(uint) + 1)
            throw new InvalidDataException(
                $"{nameof(NicotvSetMovieRequest)} requires at least {sizeof(uint) + 1} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        var nicotvId = reader.ReadUInt();
        var movieId = reader.ReadString("ASCII");
        if (movieId.Length > NicotvData.MovieIdLength - 1)
            throw new InvalidDataException(
                $"{nameof(MovieId)} cannot exceed {NicotvData.MovieIdLength - 1} characters."
            );

        return new NicotvSetMovieRequest(nicotvId, movieId);
    }
}
