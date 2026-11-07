using System.Security.Claims;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class CalendarFileTests : EventsHost
{
    private const string Agm = "Annual general meeting";

    [Fact]
    public async Task APrivateCalendarIsServedOnlyThroughTheLinkOfSomeoneItIsForWhileTheyStillAre()
    {
        Guid churchMember = Guid.CreateVersion7();
        CalendarSettings members = await CalendarAsync("Church members", Audience.HoldingRole(churchMember));
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        await SaveEventAsync(members, Agm);
        await SaveEventAsync(kids, "Break-up night");

        ClaimsPrincipal member = Person((RoleMembership.NodeOf(churchMember), null));
        string memberLink = await LinkAsync(new LinkHolder(Caller.UserId(member), null));
        string visitorLink = await LinkAsync(new LinkHolder(Caller.UserId(Person()), null));
        string guessed = new('A', memberLink.Length);

        CalendarFile everyone = await FileAsync(CalendarLinks.Public);
        Assert.Contains("Break-up night", everyone.Text);
        Assert.DoesNotContain(Agm, everyone.Text);
        Assert.False((await FileAsync(members.Calendar.Slug)).Found);
        Assert.False((await FileAsync(members.Calendar.Slug, guessed)).Found);
        Assert.False((await FileAsync(members.Calendar.Slug, visitorLink)).Found);
        Assert.DoesNotContain(Agm, (await FileAsync("", visitorLink)).Text);

        Assert.Contains(Agm, (await FileAsync(members.Calendar.Slug, memberLink)).Text);
        Assert.Contains(Agm, (await FileAsync("", memberLink)).Text);

        Access.Refuse(Caller.UserId(member)!.Value, RoleMembership.NodeOf(churchMember), null);
        Assert.False((await FileAsync(members.Calendar.Slug, memberLink)).Found);
        Assert.DoesNotContain(Agm, (await FileAsync("", memberLink)).Text);
    }

    [Fact]
    public async Task AResetLinkIsRefusedAndTheNewOneServesOnlyThePhonesCalendarsForEveryone()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        CalendarSettings team = await CalendarAsync("Kids team", Audience.SignedIn);
        await SaveEventAsync(outreach, "Outreach Saturday");
        await SaveEventAsync(team, "Team training");
        Guid phone = Guid.CreateVersion7();
        await Roles.GiveAsync(phone, EventsPermissions.AdderRole, outreach.Calendar.Id, null, Cancel);
        await Roles.GiveAsync(phone, EventsPermissions.AdderRole, team.Calendar.Id, null, Cancel);

        string before = await LinkAsync(new LinkHolder(null, phone));
        CalendarFile phones = await FileAsync("", before);
        Assert.Contains("Outreach Saturday", phones.Text);
        Assert.DoesNotContain("Team training", phones.Text);
        Assert.False((await FileAsync(team.Calendar.Slug, before)).Found);

        string after = await WithAsync(provider => provider.GetRequiredService<CalendarLinkSecrets>().ResetAsync(new LinkHolder(null, phone), Cancel));

        Assert.NotEqual(before, after);
        Assert.False((await FileAsync("", before)).Found);
        Assert.False((await FileAsync(outreach.Calendar.Slug, before)).Found);
        Assert.Contains("Outreach Saturday", (await FileAsync("", after)).Text);
    }

    private Task<string> LinkAsync(LinkHolder holder) =>
        WithAsync(provider => provider.GetRequiredService<CalendarLinkSecrets>().OfAsync(holder, Cancel));

    private Task<CalendarFile> FileAsync(string calendar, string secret = "") =>
        WithAsync(provider => provider.GetRequiredService<CalendarFiles>().ReadAsync(new CalendarFileRequest { Calendar = calendar, Key = secret }, Cancel));

    private Task SaveEventAsync(CalendarSettings calendar, string title) =>
        EditingAsync(Person((EventsPermissions.EditNode, calendar.Calendar.Id)), (editing, context) => editing.SaveEventAsync(new EventSave
        {
            CalendarId = calendar.Calendar.Id, Fields = new EventFields { Title = title },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(4)), StartMinute = 19 * 60 + 30 }, Showing = Showing.Shown,
        }, context));
}
