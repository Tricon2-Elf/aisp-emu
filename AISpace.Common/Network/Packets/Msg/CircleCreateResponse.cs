using AISpace.Common.Game;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleCreateResponse(uint result, CircleData? data) : IPacket<CircleCreateResponse>
{
    public static CircleCreateResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result); // 4 байта
        
        if (data != null && result == 0)
        {
            writer.Write(data.ToBytes()); // 866 байт
        }
        else
        {
            // Если ошибка, возвращаем пустышку 866 байт, чтобы не сломать парсер
            writer.Write(new byte[866]); 
        }

        return writer.ToBytes();
    }
}