using AISpace.Network;
using AISpace.Network.Data;

public class NpcNotifyData(uint result, uint npcObjectId, CharaData charaData) : IPacket<NpcNotifyData>
{
    private const int EntityPrefixSize = 366;
    private const int Sub798D80Size = 175;
    private const int Sub798B10Size = 25;

    public static NpcNotifyData FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(npcObjectId);

        var entity = charaData.ToBytes();
        int prefixLen = Math.Min(entity.Length, EntityPrefixSize);
        writer.Write(entity.AsSpan(0, prefixLen));
        if (prefixLen < EntityPrefixSize)
            writer.Write(new byte[EntityPrefixSize - prefixLen]);

        writer.Write(new byte[Sub798D80Size]);
        writer.Write(new byte[Sub798B10Size]);

        writer.Write((byte)0); // trailing byte read by ReadNpcData
        return writer.ToBytes();
    }
}
