using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Client-to-server in-event island selection reply (send_event_island_select_exec_r).
/// Payload: UInt Result + UInt IslandId + UInt ChannelId.
/// </summary>
public sealed class EventIslandSelectExecRRequest : IIncomingPacket<EventIslandSelectExecRRequest>
{
    public uint Result { get; init; }
    public uint IslandId { get; init; }
    public uint ChannelId { get; init; }

    public static EventIslandSelectExecRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventIslandSelectExecRRequest
        {
            Result = reader.ReadUInt(),
            IslandId = reader.ReadUInt(),
            ChannelId = reader.ReadUInt(),
        };
    }
}
