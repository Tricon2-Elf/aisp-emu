namespace aisp.Common.DAL.Entities;

/// <summary>Map definition with spawn position. MapId is the game's numeric map id (e.g. 10990100).</summary>
public class Map
{
    /// <summary>Game map id (e.g. 10990100). Used as primary key.</summary>
    public long MapId { get; set; }

    /// <summary>Island grouping from map id prefix (e.g. Da Capo, Clannad). Empty when not on a themed island.</summary>
    public string Island { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public float SpawnZ { get; set; }

    /// <summary>Spawn rotation (yaw). Stored as int for DB; use as sbyte in game.</summary>
    public int SpawnRotation { get; set; }
}
