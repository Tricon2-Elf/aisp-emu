namespace AISpace.Network.Data;

/// <summary>
/// Wire size is 285 bytes (in-memory struct is 288 with 3 bytes padding before Params).
/// Populates the client item-limit map used by wardrobe equip checks (sub_406E60).
/// </summary>
public class CharaOrderData(uint category, byte limitByte1 = 0, byte limitByte2 = 0)
{
    public uint Category { get; set; } = category;
    public byte LimitByte1 { get; set; } = limitByte1;
    public byte LimitByte2 { get; set; } = limitByte2;

    public static IReadOnlyList<CharaOrderData> DefaultClothingOrders { get; } =
    [
        new(101, 1, 1), // shirt
        new(102, 1, 2), // pants / skirt
        new(103, 2, 1), // gloves
        new(104, 2, 2), // socks
        new(106, 0, 1), // bra
        new(105, 0, 2), // shoes
        new(200), // accessories / misc
    ];

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Category);
        writer.Write(new byte[193]);
        writer.Write(LimitByte1);
        writer.Write(LimitByte2);
        writer.Write((byte)0);
        writer.Write((byte)0);
        // Params follow immediately on the wire (no padding before 0xCC in-memory offset).
        for (var i = 0; i < 20; i++)
            writer.Write(0u);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
