using AISpace.Network;

namespace AISpace.Common.Game;

public static class MyRoomInfo
{
    public const uint SixTatamiMapId = 20_000_000;
    public const uint EightTatamiMapId = 20_000_010;
    public const uint TenTatamiMapId = 20_000_020;
    public const uint TwelveTatamiMapId = 20_000_030;

    /// <summary>Alias for the base (6-tatami) MyRoom map.</summary>
    public const uint BaseMapId = SixTatamiMapId;

    /// <summary>Wire ActionType (dword +8). Spawn does not use this for click routing.</summary>
    public const uint ActionDoor = 1;
    public const uint ActionCloset = 2;
    public const uint ActionNicoTv = 3;
    public const uint ActionUseFurniture = 4;

    public const uint ClosetItemId = 11_000_250;

    public const uint DoorSerialId = 1;
    public const uint ClosetSerialId = 2;

    public static bool IsMyRoomMap(uint mapId) => mapId is SixTatamiMapId or EightTatamiMapId or TenTatamiMapId or TwelveTatamiMapId;

    public static MyRoomStage GetRoomStage(uint mapId) =>
        mapId switch
        {
            EightTatamiMapId => MyRoomStage.EightTatami,
            TenTatamiMapId => MyRoomStage.TenTatami,
            TwelveTatamiMapId => MyRoomStage.TwelveTatami,
            _ => MyRoomStage.SixTatami,
        };

    public static uint GetMapId(MyRoomStage stage) =>
        stage switch
        {
            MyRoomStage.EightTatami => EightTatamiMapId,
            MyRoomStage.TenTatami => TenTatamiMapId,
            MyRoomStage.TwelveTatami => TwelveTatamiMapId,
            _ => SixTatamiMapId,
        };

    public static (float X, float Z) GetEntrancePosition(MyRoomStage stage) =>
        stage switch
        {
            MyRoomStage.EightTatami => (123f, -170f),
            MyRoomStage.TenTatami => (123f, -220f),
            MyRoomStage.TwelveTatami => (173f, -220f),
            _ => (73f, -170f),
        };

    public static (float X, float Z) GetClosetPosition(MyRoomStage stage)
    {
        var (x, z) = GetEntrancePosition(stage);
        return (-x, z);
    }

    public static uint GetMaxFurniturePlacement(MyRoomStage stage) =>
        stage switch
        {
            MyRoomStage.TenTatami or MyRoomStage.TwelveTatami => 870,
            _ => 700,
        };
}
