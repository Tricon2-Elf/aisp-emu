using aisp.Common.Services.Toxicity;

namespace aisp.Common.Tests;

public sealed class ToxicityIdleUnloadTests
{
    [Fact]
    public void ShouldUnloadIdle_WhenPastIdleAndNotInFlight()
    {
        var last = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var now = last.AddMinutes(15);
        Assert.True(
            ToxicityClassifier.ShouldUnloadIdle(last, inFlight: 0, TimeSpan.FromMinutes(15), now)
        );
    }

    [Fact]
    public void ShouldUnloadIdle_FalseWhenInFlight()
    {
        var last = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var now = last.AddHours(1);
        Assert.False(
            ToxicityClassifier.ShouldUnloadIdle(last, inFlight: 1, TimeSpan.FromMinutes(15), now)
        );
    }

    [Fact]
    public void ShouldUnloadIdle_FalseWhenIdleDisabled()
    {
        var last = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var now = last.AddHours(1);
        Assert.False(ToxicityClassifier.ShouldUnloadIdle(last, inFlight: 0, TimeSpan.Zero, now));
    }

    [Fact]
    public void ShouldUnloadIdle_FalseWhenStillWithinWindow()
    {
        var last = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var now = last.AddMinutes(14);
        Assert.False(
            ToxicityClassifier.ShouldUnloadIdle(last, inFlight: 0, TimeSpan.FromMinutes(15), now)
        );
    }
}
