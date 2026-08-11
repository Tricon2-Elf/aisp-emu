using AISpace.Network.Data;

namespace AISpace.Common.DAL.Entities;

public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public uint ModelId { get; set; }
    public BloodType BloodType { get; set; }

    public DateTime Birthdate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoggedInAt { get; set; }
    public int Gender { get; set; }
    public uint FaceType { get; set; }
    public uint Hairstyle { get; set; }

    public string Like1 { get; set; } = string.Empty;
    public string Like2 { get; set; } = string.Empty;
    public string Like3 { get; set; } = string.Empty;
    public string LikeDesc1 { get; set; } = string.Empty;
    public string LikeDesc2 { get; set; } = string.Empty;
    public string LikeDesc3 { get; set; } = string.Empty;
    public string AvatarDesc { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public int? CircleId { get; set; }
    public Circle? Circle { get; set; }
    public ICollection<CircleMember> CircleMemberships { get; set; } = new List<CircleMember>();
    public uint CurrentMapId { get; set; }
    public int? CurrentRoomId { get; set; }
    public Room? CurrentRoom { get; set; }
    public uint HomeIslandId { get; set; }
    public CharadollPersonality CharadollPersonality { get; set; } = CharadollPersonality.None;

    public ICollection<CharacterInventory> Inventory { get; set; } = new List<CharacterInventory>();
    public ICollection<CharacterEquipment> Equipment { get; set; } = new List<CharacterEquipment>();
    public ICollection<Robo> Robos { get; set; } = new List<Robo>();
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
