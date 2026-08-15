using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class NotifyUpdateRoboState(
    uint roboId,
    uint objectId,
    uint state,
    CharacterMapData? map = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(objectId);
        writer.Write(state);
        // recv_notify_update_robo_state reads the same 30-byte chrmap layout used by CharaData.
        writer.Write((map ?? new CharacterMapData()).ToBytes());
        return writer.ToBytes();
    }
}
