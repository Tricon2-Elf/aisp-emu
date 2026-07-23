using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client maplink notify (recv_notify_maplink_data). Body: UInt Result (4) + MapLinkData (21) = 25 bytes.
/// </summary>
public class MapLinkNotifyData : IOutgoingPacket
{
    public uint Result { get; set; }
    public MapLinkData Data { get; set; } = new();

    public MapLinkNotifyData() { }

    public MapLinkNotifyData(uint result, MapLinkData data)
    {
        Result = result;
        Data = data;
    }

    public MapLinkNotifyData(uint result, float posX, float posY, float posZ, int yaw, float length, float halfExtent2)
    {
        Result = result;
        Data = new MapLinkData(posX, posY, posZ, yaw, length, halfExtent2);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        Data.WriteTo(writer);
        return writer.ToBytes();
    }
}
