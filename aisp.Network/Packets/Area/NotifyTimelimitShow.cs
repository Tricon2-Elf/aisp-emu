using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyTimelimitShow(uint tmEnd, uint remainSec) : IOutgoingPacket
{
    /// <summary>
    /// <c>tm_end</c> is a Unix timestamp. Sending 0 makes the client treat the
    /// timer as already expired, so the HUD never appears.
    /// </summary>
    public static NotifyTimelimitShow FromRemainingSeconds(uint remainSec)
    {
        var tmEnd = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + remainSec);
        return new NotifyTimelimitShow(tmEnd, remainSec);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(tmEnd);
        writer.Write(remainSec);
        return writer.ToBytes();
    }
}
