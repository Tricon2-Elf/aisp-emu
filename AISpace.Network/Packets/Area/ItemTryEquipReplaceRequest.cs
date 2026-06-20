using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class ItemTryEquipReplaceRequest : IIncomingPacket<ItemTryEquipReplaceRequest>
{
    public const int MaxEquipCount = 30;

    public uint ObjId { get; set; }
    public IReadOnlyList<ItemEquipEntry> Equips { get; set; } = [];

    public static ItemTryEquipReplaceRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        var count = reader.ReadUInt();
        if (count > MaxEquipCount)
            throw new InvalidDataException($"EquipCount {count} exceeds max {MaxEquipCount}");

        var equips = new List<ItemEquipEntry>((int)count);
        for (var i = 0; i < count; i++)
            equips.Add(ItemEquipEntry.FromBytes(reader.ReadBytes(8)));

        return new ItemTryEquipReplaceRequest { ObjId = objId, Equips = equips };
    }
}
