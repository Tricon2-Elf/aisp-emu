namespace AISpace.Common.Game;

public enum DayPhase : uint
{
    Morning = 0,
    Day = 1,
    Evening = 2,
    Night = 3,
    EarlyMorning = 4,
}

public readonly record struct ServerTime(DayPhase Phase, uint Current, uint Max);

public static class TimeZoneService
{
    private static readonly long ServerStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static readonly (DayPhase Phase, int Duration)[] Phases =
    [
        (DayPhase.EarlyMorning, 900),
        (DayPhase.Morning, 1800),
        (DayPhase.Day, 3600),
        (DayPhase.Evening, 900),
        (DayPhase.Night, 1800),
    ];

    private static readonly int CycleDuration = Phases.Sum(p => p.Duration);

    public static ServerTime GetServerTime()
    {
        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ServerStartTime;
        var cycleTime = elapsed % CycleDuration;

        foreach (var (phase, duration) in Phases)
        {
            if (cycleTime < duration)
                return new ServerTime(phase, (uint)cycleTime, (uint)duration);
            cycleTime -= duration;
        }

        return default;
    }
}
