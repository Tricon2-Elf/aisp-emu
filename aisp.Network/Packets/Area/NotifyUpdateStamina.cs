using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// TPS stamina gauge (opcode 29852 / 0x749C). <paramref name="gauge"/> is 0–100
/// and <paramref name="speed"/> is the client-side drain or regen rate.
/// </summary>
public class NotifyUpdateStamina(uint objId, float gauge, float speed) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write(gauge);
        writer.Write(speed);
        return writer.ToBytes();
    }
}
