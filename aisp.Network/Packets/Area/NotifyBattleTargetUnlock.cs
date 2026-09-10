using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyBattleTargetUnlock(uint actionObjectId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(actionObjectId); // ID of the player who unlocked the target
        return writer.ToBytes();
    }
}
