namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_updated_chapter (0xA25C): u32 questid, u16 chapter, null-terminated note (max 769).</summary>
public sealed class QuestUpdatedChapterNotify(uint questId, ushort chapter, string note)
    : IOutgoingPacket
{
    public const int NoteMaxBytes = 769;

    public uint QuestId { get; } = questId;
    public ushort Chapter { get; } = chapter;
    public string Note { get; } = note;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(QuestId);
        writer.Write(Chapter);
        writer.Write(Note, NoteMaxBytes);
        return writer.ToBytes();
    }

    public static QuestUpdatedChapterNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new QuestUpdatedChapterNotify(
            reader.ReadUInt(),
            reader.ReadUShort(),
            reader.ReadString()
        );
    }
}
