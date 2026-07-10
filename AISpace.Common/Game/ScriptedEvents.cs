namespace AISpace.Common.Game;

public static class ScriptedEvents
{
    public static class Keys
    {
        public const string IntroductionRin01 = "introdution_rin_01";
        public const string IntroductionRin02 = "introdution_rin_02";
        public const string IntroductionHotaru0 = "introdution_hotaru_0";
        public const string IntroductionShinju01 = "introdution_shinju_01";
    }

    private static readonly Dictionary<string, string> ScriptLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [Keys.IntroductionRin01] = "./script/event/introdution_rin_01.csv",
        [Keys.IntroductionRin02] = "./script/event/introdution_rin_02.csv",
        [Keys.IntroductionHotaru0] = "./script/event/introdution_hotaru_0.csv",
        [Keys.IntroductionShinju01] = "./script/event/introdution_shinju_01.csv",
    };

    public static string GetScriptLabel(string eventKey) => ScriptLabels.TryGetValue(eventKey, out var label) ? label : $"./script/event/{eventKey}.csv";
}
