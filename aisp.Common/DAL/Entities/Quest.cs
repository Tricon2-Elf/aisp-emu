namespace aisp.Common.DAL.Entities;

/// <summary>Catalog row for a quest. Display strings are resolved through <c>ITextLocaliser</c>.</summary>
public class Quest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public ushort DefaultChapter { get; set; } = 1;
    public uint DefaultRestSec { get; set; }
    public string DefaultTargetName { get; set; } = string.Empty;
    public ushort DefaultTargetRequired { get; set; } = 1;
    public bool AutoStartOnConnect { get; set; }
}
