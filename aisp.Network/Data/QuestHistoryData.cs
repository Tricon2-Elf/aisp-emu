namespace aisp.Network.Data;

/// <summary>
/// Completed quest row for <c>recv_quest_add_history</c> / <c>recv_quest_get_history_r</c>
/// (<c>ais::notify_quest_history_t</c>). Wire is 1010 bytes: <see cref="QuestBaseData"/> + u32 related quest id, u8 result.
/// A non-zero related id hides the row unless another history entry with that quest id exists.
/// </summary>
public sealed class QuestHistoryData
{
    public const int WireSize = 1010;

    public QuestBaseData Base { get; set; } = new();
    public uint RelatedQuestId { get; set; }
    public byte Result { get; set; }

    public static QuestHistoryData Read(ref PacketReader reader) =>
        new()
        {
            Base = QuestBaseData.Read(ref reader),
            RelatedQuestId = reader.ReadUInt(),
            Result = reader.ReadByte(),
        };

    public static QuestHistoryData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"QuestHistoryData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        return Read(ref reader);
    }

    public void Write(PacketWriter writer)
    {
        Base.Write(writer);
        writer.Write(RelatedQuestId);
        writer.Write(Result);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        Write(writer);
        return writer.ToBytes();
    }
}
