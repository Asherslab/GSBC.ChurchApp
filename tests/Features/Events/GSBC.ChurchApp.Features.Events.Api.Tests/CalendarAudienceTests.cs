using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class CalendarAudienceTests
{
    private static readonly Guid Helpers = Guid.CreateVersion7();
    private static readonly Guid Leaders = Guid.CreateVersion7();

    [Fact]
    public void AnEventOnSeveralCalendarsReachesEveryoneAnyOfThemIsFor()
    {
        Assert.Equal(Audience.Everyone, CalendarAudiences.Union([Audience.HoldingRole(Helpers), Audience.Everyone]));
        Assert.Equal(Audience.SignedIn, CalendarAudiences.Union([Audience.HoldingRole(Helpers), Audience.SignedIn]));
        Assert.Equal(Audience.HoldingAnyRole([Helpers, Leaders]), CalendarAudiences.Union([Audience.HoldingRole(Helpers), Audience.HoldingRole(Leaders)]));
    }

    [Fact]
    public void AgreeingToShowAnEventElsewhereWarnsOnlyWhenItWouldReachMorePeopleAndSaysWhom()
    {
        Audience kidsTeam = Audience.HoldingRole(Helpers);

        Assert.Equal((true, CalendarAudiences.EveryoneWords), Warning(CalendarAudiences.Widening(kidsTeam, Audience.Everyone)));
        Assert.Equal((true, CalendarAudiences.SignedInWords), Warning(CalendarAudiences.Widening(kidsTeam, Audience.SignedIn)));
        Assert.Equal([Leaders], CalendarAudiences.Widening(kidsTeam, Audience.HoldingRole(Leaders)).NewRoleIds);
        Assert.False(CalendarAudiences.Widening(kidsTeam, Audience.HoldingRole(Helpers)).Widens);
        Assert.False(CalendarAudiences.Widening(Audience.SignedIn, kidsTeam).Widens);
        Assert.False(CalendarAudiences.Widening(Audience.Everyone, Audience.SignedIn).Widens);
    }

    private static (bool, string) Warning(Widening widening) => (widening.Widens, widening.Words);
}
