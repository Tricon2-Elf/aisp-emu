using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemTryEquipFixRequest(uint objId) : IPacket<ItemTryEquipFixRequest>
{
    public uint ObjId = objId;

    public static ItemTryEquipFixRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemTryEquipFixRequest(objId);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
