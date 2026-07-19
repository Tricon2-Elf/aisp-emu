namespace AISpace.Common.Game;

public static class ScriptedEvents
{
    public static class Keys
    {
        public const string IntroductionRin01 = "introdution_rin_01";
        public const string IntroductionRin02 = "introdution_rin_02";
        public const string IntroductionHotaru0 = "introdution_hotaru_0";
        public const string IntroductionShinju01 = "introdution_shinju_01";
        public const string IntroductionMyRoomDaCapo = "introdution_myroom_dc";
        public const string IntroductionMyRoomClannad = "introdution_myroom_cl";
        public const string IntroductionMyRoomShuffle = "introdution_myroom_sh";
        public const string SysEvent002 = "sys_event_002";
        public const string TpsEventBat0101011 = "tps_event_bat_01_01_01_1";
        public const string Bat0101012 = "bat_01_01_01_2";
        public const string Bat0101021 = "bat_01_01_02_1";
    }

    private static readonly Dictionary<string, string> ScriptLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [Keys.IntroductionRin01] = "./script/event/introdution_rin_01.csv",
        [Keys.IntroductionRin02] = "./script/event/introdution_rin_02.csv",
        [Keys.IntroductionHotaru0] = "./script/event/introdution_hotaru_0.csv",
        [Keys.IntroductionShinju01] = "./script/event/introdution_shinju_01.csv",
        [Keys.IntroductionMyRoomDaCapo] = "./script/event/introdution_myroom_dc.csv",
        [Keys.IntroductionMyRoomClannad] = "./script/event/introdution_myroom_cl.csv",
        [Keys.IntroductionMyRoomShuffle] = "./script/event/introdution_myroom_sh.csv",
        [Keys.SysEvent002] = "./script/sys_event/002.csv",
        [Keys.TpsEventBat0101011] = "./script/tps_event/bat_01_01_01_1.csv",
        [Keys.Bat0101012] = "./script/tps_event/bat_01_01_01_2.csv",
        [Keys.Bat0101021] = "./script/tps_event/bat_01_01_02_1.csv",
    };

    public static string GetScriptLabel(string eventKey) => ScriptLabels.TryGetValue(eventKey, out var label) ? label : $"./script/event/{eventKey}.csv";
}
