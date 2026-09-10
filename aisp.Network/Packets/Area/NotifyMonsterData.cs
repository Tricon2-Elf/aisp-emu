using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class NotifyMonsterData(MonsterData monsterData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        return monsterData.ToBytes();
    }
}
