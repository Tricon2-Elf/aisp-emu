namespace AISpace.Common.Game;

public static class TimeZoneService
{
    private static readonly long _serverStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static (uint phase, float current, float max) GetServerTime()
    {
        const uint T_EARLY = 900;
        const uint T_MORN  = 1800;
        const uint T_DAY   = 3600;
        const uint T_EVE   = 900;
        const uint T_NIGHT = 1800;
        const uint TOTAL   = 9000;

        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _serverStartTime;
        uint cycleTime = (uint)(elapsed % TOTAL);

        if (cycleTime < T_EARLY) 
            return (4, (float)cycleTime, (float)T_EARLY);
        
        cycleTime -= T_EARLY;
        if (cycleTime < T_MORN) 
            return (0, (float)cycleTime, (float)T_MORN);

        cycleTime -= T_MORN;
        if (cycleTime < T_DAY) 
            return (1, (float)cycleTime, (float)T_DAY);

        cycleTime -= T_DAY;
        if (cycleTime < T_EVE) 
            return (2, (float)cycleTime, (float)T_EVE);

        cycleTime -= T_EVE;
        return (3, (float)cycleTime, (float)T_NIGHT);
    }
}