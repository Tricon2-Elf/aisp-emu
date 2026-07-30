namespace AISpace.Common.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public long AiPoints { get; set; }
    public long NicoPoints { get; set; }

    /// <summary>AI points held in the wardrobe 倉庫 (piggy bank), separate from purse <see cref="AiPoints"/>.</summary>
    public long StorageDeposit { get; set; }
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? BannedAt { get; set; }

    public ICollection<Character> Characters { get; set; } = new List<Character>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<UserStorageItem> StorageItems { get; set; } = new List<UserStorageItem>();

    public void SetPassword(string password)
    {
        PasswordHash = PasswordHasher.Hash(password);
    }

    public bool VerifyPassword(string password)
    {
        return PasswordHasher.Verify(password, PasswordHash);
    }
}
