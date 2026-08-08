using AISpace.Network.Data;

namespace AISpace.Common.DAL.Entities;

public sealed class Robo
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;

    public uint RoboId { get; set; }
    public uint State { get; set; }
    public ushort AiScriptId { get; set; }

    public uint ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BloodType BloodType { get; set; }
    public byte BirthMonth { get; set; }
    public byte BirthDay { get; set; }
    public uint Gender { get; set; }
    public byte Face { get; set; }
    public uint Hairstyle { get; set; }
    public uint ParameterId { get; set; }

    public uint JobId { get; set; }

    public string Like1 { get; set; } = string.Empty;
    public string Like2 { get; set; } = string.Empty;
    public string Like3 { get; set; } = string.Empty;
    public string LikeDesc1 { get; set; } = string.Empty;
    public string LikeDesc2 { get; set; } = string.Empty;
    public string LikeDesc3 { get; set; } = string.Empty;
    public string ProfileDescription { get; set; } = string.Empty;
    public uint ProfileUnknownDword04 { get; set; }
    public uint ProfileUnknownDword08 { get; set; }

    public byte Level { get; set; }
    public ulong StatusPoints { get; set; }
    public ulong Experience { get; set; }
    public ulong ExperienceToNextLevel { get; set; }

    public uint AvailableStatusPoints { get; set; }
    public string UserStatusText { get; set; } = string.Empty;
    public uint UserStatusIconId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public RoboTpsBattleData TpsBattleData { get; set; } = default!;
    public ICollection<RoboEquipment> Equipment { get; set; } = [];
    public ICollection<RoboItemUseEffect> ItemUseEffects { get; set; } = [];
    public ICollection<RoboDistributedStatusPoint> DistributedStatusPoints { get; set; } = [];
}
