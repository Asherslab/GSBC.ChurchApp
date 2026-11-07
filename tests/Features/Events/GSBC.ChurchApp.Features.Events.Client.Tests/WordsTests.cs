using GSBC.ChurchApp.Features.Events.Client;
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
}
