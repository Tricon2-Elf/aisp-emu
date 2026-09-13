using aisp.Network;

namespace aisp.Network.Data;

/// <summary>
/// 16-byte row in recv_niconi_commons_shop_item. Parser 0x79A180 reads four u32s into dest+0/+4/+8/+0xC:
/// type, id, デレ price, NP price (0x612540, 0x623DB3). Figure ids are shared 16-bit notebook registry ids.
/// </summary>
public sealed record NiconiCommonsShopItemRecord(uint Type, uint Id, uint AiPrice, uint NicoPrice)
{
    public const int WireSize = 16;

    public void Write(PacketWriter writer)
    {
        writer.Write(Type);
        writer.Write(Id);
        writer.Write(AiPrice);
        writer.Write(NicoPrice);
    }
}
