using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_event_quest_select_exec (0x7640): null-terminated prompt (max 1537), u32 count (max 800), then questbase rows.
/// </summary>
public sealed class EventQuestSelectExecNotify(string text, IReadOnlyList<QuestBaseData> quests)
    : IOutgoingPacket
{
    public const int TextMaxBytes = 1537;
    public const int MaxCount = 800;

    public string Text { get; } = text;
    public IReadOnlyList<QuestBaseData> Quests { get; } = quests;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Text, TextMaxBytes);
        var count = Math.Min(Quests.Count, MaxCount);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            Quests[i].Write(writer);
        return writer.ToBytes();
    }

    public static EventQuestSelectExecNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var text = reader.ReadString();
        var count = reader.ReadUInt();
        if (count > MaxCount)
            throw new InvalidDataException($"Event quest select count {count} exceeds {MaxCount}.");
        var quests = new QuestBaseData[count];
        for (var i = 0; i < count; i++)
            quests[i] = QuestBaseData.Read(ref reader);
        return new EventQuestSelectExecNotify(text, quests);
    }
}
