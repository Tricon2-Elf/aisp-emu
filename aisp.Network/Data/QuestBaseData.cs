namespace aisp.Network.Data;

/// <summary>
/// Packed quest catalog row used by work, history, and <c>recv_event_quest_select_exec</c>.
/// Wire is 1005 bytes (no struct padding): u32 id, 193-byte title, 37-byte short name, u16 chapter, 769-byte note.
/// </summary>
public sealed class QuestBaseData
{
    public const int WireSize = 1005;
    public const int TitleLength = 193;
    public const int ShortNameLength = 37;
    public const int NoteLength = 769;

    public uint QuestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public ushort Chapter { get; set; }
    public string Note { get; set; } = string.Empty;

    public static QuestBaseData Read(ref PacketReader reader) =>
        new()
        {
            QuestId = reader.ReadUInt(),
            Title = reader.ReadFixedString(TitleLength),
            ShortName = reader.ReadFixedString(ShortNameLength),
            Chapter = reader.ReadUShort(),
            Note = reader.ReadFixedString(NoteLength),
        };

    public static QuestBaseData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        QuestBaseData quest = new()
        {
            QuestId = reader.ReadUInt(),
            Title = reader.ReadFixedString(TitleLength),
            ShortName = reader.ReadFixedString(ShortNameLength),
            Chapter = reader.ReadUShort(),
            Note = reader.ReadFixedString(NoteLength),
        };
        return quest;
    }

    public void Write(PacketWriter writer)
    {
        writer.Write(QuestId);
        writer.WriteFixedString(Title, TitleLength);
        writer.WriteFixedString(ShortName, ShortNameLength);
        writer.Write(Chapter);
        writer.WriteFixedString(Note, NoteLength);
    }
}
