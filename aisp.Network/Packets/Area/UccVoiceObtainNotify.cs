using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_ucc_voice_obtain (0xCDF6). Same one-u32 shape as recv_ucc_adv_figure_obtain (0x13DE / 0x7BFC76).
/// Pushed after a successful voice buy so the voice list updates without a relog.
/// </summary>
public sealed class UccVoiceObtainNotify(uint voiceId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(voiceId);
        return writer.ToBytes();
    }
}
