using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_ucc_adv_figure_obtain (0x13DE): UInt figureId (picker key, not the bag item id).
/// Parser 0x7BFC76. Pushed after a successful figure buy so the picker updates without a relog.
/// </summary>
public sealed class UccAdvFigureObtainNotify(uint figureId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(figureId);
        return writer.ToBytes();
    }
}
