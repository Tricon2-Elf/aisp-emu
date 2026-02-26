using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemTryEquipFixResponse(uint result) : IPacket<ItemTryEquipFixResponse>
{
    public uint Result = result;

    public static ItemTryEquipFixResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        return new ItemTryEquipFixResponse(result);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
