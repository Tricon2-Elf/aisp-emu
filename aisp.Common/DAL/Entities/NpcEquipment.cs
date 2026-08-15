namespace aisp.Common.DAL.Entities;

public class NpcEquipment
{
    public int Id { get; set; }
    public int NpcId { get; set; }
    public int SlotIndex { get; set; }
    public int ItemId { get; set; }
    public int SortOrder { get; set; }

    public Npc Npc { get; set; } = default!;
}
