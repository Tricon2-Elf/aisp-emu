using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemEquipEnded : IPacket<ItemEquipEnded>
{
    public uint ObjId { get; set; }

    public ItemEquipEnded(uint objId)
    {
        ObjId = objId;
    }

    public static ItemEquipEnded FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemEquipEnded(objId);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
