using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_started (0xD46E). One packed <see cref="QuestWorkData"/> row.</summary>
public sealed class QuestStartedNotify(QuestWorkData quest) : IOutgoingPacket
{
    public QuestWorkData Quest { get; } = quest;

    public byte[] ToBytes() => Quest.ToBytes();

    public static QuestStartedNotify FromBytes(ReadOnlySpan<byte> data) =>
        new(QuestWorkData.FromBytes(data));
}
