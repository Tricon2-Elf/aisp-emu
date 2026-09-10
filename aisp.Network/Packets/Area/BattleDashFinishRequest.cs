using aisp.Network;

namespace aisp.Network.Packets.Area;

public class BattleDashFinishRequest : IIncomingPacket<BattleDashFinishRequest>
{
    public static BattleDashFinishRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
