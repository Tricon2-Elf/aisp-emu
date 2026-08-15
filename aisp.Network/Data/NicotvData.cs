namespace aisp.Network.Data;

public sealed class NicotvData(
    uint channelId = 0,
    string movieId = "",
    NicotvPlaybackState playbackState = NicotvPlaybackState.Closed,
    NicotvCommentVisibility commentVisibility = NicotvCommentVisibility.Visible
)
{
    public const int MovieIdLength = 97;
    public const int WireSize = sizeof(uint) + MovieIdLength + sizeof(uint) + sizeof(uint);

    public uint ChannelId { get; } = channelId;
    public string MovieId { get; } = movieId;
    public NicotvPlaybackState PlaybackState { get; } = playbackState;
    public NicotvCommentVisibility CommentVisibility { get; } = commentVisibility;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ChannelId);
        writer.WriteFixedAsciiString(MovieId, MovieIdLength);
        writer.Write((uint)PlaybackState);
        writer.Write((uint)CommentVisibility);
        return writer.ToBytes();
    }

    public static NicotvData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvData)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new NicotvData(
            reader.ReadUInt(),
            reader.ReadFixedString(MovieIdLength, "ASCII"),
            (NicotvPlaybackState)reader.ReadUInt(),
            (NicotvCommentVisibility)reader.ReadUInt()
        );
    }
}
