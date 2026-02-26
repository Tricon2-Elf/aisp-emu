using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class TrashboxOpenResponse : IPacket<TrashboxOpenResponse>
{
    public uint Result { get; set; }

    public TrashboxOpenResponse(uint result)
    {
        Result = result;
    }

    public static TrashboxOpenResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new TrashboxOpenResponse(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
