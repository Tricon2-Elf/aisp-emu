using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets;

public class TimeZoneGetResponse(uint result, uint timezone, uint time, uint timeZoneMax, byte flag) : IPacket<TimeZoneGetResponse>
{
    public uint Result = result;
    public uint Timezone = timezone;
    public uint Time = time;
    public uint TimeZoneMax = timeZoneMax;
    public byte Flag = flag;

    public static TimeZoneGetResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);      // 4 байта
        writer.Write(Timezone);    // 4 байта (0-4)
        writer.Write(Time);        // 4 байта (текущий прогресс)
        writer.Write(TimeZoneMax); // 4 байта (макс. прогресс периода)
        writer.Write(Flag);        // 1 байт (0 или 1 для ресинка)
        return writer.ToBytes();
    }
}