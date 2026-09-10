using aisp.Network;

namespace aisp.Network.Packets.Area;

public class BattleTargetUnlockResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
