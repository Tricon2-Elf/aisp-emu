namespace aisp.Network.Data;

/// <summary>
/// One drama disc listing as the client's shop window reads it (parser 0x799BC0; 0x638 bytes in memory, 1589 on
/// the wire). Shared by recv_adventure_shop_started, recv_adventure_shop_item, the search replies, and the
/// ranking / purchase-history rows. Strings are fixed width with a NUL terminator inside the field.
/// Field meanings follow the client's consumers (row builder 0x5C6FF0, detail pane 0x5C3720, buy check 0x5CF0BD).
/// </summary>
public sealed class AdventureShopItemRecord
{
    public const int WireSize = 1589;
    public const int AuthorNameLength = 37;
    public const int TitleLength = 121;
    public const int TagLength = 61;
    public const int TagCount = 10;
    public const int CommentLength = 768;

    public long ScriptId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    /// <summary>Price the buy button checks against the player's purse; shown with the plain money icon when <see cref="PriceAi"/> is 0.</summary>
    public long Price { get; init; }

    /// <summary>Price in デレ (AI points); when non-zero the rows show it instead of <see cref="Price"/>, with the AI-point icon.</summary>
    public long PriceAi { get; init; }

    /// <summary>Tags; the client only reads the one at <see cref="GenreTagIndex"/> and maps its text to a genre 0-9 by comparing it with its localized genre names.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Bit i marks tag i; parsed and never read.</summary>
    public ushort TagFlags { get; init; }

    /// <summary>Index into <see cref="Tags"/> of the genre tag.</summary>
    public byte GenreTagIndex { get; init; }
    public string Comment { get; init; } = string.Empty;

    /// <summary>公式配信: copied into the client's download-list entry on download_request_r and used by the PC library's ribbon tab (verified live). Not the upload request's publish flag.</summary>
    public byte Official { get; init; }

    /// <summary>Never read by the client.</summary>
    public byte Reserved61E { get; init; }

    /// <summary>Upload date as Unix seconds; the rows format it as a date.</summary>
    public uint UploadedAt { get; init; }

    /// <summary>Stored by the client and never read.</summary>
    public uint Reserved624 { get; init; }

    /// <summary>購入数 on the rows and the detail card.</summary>
    public uint Purchases { get; init; }

    /// <summary>ページ: the work's manuscript sheet count; also copied into the client's download-list entry.</summary>
    public uint Pages { get; init; }

    /// <summary>アップロード容量 on the detail card, in bytes.</summary>
    public long ContentBytes { get; init; }

    public void WriteTo(PacketWriter writer)
    {
        writer.Write((ulong)ScriptId);
        writer.WriteFixedStringNulTerminated(AuthorName, AuthorNameLength);
        writer.WriteFixedStringNulTerminated(Title, TitleLength);
        writer.Write((ulong)Price);
        writer.Write((ulong)PriceAi);
        for (var i = 0; i < TagCount; i++)
            writer.WriteFixedStringNulTerminated(i < Tags.Count ? Tags[i] : "", TagLength);
        writer.Write(TagFlags);
        writer.Write(GenreTagIndex);
        writer.WriteFixedStringNulTerminated(Comment, CommentLength);
        writer.Write(Official);
        writer.Write(Reserved61E);
        writer.Write(UploadedAt);
        writer.Write(Reserved624);
        writer.Write(Purchases);
        writer.Write(Pages);
        writer.Write((ulong)ContentBytes);
    }
}
