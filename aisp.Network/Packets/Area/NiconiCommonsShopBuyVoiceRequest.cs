using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_niconi_commons_shop_buy_voice (0x2F55). Wrapper 0x7AB440, alloc 10:
/// UInt id + extra byte (same shape as buy_figure).
/// </summary>
public sealed class NiconiCommonsShopBuyVoiceRequest
    : IIncomingPacket<NiconiCommonsShopBuyVoiceRequest>
{
    public uint Id { get; init; }
    public byte Extra { get; init; }

    public static NiconiCommonsShopBuyVoiceRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var id = reader.ReadUInt();
        var extra = data.Length > 4 ? reader.ReadByte() : (byte)0;
        return new NiconiCommonsShopBuyVoiceRequest { Id = id, Extra = extra };
    }
}
