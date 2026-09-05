namespace aisp.Common.DAL.Entities;

/// <summary>
/// The two multipart parts the client POSTs to upload.php for a listing: the command script (form field uccadv)
/// and the actor table (form field datalist), both plain UTF-8 text. Stored as uploaded; download.php packs them
/// into the client's ADV0 cache format on the way out. The server never interprets them.
/// </summary>
public sealed class AdventureListingContent
{
    public long ScriptId { get; set; }
    public AdventureListing Listing { get; set; } = default!;
    public byte[] Script { get; set; } = [];
    public byte[] Datalist { get; set; } = [];
}
