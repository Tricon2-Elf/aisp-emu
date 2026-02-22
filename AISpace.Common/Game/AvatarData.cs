namespace AISpace.Common.Game;

public class AvatarData(uint AvatarId, CharaData chara)
{
    public byte[] ToBytes()
    {
        var writer = new Network.PacketWriter();
        writer.Write(AvatarId); // 4 байта
        writer.Write(chara.ToBytes()); // 383 байта
        writer.Write((ushort)8); // Флаг/разделитель (2 байта)
        
        // Паддинг: 928 - 4 - 383 - 2 = 539 байт
        writer.Write(new byte[539]); 
        
        return writer.ToBytes(); // Итого 928 байт
    }
}