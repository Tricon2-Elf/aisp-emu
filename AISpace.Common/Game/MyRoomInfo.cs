namespace AISpace.Common.Game;

/// <summary>
/// MyRoom constants derived from client data (localDocs).
/// Map ids: settings/tps_map.csv lines 72-83 (20000000/10/20/30 = 6/8/10/12.5 tatami, field R01_01..04).
/// Entrance (door) positions per expansion stage: settings/myroom.csv columns 8-9.
/// Door/closet visuals+collision come from recv_notify_myroom_furniture (not map geometry).
/// Click UI for placed furniture uses furniture.csv アクション on ItemId (sub_419BC0), NOT the wire ActionType:
///   1 → CHL 141 drama playback (PC 11000220)
///   2 → CHL 142 adventure/drama work list (notebook 11000160) → send_get_adventure_work_list (13000)
///   3 → Nico TV
///   4 → send_myroom_use_furniture (0x2231); server may push recv_storage_opened (0x2CA5) for 倉庫
/// Builtin closet uses an action-4 catalog ItemId so the client asks the server; 倉庫 is not csv action 2.
/// </summary>
public static class MyRoomInfo
{
    public const uint BaseMapId = 20_000_000;

    /// <summary>
    /// Wire ActionType (dword at +8). Spawn (sub_48AC50) does not use this for click routing;
    /// kept for packet layout / possible builtin semantics. Do not confuse with furniture.csv アクション.
    /// </summary>
    public const uint ActionDoor = 1;
    public const uint ActionCloset = 2;
    public const uint ActionNicoTv = 3;
    public const uint ActionUseFurniture = 4;

    /// <summary>和風三連衝立 (folding screen) - stand-in door model; csv アクション empty (not drama).</summary>
    public const uint DoorItemId = 11_001_010;

    /// <summary>
    /// Christmas tree — only live furniture.csv rows with アクション=4 (send_myroom_use_furniture).
    /// Real closet models 11000240–242 are commented out with empty アクション; dresser 11000250 is also
    /// action-empty (collision/visual only). Action 4 is required for the client to ask the server,
    /// which then pushes recv_storage_opened for 倉庫. Visual is wrong until a better action-4 asset exists.
    /// </summary>
    public const uint ClosetItemId = 11_001_170;

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

    /// <summary>Maximum placed furniture count per stage, from settings/myroom.csv column 1.</summary>
    public static uint GetMaxFurniturePlacement(byte stage) =>
        stage switch
        {
            2 or 3 => 870,
            _ => 700,
        };
}
