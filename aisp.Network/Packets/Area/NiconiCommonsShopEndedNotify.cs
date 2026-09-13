namespace aisp.Network.Packets.Area;

/// <summary>recv_niconi_commons_shop_ended (0xC13B): empty. Window closes on this, not on end_r.</summary>
public sealed class NiconiCommonsShopEndedNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
