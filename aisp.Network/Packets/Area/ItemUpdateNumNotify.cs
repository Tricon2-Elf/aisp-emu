using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_item_update_num (0x05F8). 10 bytes: UInt place, UInt serialId, UShort num.
/// Client (CAIProtoArea slot 92 → 0x794C80) sets the per-place count of the catalog record keyed by serial
/// and refreshes the item window; num == 0 should be sent as item_delete instead so the owned list is unlinked.
/// </summary>
public sealed class ItemUpdateNumNotify(uint place, uint serialId, ushort num) : IOutgoingPacket
{
    public uint Place { get; } = place;
    public uint SerialId { get; } = serialId;
    public ushort Num { get; } = num;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Place);
        writer.Write(SerialId);
        writer.Write(Num);
        return writer.ToBytes();
    }
}
