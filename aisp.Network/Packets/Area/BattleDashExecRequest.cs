using aisp.Network;

namespace aisp.Network.Packets.Area;

public class BattleDashExecRequest : IIncomingPacket<BattleDashExecRequest>
{
    public static BattleDashExecRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
