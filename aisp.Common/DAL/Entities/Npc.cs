namespace aisp.Common.DAL.Entities;

public enum NpcInteractionType
{
    Shop = 0,
    Decorative = 1,
    AdventureShopBuy = 2,
    AdventureShopUpload = 3,
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

    /// <summary>The purple plate the original service drew behind NPC names (verified live: variant 4 is purple, 5 blue).</summary>
    public const uint DefaultNamePlate = 4;

    /// <summary>Name plate variant behind the NPC's name (CharaData.NamePlate: 0 none, 1-6 and 0xFFFFFFFF the client's role plates, 4 being its ordinary-NPC plate); seeds default to <see cref="DefaultNamePlate"/>.</summary>
    public uint NamePlate { get; set; } = DefaultNamePlate;
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int Rotation { get; set; }
    public int? ShopId { get; set; }
    public NpcInteractionType InteractionType { get; set; } = NpcInteractionType.Shop;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public NpcEventKind EventKind { get; set; }
    public string? EventKey { get; set; }

    public Shop? Shop { get; set; }
    public ICollection<NpcEquipment> Equipment { get; set; } = new List<NpcEquipment>();
}
