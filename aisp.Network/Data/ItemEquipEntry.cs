namespace aisp.Network.Data;

/// <summary>
/// Wire format for item_equip_t (SerialId + SocketBit).
/// </summary>
public class ItemEquipEntry(uint itemId, uint socketBit)
{
    public uint ItemId { get; set; } = itemId;
    public uint SocketBit { get; set; } = socketBit;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ItemId);
        writer.Write(SocketBit);
        return writer.ToBytes();
    }

    public static ItemEquipEntry FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemEquipEntry(reader.ReadUInt(), reader.ReadUInt());
    }
}
