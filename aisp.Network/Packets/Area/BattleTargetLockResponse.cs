using aisp.Network;

namespace aisp.Network.Packets.Area;

public class BattleTargetLockResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result); // Exactly 4 bytes!
        return writer.ToBytes();
    }
}
