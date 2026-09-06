namespace aisp.Network.Packets.Area;

/// <summary>send_quest_get_work (0xF582). Empty body; the client asks for the active-quest list.</summary>
public sealed class QuestWorkGetRequest : IIncomingPacket<QuestWorkGetRequest>
{
    public static QuestWorkGetRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
