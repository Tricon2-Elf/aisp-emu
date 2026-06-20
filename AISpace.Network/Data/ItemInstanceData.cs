namespace AISpace.Network.Data;

/// <summary>
/// Wire format for item_t used by recv_item_create.
/// </summary>
public readonly struct ItemInstanceData(uint place, uint serialId, ushort num, uint itemId, ulong expireAt = 0)
{
    public uint Place { get; } = place;
    public uint SerialId { get; } = serialId;
    public ushort Num { get; } = num;
    public uint ItemId { get; } = itemId;
    public ulong ExpireAt { get; } = expireAt;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Place);
        writer.Write(SerialId);
        writer.Write(Num);
        writer.Write(ItemId);
        writer.Write(ExpireAt);
        return writer.ToBytes();
    }
}
