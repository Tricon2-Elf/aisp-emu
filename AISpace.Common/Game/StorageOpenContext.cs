namespace AISpace.Common.Game;

/// <summary>
/// Tracks how the account 倉庫 UI was opened so close can emit the matching
/// storage_furn_* responses when appropriate.
/// </summary>
public enum StorageOpenContext : byte
{
    None = 0,
    Wardrobe = 1,
    Furniture = 2,
}
