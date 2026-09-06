using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_quest_get_history_r (0x32A4): u32 result, u32 count (max 800), then <see cref="QuestHistoryData"/> rows.
/// </summary>
public sealed class QuestGetHistoryResponse(uint result, IReadOnlyList<QuestHistoryData> history)
    : IOutgoingPacket
{
    public const int MaxCount = 800;

    public uint Result { get; } = result;
    public IReadOnlyList<QuestHistoryData> History { get; } = history;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        var count = Math.Min(History.Count, MaxCount);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            History[i].Write(writer);
        return writer.ToBytes();
    }

    public static QuestGetHistoryResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        var count = reader.ReadUInt();
        if (count > MaxCount)
            throw new InvalidDataException($"Quest history count {count} exceeds {MaxCount}.");
        var history = new QuestHistoryData[count];
        for (var i = 0; i < count; i++)
            history[i] = QuestHistoryData.Read(ref reader);
        return new QuestGetHistoryResponse(result, history);
    }
}
