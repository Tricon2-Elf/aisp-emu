namespace AISpace.Common.DAL.Entities;

public class User
{
    public int Id { get; set; }           // Primary key
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public long NpsPoints { get; set; }

    public ICollection<Character> Characters { get; set; } = new List<Character>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    // Method for setting password safely
    public void SetPassword(string password)
    {
        PasswordHash = PasswordHasher.Hash(password);
    }

    // Method for checking password
    public bool VerifyPassword(string password)
    {
        return PasswordHasher.Verify(password, PasswordHash);
    }
}
