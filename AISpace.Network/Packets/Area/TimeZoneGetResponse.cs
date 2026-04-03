using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class TimeZoneGetResponse(uint Result, uint Timezone, uint Time, uint TimeZoneMax, byte Flag) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Timezone);
        writer.Write(Time);
        writer.Write(TimeZoneMax);
        writer.Write(Flag);
        return writer.ToBytes();
    }
}
