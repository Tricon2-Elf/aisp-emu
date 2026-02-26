using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemEquipStartRequest(uint objId) : IPacket<ItemEquipStartRequest>
{
    public uint ObjId = objId;

    public static ItemEquipStartRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemEquipStartRequest(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
