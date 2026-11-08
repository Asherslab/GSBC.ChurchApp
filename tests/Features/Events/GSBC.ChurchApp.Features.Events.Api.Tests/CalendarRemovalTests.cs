using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class CalendarRemovalTests
{
    [Fact]
    public void TheConfirmationCountsWhatGoesAndWhatOnlyStopsBeingShownThere()
    {
        CalendarRecord kids = Calendar("Kids", Audience.Everyone, shownOnNewPhones: true);
        CalendarRecord wholeChurch = Calendar("Whole church", Audience.Everyone);
        EventRecord camp = OneOff(kids), breakUp = OneOff(kids), picnic = OneOff(wholeChurch);
        RegularEventRecord impactKids = new() { Id = Guid.CreateVersion7(), CalendarId = kids.Id };
        ShowingRecord campOnWholeChurch = Showing(wholeChurch, camp);
        ShowingRecord picnicOnKids = Showing(kids, picnic);
        ShowingRecord breakUpAskedOnly = Showing(wholeChurch, breakUp, agreed: false);

        CalendarRemoval removal = CalendarRemover.WhatGoes(
            new EventsBook([kids, wholeChurch], [camp, breakUp, picnic], [impactKids], [], [], [], [campOnWholeChurch, picnicOnKids, breakUpAskedOnly]), kids.Id);

        Assert.Equal(("Kids", 2, 1), (removal.Name, removal.Events, removal.RegularEvents));
        Assert.Equal((1, 1), (removal.ShownElsewhere, removal.FromElsewhere));
        Assert.True(removal.ShownOnNewPhones);
        Assert.True(removal.ForEveryone);
    }

    private static CalendarRecord Calendar(string name, Audience audience, bool shownOnNewPhones = false) =>
        new() { Id = Guid.CreateVersion7(), Name = name, Slug = CalendarLinks.SlugOf(name), Audience = audience.Text, ShownOnNewPhones = shownOnNewPhones };

    private static EventRecord OneOff(CalendarRecord calendar) => new() { Id = Guid.CreateVersion7(), CalendarId = calendar.Id };

    private static ShowingRecord Showing(CalendarRecord on, EventRecord item, bool agreed = true) =>
        new() { Id = Guid.CreateVersion7(), CalendarId = on.Id, EventId = item.Id, AgreedAt = agreed ? new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc) : null };
}
