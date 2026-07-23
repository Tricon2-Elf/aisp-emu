namespace AISpace.Network.Data;

/// <summary>
/// Level and experience state read by the client's <c>sub_798B10</c>.
/// The same layout is used for normal character and cosplay progression.
/// </summary>
public sealed class LevelProgressData
{
    public const int WireSize = 25;

    public byte Level { get; set; }
    public ulong StatusPoints { get; set; }
    public ulong Experience { get; set; }
    public ulong ExperienceToNextLevel { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Level);
        writer.Write(StatusPoints);
        writer.Write(Experience);
        writer.Write(ExperienceToNextLevel);
        return writer.ToBytes();
    }

    public static LevelProgressData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException($"LevelProgressData requires at least {WireSize} bytes.", nameof(data));

        var reader = new PacketReader(data);
        return new LevelProgressData
        {
            Level = reader.ReadByte(),
            StatusPoints = reader.ReadULong(),
            Experience = reader.ReadULong(),
            ExperienceToNextLevel = reader.ReadULong(),
        };
    }
}
