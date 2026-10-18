using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_update_move_speed (0xB091): object id plus the two speed floats
/// the client stores for that character. Defaults are 500, which is why a short
/// path is covered almost immediately.
/// </summary>
public class NotifyUpdateMoveSpeed(uint objId, float walkSpeed, float runSpeed) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write(walkSpeed);
        writer.Write(runSpeed);
        return writer.ToBytes();
    }
}
