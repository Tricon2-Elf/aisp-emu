namespace aisp.Common.Config;

/// <summary>
/// When drama disc sales become collectable. The author's share of each sale is デレ, the in-game currency, and
/// the original service settled once a week (Saturday 05:00 Japan time): purchases made before a cutoff are moved
/// into the author's collectable デレ balance at that cutoff, and the shop's 売上 clerk pays it out on the next visit.
/// </summary>
public class AdventureSettlementOptions
{
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Saturday;

    /// <summary>Local time of day in <see cref="TimeZone"/>, "HH:mm".</summary>
    public string Time { get; set; } = "05:00";

    /// <summary>IANA or Windows time zone id.</summary>
    public string TimeZone { get; set; } = "Asia/Tokyo";

    /// <summary>How often the settlement service checks for a passed cutoff.</summary>
    public int CheckIntervalMinutes { get; set; } = 5;

    public TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    public TimeOnly ResolveTime() =>
        TimeOnly.TryParseExact(Time, "HH:mm", out var time) ? time : new TimeOnly(5, 0);

    /// <summary>The most recent cutoff at or before <paramref name="nowUtc"/>.</summary>
    public DateTime GetLastCutoffUtc(DateTime nowUtc)
    {
        var zone = ResolveTimeZone();
        var time = ResolveTime();
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            zone
        );
        var candidate = local.Date.Add(time.ToTimeSpan());
        while (candidate.DayOfWeek != DayOfWeek || candidate > local)
            candidate = candidate.AddDays(-1);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(candidate, DateTimeKind.Unspecified),
            zone
        );
    }

    /// <summary>The first cutoff strictly after <paramref name="nowUtc"/>.</summary>
    public DateTime GetNextCutoffUtc(DateTime nowUtc) => GetLastCutoffUtc(nowUtc).AddDays(7);
}
