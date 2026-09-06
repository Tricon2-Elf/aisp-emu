namespace aisp.Common.DAL.Entities;

public class CharacterQuestHistory
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;
    public int QuestId { get; set; }
    public Quest Quest { get; set; } = default!;
    public ushort Chapter { get; set; }
    public byte Result { get; set; } = 1;
    public DateTime CompletedAtUtc { get; set; }
}
