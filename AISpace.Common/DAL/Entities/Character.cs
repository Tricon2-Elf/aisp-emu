using AISpace.Network.Data;

namespace AISpace.Common.DAL.Entities;

public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public uint ModelId { get; set; }
    public BloodType BloodType { get; set; }

    public DateTime Birthdate { get; set; }
    public int Gender { get; set; }
    public uint FaceType { get; set; }
    public uint Hairstyle { get; set; }

    public string Like1 { get; set; } = "Nothing";
    public string Like2 { get; set; } = "Nothing";
    public string Like3 { get; set; } = "Nothing";
    public string LikeDesc1 { get; set; } = string.Empty;
    public string LikeDesc2 { get; set; } = string.Empty;
    public string LikeDesc3 { get; set; } = string.Empty;
    public string AvatarDesc { get; set; } = "Welcome to AI-Space!";

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public int? CircleId { get; set; }
    public Circle? Circle { get; set; }
    public uint CurrentMapId { get; set; }
    public uint HomeIslandId { get; set; }

    public ICollection<CharacterInventory> Inventory { get; set; } = new List<CharacterInventory>();
    public ICollection<CharacterEquipment> Equipment { get; set; } = new List<CharacterEquipment>();
}
