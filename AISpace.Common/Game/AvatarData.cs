namespace AISpace.Common.Game;

public class AvatarData(uint AvatarId, CharaData chara)
{
    public byte[] ToBytes()
    {
        var writer = new Network.PacketWriter();
        writer.Write(AvatarId);
        writer.Write(chara.ToBytes());
        writer.Write((ushort)8);
        writer.Write(new byte[539]); 
        
        return writer.ToBytes();
    }
}
