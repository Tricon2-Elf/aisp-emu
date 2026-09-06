using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_add_history (0xA8E1). One packed <see cref="QuestHistoryData"/> row.</summary>
public sealed class QuestAddHistoryNotify(QuestHistoryData history) : IOutgoingPacket
{
    public QuestHistoryData History { get; } = history;

    public byte[] ToBytes() => History.ToBytes();

    public static QuestAddHistoryNotify FromBytes(ReadOnlySpan<byte> data) =>
        new(QuestHistoryData.FromBytes(data));
}
