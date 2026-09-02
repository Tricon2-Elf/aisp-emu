using aisp.Common.Localisation;

namespace aisp.Common.DAL.Entities;

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
    public DateTime? LastLoggedInAt { get; set; }
    public DateTime? BannedAt { get; set; }
    public GameLanguage Language { get; set; } = GameLanguage.Japanese;

    /// <summary>原稿用紙 (manuscript sheets) the account holds for the drama editor; shared across its characters.</summary>
    public int AdventureSheetStock { get; set; }

    /// <summary>Next drama work id handed out by recv_adventure_work_create_r. Monotonic; ids are never reused.</summary>
    public int NextAdventureWorkId { get; set; } = 1;

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
