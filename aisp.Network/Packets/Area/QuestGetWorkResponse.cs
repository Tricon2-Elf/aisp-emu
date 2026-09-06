using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_quest_get_work_r (0x8D0C): u32 result, u32 count (max 9), then <see cref="QuestWorkData"/> rows.
/// </summary>
public sealed class QuestGetWorkResponse(uint result, IReadOnlyList<QuestWorkData> quests)
    : IOutgoingPacket
{
    public const int MaxCount = 9;

    public uint Result { get; } = result;
    public IReadOnlyList<QuestWorkData> Quests { get; } = quests;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        var count = Math.Min(Quests.Count, MaxCount);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            Quests[i].Write(writer);
        return writer.ToBytes();
    }

    public static QuestGetWorkResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        var count = reader.ReadUInt();
        if (count > MaxCount)
            throw new InvalidDataException($"Quest work count {count} exceeds {MaxCount}.");
        var quests = new QuestWorkData[count];
        for (var i = 0; i < count; i++)
            quests[i] = QuestWorkData.Read(ref reader);
        return new QuestGetWorkResponse(result, quests);
    }
}
