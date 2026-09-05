using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_shop_ended (0xAD2D): empty body. The drama disc shop window closes on this, not on the end reply.</summary>
public sealed class AdventureShopEndedNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
