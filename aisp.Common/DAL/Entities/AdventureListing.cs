namespace aisp.Common.DAL.Entities;

public enum AdventureListingState
{
    /// <summary>send_adventure_upload_request accepted; waiting for the manuscript on upload.php and the client's report.</summary>
    Pending = 0,

    /// <summary>On sale: visible in the disc shop and in the author's upload list.</summary>
    Listed = 1,

    /// <summary>Taken down by the author (or replaced by a newer upload of the same work). Buyers keep their copies.</summary>
    Delisted = 2,

    /// <summary>
    /// A pending upload the client reported as failed, or that a retry superseded. Kept so the script id is never
    /// handed out again: the client remembers a failed id and rejects a later upload that gets the same one.
    /// </summary>
    Abandoned = 3,
}

/// <summary>
/// A drama disc listing in the はっぴぃ・すとぉりぃ shop. ScriptId is the id the client uses everywhere (the
/// wire scriptId, the dl/drama/ai{ScriptId}.txt cache name, the upload.php scriptid field) and is never reused:
/// re-uploading a work makes a new listing. The packed manuscript lives in <see cref="AdventureListingContent"/>.
/// </summary>
public sealed class AdventureListing
{
    /// <summary>First id handed out; the legacy service's discs sit below this.</summary>
    public const long FirstScriptId = 10001;

    public long ScriptId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int CharacterId { get; set; }

    /// <summary>The author's per-account work id (AdventureWork.WorkId) this listing was uploaded from.</summary>
    public int WorkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Genre { get; set; }
    public string Comment { get; set; } = string.Empty;

    /// <summary>Sale price in デレ (AI points). The original shop only ever sold for デレ.</summary>
    public long Price { get; set; }

    /// <summary>The upload dialog's 「ダウンロード時に内容を公開する」: buyers may open the manuscript in the editor (unlocked in their PC library).</summary>
    public bool ContentsPublic { get; set; }

    /// <summary>公式配信: operator content, shown under the PC library's ribbon tab. Never set by a player upload.</summary>
    public bool Official { get; set; }

    /// <summary>Byte size of the packed script plus the actor table, as announced by the client (shown as アップロード容量).</summary>
    public long ContentSize { get; set; }

    /// <summary>原稿用紙 (manuscript sheets) of the work at upload time; the shop shows it as ページ.</summary>
    public int Pages { get; set; }
    public AdventureListingState State { get; set; }
    public int SalesCount { get; set; }
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ListedAt { get; set; }
    public DateTime? DelistedAt { get; set; }

    public AdventureListingContent? Content { get; set; }
}
