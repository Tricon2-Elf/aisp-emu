using AISpace.Common.Game;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleGetDataResponse(uint result, List<CircleData> circles) : IPacket<CircleGetDataResponse>
{
    public static CircleGetDataResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        
        // 1. Result (4 байта)
        writer.Write(result); 
        
        // 2. Count of Circles (4 байта)
        writer.Write((uint)circles.Count); 
        
        // 3. CircleData array (каждый по 866 байт)
        foreach(var c in circles)
        {
            writer.Write(c.ToBytes());
        }

        // 4. AuthLevel Count (4 байта)
        writer.Write((uint)circles.Count);

        // 5. AuthLevel array (каждый по 4 байта)
        foreach(var c in circles)
        {
            writer.Write((uint)1); // 1 = Лидер
        }

        // ПАДДИНГА В КОНЦЕ ПАКЕТА БЫТЬ НЕ ДОЛЖНО!
        return writer.ToBytes();
    }
}