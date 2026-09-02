using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_adventure_upload_request (0x89F8): the 説明事項 window's 同意する. Listing metadata only; the packed
/// manuscript itself goes over HTTP (upload.php) once the server hands back a ticket in the reply.
/// Client wrapper 0x7A8E40: u16 workId, title (max 121 incl. NUL), u32 genre, comment (max 769), author name
/// (max 37), int64 price, u8 publish, int64 content size. The last field is the combined byte size of the
/// work's drama_N.csv and datalist_N.txt, the two parts of the HTTP upload that follows (verified: 20040 + 308).
/// </summary>
public sealed class AdventureUploadRequestRequest(
    ushort workId,
    string title,
    uint genre,
    string comment,
    string authorName,
    long price,
    byte publish,
    long contentSize
) : IIncomingPacket<AdventureUploadRequestRequest>
{
    public ushort WorkId { get; } = workId;
    public string Title { get; } = title;
    public uint Genre { get; } = genre;
    public string Comment { get; } = comment;
    public string AuthorName { get; } = authorName;
    public long Price { get; } = price;
    public byte Publish { get; } = publish;
    public long ContentSize { get; } = contentSize;

    public static AdventureUploadRequestRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var workId = reader.ReadUShort();
        var title = reader.ReadString();
        var genre = reader.ReadUInt();
        var comment = reader.ReadString();
        var authorName = reader.ReadString();
        var price = (long)reader.ReadULong();
        var publish = reader.ReadByte();
        var contentSize = (long)reader.ReadULong();
        return new AdventureUploadRequestRequest(
            workId,
            title,
            genre,
            comment,
            authorName,
            price,
            publish,
            contentSize
        );
    }
}
