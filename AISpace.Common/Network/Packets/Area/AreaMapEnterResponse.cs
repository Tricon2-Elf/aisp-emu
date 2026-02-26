namespace AISpace.Common.Network.Packets.Area;

public class AreaMapEnterResponse(uint Result) : IPacket<AreaMapEnterResponse>
{
    public static AreaMapEnterResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result); // 4 bytes
        return writer.ToBytes();
    }
}
