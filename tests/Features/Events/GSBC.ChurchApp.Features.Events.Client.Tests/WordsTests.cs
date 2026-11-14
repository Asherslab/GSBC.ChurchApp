using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class WordsTests
{
    [Fact]
    public void ADateThatIsNotOnSaysWhyOnItsOwnLineNeverAsASuffix()
    {
        EventOccurrence kids = new()
        {
            Fields = new EventFields { Title = "Impact Kids" }, NotOn = true, NotOnReason = "Camp weekend", BackOn = "2026-11-20",
        };

        Assert.Equal("Camp weekend · back on 20 November", EventWords.NotOnLine(kids));
        Assert.Equal("Impact Kids", kids.Fields.Title);
    }

    [Fact]
    public void TheHeadNeverSaysTheLabelAgainAboveATitleThatAlreadyHasIt()
    {
        Assert.Equal("Services", EventWords.LabelOf("Anniversary", "Anniversary Sunday Service", "Services"));
        Assert.Equal("Baptism", EventWords.LabelOf("Baptism", "Morning service", "Services"));
        Assert.Equal("Services", EventWords.LabelOf("", "Morning service", "Services"));
    }

    [Fact]
    public async Task WideningIsOneFullSentenceAndSilentWhenNothingWidens()
    {
        WideningWords words = new(new NoServices());

        Assert.Equal("This will show to everyone with the app, not only Kids team.",
            await words.SentenceAsync(true, CalendarAudiences.EveryoneWords, [], "Kids team"));
        Assert.Null(await words.SentenceAsync(false, "", [], "Kids team"));
    }

    private sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public void RemovingACalendarSaysWhatGoesWhatStaysAndThatItCantBeUndone()
    {
        CalendarRemoval kids = new() { Name = "Kids", Events = 14, RegularEvents = 1, ShownElsewhere = 2, FromElsewhere = 1, AddedBy = 27, ForEveryone = true };
        CalendarRemoval empty = new() { Name = "Spare", ShownOnNewPhones = true };

        Assert.Equal(
            [
                "14 events and one regular event (with every date) in it go too.",
                "2 of them are also shown on other calendars, and come off those too.",
                "One event from another calendar is shown here. It stays on its own calendar.",
                "27 people and phones have added it. It comes off every phone that has it the next time it connects. Calendar apps subscribed to its link stop getting it.",
                "This can't be undone.",
            ],
            RemovalWords.Lines(kids));
        Assert.Equal("Remove Kids and its events", RemovalWords.Confirm(kids));
        Assert.Equal(
            ["It has no events.", "Nobody has added it, and it's shown on every new phone. It comes off every phone that has it the next time it connects.", "This can't be undone."],
            RemovalWords.Lines(empty));
        Assert.Equal("Remove Spare", RemovalWords.Confirm(empty));
    }
}
