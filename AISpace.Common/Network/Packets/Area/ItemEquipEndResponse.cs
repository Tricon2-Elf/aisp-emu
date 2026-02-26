using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemEquipEndResponse : IPacket<ItemEquipEndResponse>
{
    public uint Result { get; set; }

    public ItemEquipEndResponse(uint result)
    {
        Result = result;
    }

    public static ItemEquipEndResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        return new ItemEquipEndResponse(result);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
