using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemEquipEndRequest : IPacket<ItemEquipEndRequest>
{
    public uint ObjId { get; set; }

    public ItemEquipEndRequest(uint objId)
    {
        ObjId = objId;
    }

    public static ItemEquipEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemEquipEndRequest(objId);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
