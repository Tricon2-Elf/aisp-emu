namespace aisp.Common.DAL.Entities;

/// <summary>
/// One copy of a drama disc bought in the shop. The price is debited from the buyer at once; the author's share
/// waits in <see cref="AuthorShare"/> until the weekly settlement moves it into User.AdventureSalesBalance, which the
/// 売上担当 clerk pays out. Buyers can hide entries from their 購入履歴 without losing the copy.
/// </summary>
public sealed class AdventurePurchase
{
    public int Id { get; set; }
    public long ScriptId { get; set; }
    public AdventureListing Listing { get; set; } = default!;
    public int BuyerUserId { get; set; }
    public User BuyerUser { get; set; } = default!;
    public int BuyerCharacterId { get; set; }

    /// <summary>Price paid by the buyer, in デレ (the in-game currency, User.AiPoints).</summary>
    public long Price { get; set; }

    /// <summary>The author's cut of <see cref="Price"/>, fixed at purchase time from the upload rate.</summary>
    public long AuthorShare { get; set; }
    public DateTime PurchasedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public bool HiddenFromHistory { get; set; }

    /// <summary>Removed from the download list with send_adventure_download_delete_request; the copy can still be re-downloaded from the history.</summary>
    public bool HiddenFromDownloads { get; set; }
}
