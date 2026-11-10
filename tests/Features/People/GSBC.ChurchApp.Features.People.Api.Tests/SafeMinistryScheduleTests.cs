using GSBC.ChurchApp.Features.People.Api.SafeMinistry;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class SafeMinistryScheduleTests
{
    [Theory]
    [InlineData("2026-11-10T15:59:00Z", "2026-11-10T16:00:00Z")]
    [InlineData("2026-11-10T16:00:00Z", "2026-11-11T16:00:00Z")]
    [InlineData("2026-11-10T03:00:00Z", "2026-11-10T16:00:00Z")]
    public void ItSyncsAt2amInBrisbaneEachNight(string now, string next) =>
        Assert.Equal(DateTime.Parse(next).ToUniversalTime(), SafeMinistrySchedule.NextRun(DateTime.Parse(now).ToUniversalTime()));

    [Fact]
    public void ANightMissedWhileTheServerWasDownIsCaughtUpOnStart()
    {
        DateTime now = new(2026, 11, 10, 3, 0, 0, DateTimeKind.Utc);

        Assert.True(SafeMinistrySchedule.Missed(null, now));
        Assert.True(SafeMinistrySchedule.Missed(now.AddHours(-26), now));
        Assert.False(SafeMinistrySchedule.Missed(now.AddHours(-11), now));
    }
}
