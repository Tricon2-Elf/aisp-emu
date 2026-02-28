namespace AISpace.Common.Network.Packets.Area;

public class MapLinkGetDataResponse : IPacket<MapLinkGetDataResponse>
{
    public uint Result { get; set; }

    public MapLinkGetDataResponse() { }

    public MapLinkGetDataResponse(uint result)
    {
        Result = result;
    }

    public static MapLinkGetDataResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MapLinkGetDataResponse(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
