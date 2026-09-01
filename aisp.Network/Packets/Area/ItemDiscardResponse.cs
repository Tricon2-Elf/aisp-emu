using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_item_discard_r (0x2546). 4 bytes: UInt32 result (0 = success). The bag itself is updated by item_update_num / item_delete.</summary>
public sealed class ItemDiscardResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
