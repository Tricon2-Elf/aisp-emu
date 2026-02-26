using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class TrashboxCloseResponse(uint result) : IPacket<TrashboxCloseResponse>
{
    public uint Result = result;

    public static TrashboxCloseResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
