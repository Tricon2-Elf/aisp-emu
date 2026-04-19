namespace AISpace.Network.Data;

public class AvatarData(uint AvatarId, CharaData chara)
{
    public byte[] ToBytes()
    {
        PacketWriter writer = new();
        writer.Write(AvatarId); // 4 — first 4 bytes of AvatarData payload; client ReadAvatarData reads this as m_AvatarId
        writer.Write(chara.ToBytes()); // 383 (CharaData layout matches client ReadEntityData)
        writer.Write((ushort)8);
        writer.Write(new byte[539]); // 4+383+2+539 = 928
        return writer.ToBytes(); // 928 bytes
    }
}
