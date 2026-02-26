using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemEquipForceStarted(uint objId) : IPacket<ItemEquipForceStarted>
{
    public uint ObjId = objId;

    public static ItemEquipForceStarted FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemEquipForceStarted(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
