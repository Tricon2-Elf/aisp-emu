using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_mission_situation_message: null-terminated message, max 91 bytes
/// including NUL (decomp <c>ReadNullTerminated(..., 91)</c>).
/// </summary>
public sealed class NotifyMissionSituationMessage(string message) : IOutgoingPacket
{
    public const int MaxBytesIncludingNul = 91;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(message, MaxBytesIncludingNul - 1, "Shift_JIS");
        return writer.ToBytes();
    }
}
