namespace AISpace.Network.Packets.Area;

/// <summary>
/// Client-to-server area-map selector reply (send_event_areamap_select_exec_r).
/// Payload: UInt Result + UInt MapId + UInt ChannelId.
/// </summary>
public sealed class EventAreaMapSelectExecRRequest
{
    public uint Result { get; init; }
    public uint MapId { get; init; }
    public uint ChannelId { get; init; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(MapId);
        writer.Write(ChannelId);
        return writer.ToBytes();
    }

    public static EventAreaMapSelectExecRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventAreaMapSelectExecRRequest
        {
            Result = reader.ReadUInt(),
            MapId = reader.ReadUInt(),
            ChannelId = reader.ReadUInt(),
        };
    }
}
