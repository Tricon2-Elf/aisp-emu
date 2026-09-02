using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_adventure_work_add_sheet (0x2FFF) / send_adventure_work_sub_sheet (0x4187): UShort WorkId, UInt Count, then a trailing UShort.
/// </summary>
public sealed class AdventureWorkSheetRequest(ushort workId, uint count)
    : IIncomingPacket<AdventureWorkSheetRequest>
{
    public ushort WorkId { get; } = workId;
    public uint Count { get; } = count;

    public static AdventureWorkSheetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var id = data.Length >= 2 ? reader.ReadUShort() : (ushort)0;
        var count = data.Length >= 6 ? reader.ReadUInt() : 1u;
        return new AdventureWorkSheetRequest(id, count);
    }
}
