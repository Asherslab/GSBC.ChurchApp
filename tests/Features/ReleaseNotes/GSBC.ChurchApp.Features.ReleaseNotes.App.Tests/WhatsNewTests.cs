using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.ReleaseNotes.App.Tests;

public sealed class WhatsNewTests
{
    private static readonly ReleaseNote Themes = Note("themes", highlight: true);
    private static readonly ReleaseNote Passkeys = Note("passkeys", highlight: true);
    private static readonly ReleaseNote SmallFix = Note("small-fix", highlight: false);

    [Fact]
    public async Task HighlightsShowOnceAfterAnUpdateAndNeverOnAFreshInstall()
    {
        Phone phone = new();

        Assert.Empty(await new WhatsNew(phone, [Themes]).TakeUnseenHighlightsAsync());
        Assert.Empty(await new WhatsNew(phone, [Themes]).TakeUnseenHighlightsAsync());

        WhatsNew updated = new(phone, [Passkeys, SmallFix, Themes]);
        Assert.Equal([Passkeys], await updated.TakeUnseenHighlightsAsync());
        Assert.Empty(await updated.TakeUnseenHighlightsAsync());
    }

    private static ReleaseNote Note(string id, bool highlight) =>
        new(id, null, id, "A line for everyone.", highlight, false, null, null, null, null, null, null, null);

    private sealed class Phone : ILocalPreferences
    {
        private readonly Dictionary<string, string> _stored = [];

        public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(_stored.GetValueOrDefault(key));

        public ValueTask SetAsync(string key, string? value)
        {
            if (value is null)
                _stored.Remove(key);
            else
                _stored[key] = value;
            return ValueTask.CompletedTask;
        }
    }
}
