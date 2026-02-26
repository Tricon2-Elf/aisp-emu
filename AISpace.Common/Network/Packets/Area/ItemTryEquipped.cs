using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemTryEquipped(uint objId, uint serialId, uint socketBit) : IPacket<ItemTryEquipped>
{
    public uint ObjId = objId;
    public uint SerialId = serialId;
    public uint SocketBit = socketBit;

    public static ItemTryEquipped FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        var serialId = reader.ReadUInt();
        var socketBit = reader.ReadUInt();
        return new ItemTryEquipped(objId, serialId, socketBit);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(SerialId);
        writer.Write(SocketBit);
        return writer.ToBytes();
    }
}
