namespace aisp.Common.DAL.Entities;

public class CharacterQuestWork
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;
    public int QuestId { get; set; }
    public Quest Quest { get; set; } = default!;
    public ushort Chapter { get; set; } = 1;
    public uint RestSec { get; set; }
    public ushort TargetNow { get; set; }
    public ushort TargetRequired { get; set; }
    public DateTime StartedAtUtc { get; set; }
}
