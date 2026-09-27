using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_mission_outmap_choice_open (0x171B): empty body. Opens the
/// leave-mission confirm on the client.
/// </summary>
public sealed class NotifyMissionOutmapChoiceOpen : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
