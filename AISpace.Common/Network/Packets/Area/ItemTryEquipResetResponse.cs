using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class ItemTryEquipResetResponse(uint result) : IPacket<ItemTryEquipResetResponse>
{
    public uint Result = result;

    public static ItemTryEquipResetResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        return new ItemTryEquipResetResponse(result);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
