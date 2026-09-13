namespace aisp.Common.DAL.Entities;

public class DramaFigureBox
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class DramaFigureDefinition
{
    public int Id { get; set; }
    public int BoxId { get; set; }
    public DramaFigureBox Box { get; set; } = null!;
    public int? ItemId { get; set; }
    public Item? Item { get; set; }

    /// <summary>Name for figures without an inventory item; item-backed figures use Item.Name.</summary>
    public string Name { get; set; } = "";
    public int Gender { get; set; }
    public int People { get; set; } = 1;
    public int PackageId { get; set; }
    public int Face { get; set; }
    public int Hairstyle { get; set; }
    public int ModelId { get; set; }
    public bool AlwaysGranted { get; set; }
    public int SortOrder { get; set; }
    public ICollection<DramaFigureEquipment> Equipment { get; set; } =
        new List<DramaFigureEquipment>();
}

public class DramaFigureEquipment
{
    public int FigureId { get; set; }
    public DramaFigureDefinition Figure { get; set; } = null!;
    public int SlotIndex { get; set; }
    public int ItemId { get; set; }
}

public class DramaAudioDefinition
{
    public int Kind { get; set; }
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int SortOrder { get; set; }
}
