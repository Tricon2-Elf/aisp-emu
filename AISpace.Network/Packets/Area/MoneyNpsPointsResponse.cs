using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MoneyNpsPointsResponse(uint result, ulong total, ulong limit) : IOutgoingPacket
{
    public uint Result = result;
    public ulong Total = total;
    public ulong Limit = limit;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Total);
        writer.Write(Limit);
        return writer.ToBytes();
    }
}
