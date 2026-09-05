using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_shop_end (0xB34F): the drama disc shop window (ドラマショップ 販売) is being closed. No body.</summary>
public sealed class AdventureShopEndRequest(byte[] raw) : IIncomingPacket<AdventureShopEndRequest>
{
    public byte[] Raw { get; } = raw;

    public static AdventureShopEndRequest FromBytes(ReadOnlySpan<byte> data) => new(data.ToArray());
}
