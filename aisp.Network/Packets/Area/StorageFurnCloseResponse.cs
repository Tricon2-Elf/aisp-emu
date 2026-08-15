namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_storage_furn_close_r (0x4E60). Sent with recv_storage_close_r when storage
/// was opened via a My Room furniture/wardrobe context.
/// Payload: UInt32 result (0 = success).
/// </summary>
public sealed class StorageFurnCloseResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
