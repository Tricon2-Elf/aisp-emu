namespace aisp.Common.DAL.Entities;

public class CharacterEventStatus
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;
    public string EventKey { get; set; } = string.Empty;
    public DateTime CompletedAtUtc { get; set; }
}
