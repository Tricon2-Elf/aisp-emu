namespace aisp.Common.DAL.Entities;

public enum AdventureTicketPurpose
{
    /// <summary>Issued by recv_adventure_upload_request_r; redeemed by one POST to upload.php.</summary>
    Upload = 0,

    /// <summary>Issued by recv_adventure_shop_download_request_r (or the buy reply); redeemed by one POST to download.php.</summary>
    Download = 1,
}

/// <summary>
/// One-time token that lets the client's plain HTTP upload / download call prove which player and listing it
/// belongs to. The client copies it into the multipart ticket field (40 bytes max).
/// </summary>
public sealed class AdventureTicket
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public long ScriptId { get; set; }
    public AdventureTicketPurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
