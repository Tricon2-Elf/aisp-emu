namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_storage_furn_open_r (0x88C1). Server-pushed result before recv_storage_opened
/// when storage is opened from a My Room context (wardrobe / furniture).
/// Payload: UInt32 result (0 = success).
/// </summary>
public sealed class StorageFurnOpenResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
