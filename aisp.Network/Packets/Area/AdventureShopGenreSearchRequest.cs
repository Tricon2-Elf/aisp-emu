using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_adventure_shop_genre_search (0x157F, wrapper 0x7A80F0): a genre tab click, a sort change or a page change
/// in the drama disc shop window. Four u32: genre tab (0-9), filter (always 0 in this client), sort (0-2 combo
/// index; a sort change resets the page to 0) and index (0-based page of 50 listings).
/// </summary>
public sealed class AdventureShopGenreSearchRequest(uint genre, uint filter, uint sort, uint index)
    : IIncomingPacket<AdventureShopGenreSearchRequest>
{
    public const int PageSize = 50;

    public uint Genre { get; } = genre;
    public uint Filter { get; } = filter;
    public uint Sort { get; } = sort;
    public uint Index { get; } = index;

    public static AdventureShopGenreSearchRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AdventureShopGenreSearchRequest(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt()
        );
    }
}
