using AISpace.Network;

namespace AISpace.Network.Packets.Common;

public class PingResponse(uint Time) : IPacket<PingResponse>
{
    public static PingResponse FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        uint currentTime = reader.ReadUInt();
        return new PingResponse(currentTime);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Time); //Result
        return writer.ToBytes();
    }
}
