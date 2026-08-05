namespace AISpace.Common.Game;

/// <summary>
/// Tracks whether the account 倉庫 UI is open so close can emit matching
/// storage_furn_* responses. Only the My Room wardrobe NPC opens storage today.
/// </summary>
public enum StorageOpenContext : byte
{
    None = 0,
    Wardrobe = 1,
}
