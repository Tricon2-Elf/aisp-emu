namespace AISpace.Common.DAL.Entities;

public enum NpcInteractionType
{
    Shop = 0,
    Decorative = 1,
}

public class Npc
{
    public int Id { get; set; }
    public long MapId { get; set; }
    public int ChannelId { get; set; } = -1;
    public int DayPhase { get; set; } = -1;
    public DateTime DateStartUtc { get; set; } = DateTime.UnixEpoch;
    public DateTime DateEndUtc { get; set; } = DateTime.MaxValue;
    public long NpcObjectId { get; set; }
    public long ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int Rotation { get; set; }
    public int? ShopId { get; set; }
    public NpcInteractionType InteractionType { get; set; } = NpcInteractionType.Shop;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public string? ScriptedEventKey { get; set; }

    public Shop? Shop { get; set; }
    public ICollection<NpcEquipment> Equipment { get; set; } = new List<NpcEquipment>();
}
