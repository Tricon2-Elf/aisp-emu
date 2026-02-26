using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets;

public class TimeZoneGetResponse(uint result, uint timezone, float time, float timeZoneMax, byte flag) : IPacket<TimeZoneGetResponse>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(timezone);
        writer.Write(time);
        writer.Write(timeZoneMax);
        writer.Write(flag);
        return writer.ToBytes();
    }

    public static TimeZoneGetResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
