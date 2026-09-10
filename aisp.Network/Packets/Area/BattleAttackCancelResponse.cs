using aisp.Network;

namespace aisp.Network.Packets.Area;

public class BattleAttackCancelResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
