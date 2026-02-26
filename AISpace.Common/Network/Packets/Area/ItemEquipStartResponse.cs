using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemEquipStartResponse(uint result) : IPacket<ItemEquipStartResponse>
{
    public uint Result = result;

    public static ItemEquipStartResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemEquipStartResponse(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
