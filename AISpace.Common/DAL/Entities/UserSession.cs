namespace AISpace.Common.DAL.Entities;

public class UserSession
{
    public int Id { get; set; }           // Primary key
    public string OTP { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = default!;
}
