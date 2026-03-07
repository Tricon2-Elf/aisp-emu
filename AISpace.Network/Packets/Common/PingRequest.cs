using AISpace.Network;

namespace AISpace.Network.Packets.Common;

public class PingRequest(uint Time) : IPacket<PingRequest>
{
    public static PingRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        uint currentTime = reader.ReadUInt();
        return new PingRequest(currentTime);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Time); //Result
        return writer.ToBytes();
    }
}
