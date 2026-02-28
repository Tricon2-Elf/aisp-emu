namespace AISpace.Common.Network.Packets.Area;

/// <summary>
/// Server-to-client maplink notify (recv_notify_maplink_data). Body: UInt Result (4) + MapLinkData (21) = 25 bytes.
/// </summary>
public class MapLinkNotifyData : IPacket<MapLinkNotifyData>
{
    public uint Result { get; set; }
    public MapLinkData Data { get; set; } = new();

    public MapLinkNotifyData() { }

    public MapLinkNotifyData(uint result, MapLinkData data)
    {
        Result = result;
        Data = data;
    }

    public MapLinkNotifyData(uint result, float posX, float posY, float posZ, byte yaw, float length, float halfExtent2)
    {
        Result = result;
        Data = new MapLinkData(posX, posY, posZ, yaw, length, halfExtent2);
    }

    public static MapLinkNotifyData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        var mapLinkData = new MapLinkData
        {
            PositionX = reader.ReadFloat(),
            PositionY = reader.ReadFloat(),
            PositionZ = reader.ReadFloat(),
            Yaw = reader.ReadByte(),
            Length = reader.ReadFloat(),
            Depth = reader.ReadFloat(),
        };
        return new MapLinkNotifyData(result, mapLinkData);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        Data.WriteTo(writer);
        return writer.ToBytes();
    }
}
