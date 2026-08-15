namespace aisp.Common.DAL.Entities;

/// <summary>Account-shared 倉庫 item stack (client item-table place=1).</summary>
public class UserStorageItem
{
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public int Quantity { get; set; }
}
