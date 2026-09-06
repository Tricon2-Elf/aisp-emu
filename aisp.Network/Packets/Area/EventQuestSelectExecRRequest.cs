namespace aisp.Network.Packets.Area;

/// <summary>send_event_quest_select_exec_r (0x84E1): u32 result, u32 select_id.</summary>
public sealed class EventQuestSelectExecRRequest : IIncomingPacket<EventQuestSelectExecRRequest>
{
    public uint Result { get; init; }
    public uint SelectId { get; init; }

    public static EventQuestSelectExecRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventQuestSelectExecRRequest
        {
            Result = reader.ReadUInt(),
            SelectId = reader.ReadUInt(),
        };
    }
}
