namespace AISpace.Common.Game;

/// <summary>
/// MyRoom constants derived from client data (localDocs).
/// Map ids: settings/tps_map.csv lines 72-83 (20000000/10/20/30 = 6/8/10/12.5 tatami, field R01_01..04).
/// Entrance (door) positions per expansion stage: settings/myroom.csv columns 8-9.
/// The door/closet are NOT map geometry; the client spawns them from recv_notify_myroom_furniture
/// entries with action type 1 (door -> UI control 141) and 2 (closet/wardrobe -> UI control 142).
/// See decompiled handler sub_48AC50 (aisp-decompiled.c:107151) and click dispatch sub_419BC0 (:22612).
/// </summary>
public static class MyRoomInfo
{
    public const uint BaseMapId = 20_000_000;

    /// <summary>Furniture action types (dword at offset +8 of the furniture wire struct).</summary>
    public const uint ActionDoor = 1;
    public const uint ActionCloset = 2;
    public const uint ActionNicoTv = 3;
    public const uint ActionUseFurniture = 4;

    /// <summary>和風三連衝立 (folding screen) - stand-in model for the room door; asset item/1/10/01010 exists.</summary>
    public const uint DoorItemId = 11_001_010;

    /// <summary>カントリーなタンス (country dresser) - wardrobe model; asset item/1/10/00250 exists.</summary>
    public const uint ClosetItemId = 11_000_250;

    /// <summary>Fixed furniture serial ids used for the built-in room objects.</summary>
    public const uint DoorSerialId = 1;
    public const uint ClosetSerialId = 2;

    public static bool IsMyRoomMap(uint mapId) => mapId is BaseMapId or 20_000_010 or 20_000_020 or 20_000_030;

    /// <summary>Room expansion stage 0-3 (6/8/10/12.5 tatami), byte at offset 58 of the 76-byte myroom info struct.</summary>
    public static byte GetRoomStage(uint mapId) => IsMyRoomMap(mapId) ? (byte)((mapId - BaseMapId) / 10) : (byte)0;

    /// <summary>Door (entrance) x/z per stage, from settings/myroom.csv (入り口_X座標, 入り口_Z座標).</summary>
    public static (float X, float Z) GetEntrancePosition(byte stage) =>
        stage switch
        {
            1 => (123f, -170f),
            2 => (123f, -220f),
            3 => (173f, -220f),
            _ => (73f, -170f),
        };

    /// <summary>Closet position: mirrored across the room center on the same (entrance) wall.</summary>
    public static (float X, float Z) GetClosetPosition(byte stage)
    {
        var (x, z) = GetEntrancePosition(stage);
        return (-x, z);
    }
}
