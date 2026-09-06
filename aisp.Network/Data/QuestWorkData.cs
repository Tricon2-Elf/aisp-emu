namespace aisp.Network.Data;

/// <summary>
/// Active quest row for <c>recv_quest_started</c> / <c>recv_quest_get_work_r</c>.
/// Wire is 1147 bytes: <see cref="QuestBaseData"/> + u32 restsec, 97-byte location, 37-byte target, u16 now, u16 required.
/// </summary>
public sealed class QuestWorkData
{
    public const int WireSize = 1147;
    public const int LocationLength = 97;
    public const int TargetNameLength = 37;

    public QuestBaseData Base { get; set; } = new();
    public uint RestSec { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public ushort Now { get; set; }
    public ushort Required { get; set; }

    public static QuestWorkData Read(ref PacketReader reader) =>
        new()
        {
            Base = QuestBaseData.Read(ref reader),
            RestSec = reader.ReadUInt(),
            LocationName = reader.ReadFixedString(LocationLength),
            TargetName = reader.ReadFixedString(TargetNameLength),
            Now = reader.ReadUShort(),
            Required = reader.ReadUShort(),
        };

    public static QuestWorkData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return Read(ref reader);
    }

    public void Write(PacketWriter writer)
    {
        Base.Write(writer);
        writer.Write(RestSec);
        writer.WriteFixedString(LocationName, LocationLength);
        writer.WriteFixedString(TargetName, TargetNameLength);
        writer.Write(Now);
        writer.Write(Required);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        Write(writer);
        return writer.ToBytes();
    }
}
