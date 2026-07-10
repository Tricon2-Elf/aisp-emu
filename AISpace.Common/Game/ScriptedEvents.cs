namespace AISpace.Common.Game;

public static class ScriptedEvents
{
    public static class Keys
    {
        public const string IntroductionRin01 = "introdution_rin_01";
    }

    private static readonly Dictionary<string, string> ScriptLabels = new(StringComparer.OrdinalIgnoreCase) { [Keys.IntroductionRin01] = "./script/event/introdution_rin_01.csv" };

    public static string GetScriptLabel(string eventKey) => ScriptLabels.TryGetValue(eventKey, out var label) ? label : $"./script/event/{eventKey}.csv";
}
