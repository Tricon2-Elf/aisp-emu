using System;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class MoneyNpsPointsResponse(uint result, ulong total, ulong limit) : IPacket<MoneyNpsPointsResponse>
{
    public uint Result = result;
    public ulong Total = total;
    public ulong Limit = limit;

    public static MoneyNpsPointsResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Total);
        writer.Write(Limit);
        return writer.ToBytes();
    }
}
