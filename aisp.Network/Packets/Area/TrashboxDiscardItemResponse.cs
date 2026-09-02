using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_trashbox_discard_item_r (0xBBEB). 4 bytes: UInt32 result. On receipt the client sends send_trashbox_close while the bin window is open, regardless of result.</summary>
public sealed class TrashboxDiscardItemResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
