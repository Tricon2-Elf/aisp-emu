namespace AISpace.Common.Network.Packets.Area;

public class MapLinkData
{
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public byte Yaw { get; set; }
    public float Length { get; set; }

    //Depth of the maplink. Maplink only shows as a link so the depth is actually invisible
    public float Depth { get; set; }

    public MapLinkData() { }

    public MapLinkData(float positionX, float positionY, float positionZ, byte yaw, float length, float depth)
    {
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        Yaw = yaw;
        Length = length;
        Depth = depth;
    }

    public void WriteTo(PacketWriter writer)
    {
        writer.Write(PositionX);
        writer.Write(PositionY);
        writer.Write(PositionZ);
        writer.Write(Yaw);
        writer.Write(Length);
        writer.Write(Depth);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        WriteTo(writer);
        return writer.ToBytes();
    }
}
