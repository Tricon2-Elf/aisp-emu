namespace AISpace.Common.DAL.Entities;

public class Circle
{
    public int Id;
    public required string Name;

    public int LeaderCharacterId { get; internal set; }
    public DateTime CreatedAt { get; internal set; }
}
