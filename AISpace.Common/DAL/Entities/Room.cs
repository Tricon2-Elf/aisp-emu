using AISpace.Network;

namespace AISpace.Common.DAL.Entities;

public sealed class Room
{
    public int Id { get; set; }
    public int OwnerCharacterId { get; set; }
    public Character OwnerCharacter { get; set; } = default!;
    public string Name { get; set; } = "My Room";
    public MyRoomStage Stage { get; set; } = MyRoomStage.SixTatami;
    public uint Security { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<MyRoomFurniture> Furniture { get; set; } = new List<MyRoomFurniture>();
}
