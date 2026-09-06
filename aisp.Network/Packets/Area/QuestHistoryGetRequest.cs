namespace aisp.Network.Packets.Area;

/// <summary>send_quest_get_history (0x4BED). Empty body; the client asks for completed quests.</summary>
public sealed class QuestHistoryGetRequest : IIncomingPacket<QuestHistoryGetRequest>
{
    public static QuestHistoryGetRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
