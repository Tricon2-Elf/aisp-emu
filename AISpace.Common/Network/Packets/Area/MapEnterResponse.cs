using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class MapEnterResponse(uint result, uint mapId, float x, float y, float z, float rot) : IPacket<MapEnterResponse>
{
    public uint Result = result;
    public uint MapId = mapId;
    public float X = x;
    public float Y = y;
    public float Z = z;
    public float Rotation = rot;

    public static MapEnterResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);           // 0 = успех
        writer.Write(MapId);            // ID карты
        writer.Write(MapId);            // Serial ID (обычно совпадает с ID карты)
        writer.Write(X);                // Координата X
        writer.Write(Y);                // Координата Y
        writer.Write(Z);                // Координата Z
        writer.Write(Rotation);         // Поворот
        writer.Write((byte)0);          // Флаг состояния (0 = стоит)
        return writer.ToBytes();
    }
}