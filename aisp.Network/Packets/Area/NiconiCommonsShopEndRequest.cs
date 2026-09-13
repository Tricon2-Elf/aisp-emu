using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_niconi_commons_shop_end (0xCDF2). Wrapper 0x7AAF20, alloc 2 (opcode only).
/// </summary>
public sealed class NiconiCommonsShopEndRequest(byte[] raw)
    : IIncomingPacket<NiconiCommonsShopEndRequest>
{
    public byte[] Raw { get; } = raw;

    public static NiconiCommonsShopEndRequest FromBytes(ReadOnlySpan<byte> data) =>
        new(data.ToArray());
}
