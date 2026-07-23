using AISpace.Network;
using AISpace.Network.Data;

public class NpcNotifyData(uint result, uint npcObjectId, CharaData charaData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(npcObjectId);
        writer.Write(charaData.ToBytes());
        writer.Write((byte)0); // trailing byte read by ReadNpcData
        return writer.ToBytes();
    }
}
