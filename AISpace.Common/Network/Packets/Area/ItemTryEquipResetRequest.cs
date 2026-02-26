using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemTryEquipResetRequest(uint objId) : IPacket<ItemTryEquipResetRequest>
{
    public uint ObjId = objId;

    public static ItemTryEquipResetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemTryEquipResetRequest(objId);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
