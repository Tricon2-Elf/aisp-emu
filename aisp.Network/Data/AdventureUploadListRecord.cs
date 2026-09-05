namespace aisp.Network.Data;

/// <summary>
/// One record of recv_get_adventure_upload_list_r (parser 0x7998A0; 0x630 bytes in memory, 1574 on the wire):
/// int64 scriptId, char[37] author, char[121] title, int64 price, char[769] comment, u8 contents-public flag, u32 genre,
/// int64 file size, u32, 10×char[61] tags, u32. The names after the title follow the upload request's field order;
/// the client only stores the record.
/// </summary>
public sealed class AdventureUploadListRecord
{
    public const int WireSize = 1574;
    public const int CommentLength = 769;

    public long ScriptId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public long Price { get; init; }
    public string Comment { get; init; } = string.Empty;
    public byte ContentsPublic { get; init; }
    public uint Genre { get; init; }
    public long FileSize { get; init; }
    public uint Sales { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public uint UploadedAt { get; init; }

    public void WriteTo(PacketWriter writer)
    {
        writer.Write((ulong)ScriptId);
        writer.WriteFixedStringNulTerminated(AuthorName, AdventureShopItemRecord.AuthorNameLength);
        writer.WriteFixedStringNulTerminated(Title, AdventureShopItemRecord.TitleLength);
        writer.Write((ulong)Price);
        writer.WriteFixedStringNulTerminated(Comment, CommentLength);
        writer.Write(ContentsPublic);
        writer.Write(Genre);
        writer.Write((ulong)FileSize);
        writer.Write(Sales);
        for (var i = 0; i < AdventureShopItemRecord.TagCount; i++)
            writer.WriteFixedStringNulTerminated(
                i < Tags.Count ? Tags[i] : "",
                AdventureShopItemRecord.TagLength
            );
        writer.Write(UploadedAt);
    }
}

/// <summary>
/// One record of recv_get_adventure_download_list_r (parser 0x799A80; 17 bytes on the wire): int64 scriptId, then
/// a u32, a u32 and a u8 the client stores without naming: time, page count, and whether the buyer may open the
/// manuscript (the upload dialog's 公開する; 0 shows the lock in the PC library, verified live).
/// </summary>
public sealed record AdventureDownloadListRecord(long ScriptId, uint Time, uint Pages, byte Open)
{
    public const int WireSize = 17;

    public void WriteTo(PacketWriter writer)
    {
        writer.Write((ulong)ScriptId);
        writer.Write(Time);
        writer.Write(Pages);
        writer.Write(Open);
    }
}
