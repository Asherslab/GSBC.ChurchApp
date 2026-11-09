using GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Tests;

public sealed class WebsitePageTests
{
    [Fact]
    public void AListingPageGivesItsSermonsTheLastPageAndHowManyTheWebsiteSaysItHas()
    {
        ListingPage first = ListingPages.Read(SampleFiles.Text("listing-1.html"));
        ListingPage last = ListingPages.Read(SampleFiles.Text("listing-2.html"));

        Assert.Equal(["/media/2026-11-01-safe-harbour-part-4-harbour-of-hope", "/media/2026-10-29-the-good-soil"], first.Sermons.Select(sermon => sermon.Path));
        Assert.Equal(("Safe Harbour: Part 4 - Harbour Of Hope", 2, 3), (first.Sermons[0].Title, first.LastPage, first.WebsiteCount));
        Assert.Equal((1, 2), (last.Sermons.Count, last.LastPage));
    }

    [Fact]
    public void ASermonsPageGivesEverythingTheImportBringsAcross()
    {
        WebsiteSermon sermon = SermonPages.Read("/media/2026-11-01-safe-harbour-part-4-harbour-of-hope", SampleFiles.Text("sermon-harbour.html"))!;

        Assert.Equal((900, "Safe Harbour: Part 4 - Harbour Of Hope", new DateOnly(2026, 11, 1)), (sermon.WebsiteId, sermon.Title, sermon.PreachedOn));
        Assert.Equal(("Jo Ellery", "https://files.example/people/ellery.jpg"), (sermon.Speaker, sermon.SpeakerPhotoUrl));
        Assert.Equal(new WebsiteSeriesLink("safe-harbour", "Safe Harbour"), sermon.Series);
        Assert.Equal(("https://files.example/2026/11/01/harbour-4.mp3", "AbCdEfGhIj0", "Psalm 107:23-30"), (sermon.AudioUrl, sermon.YouTubeId, sermon.Passage));
        Assert.Equal("Where we drop anchor when the wind rises.\n\nNotes: read the whole psalm first.", sermon.Description);
        Assert.Equal("Sunday Morning", ServiceLabels.FromSummary(sermon.ServiceLabel, sermon.Speaker!));
        Assert.Equal(["https://files.example/2026/11/01/art/harbour.png", "https://files.example/2026/11/01/art/harbour-wide.png"],
            sermon.ArtworkUrls.Select(url => TithelyArtwork.OriginalOf(url, "files.example")));
    }

    [Fact]
    public void AnOldSermonByAGuestSpeakerHasNoSpeakerNoServiceAndNoSoundCloudCaption()
    {
        WebsiteSermon sermon = SermonPages.Read("/media/2019-05-25-bro-kit-ward-hope-in-a-storm", SampleFiles.Text("sermon-storm.html"))!;

        Assert.Equal((null, "", "", null), (sermon.Speaker, sermon.ServiceLabel, sermon.Description, sermon.Series));
        Assert.Equal(new CreditedTitle("Hope In A Storm", "Kit Ward", "Bro"), GuestTitles.Split(sermon.Title));
    }

    [Theory]
    [InlineData("Pastor Lee Varga - A Higher Perspective", "A Higher Perspective", "Lee Varga", "Pastor")]
    [InlineData("Missonary Tom Rook - Partakers Of His Nature", "Partakers Of His Nature", "Tom Rook", "Missionary")]
    [InlineData("Bro Kit Ward - Strengthening the Home - Part 7c - The Empty Nest", "Strengthening the Home - Part 7c - The Empty Nest", "Kit Ward", "Bro")]
    [InlineData("Things I've learned from John - Bro Kit Ward", "Things I've learned from John", "Kit Ward", "Bro")]
    [InlineData("Dr Ann Chalmers Jnr - Doers Of The Word", "Doers Of The Word", "Ann Chalmers Jnr", "Dr")]
    [InlineData("Bro Piet van Rooyen - The Sixteenth Verse", "The Sixteenth Verse", "Piet van Rooyen", "Bro")]
    public void AGuestSpeakersNameAndHonorificComeOutOfTheTitle(string title, string rest, string speaker, string honorific) =>
        Assert.Equal(new CreditedTitle(rest, speaker, honorific), GuestTitles.Split(title));

    [Theory]
    [InlineData("Missions Panel")]
    [InlineData("Kit Ward - Fun Facts, Fossils and Faith")]
    [InlineData("Q & A With A Legal Panel")]
    public void ATitleWithoutAnHonorificKeepsItsWords(string title) => Assert.Null(GuestTitles.Split(title));

    [Fact]
    public void ASeriesPageGivesItsNameDescriptionAndPicture() =>
        Assert.Equal(new WebsiteSeries("safe-harbour", "Safe Harbour", "Four weeks on where we shelter.", "https://files.example/2026/10/series/safe-harbour.png"),
            SeriesPage.Read("safe-harbour", "Safe harbour", SampleFiles.Text("series-safe-harbour.html")));

    [Theory]
    [InlineData("Sun AM", "Jo Ellery", "Sunday Morning")]
    [InlineData("Wed", "Jo Ellery", "Wednesday Night")]
    [InlineData("Sun AM - Ps Lee Varga", "Lee Varga", "Sunday Morning")]
    [InlineData("Mon PM", "Jo Ellery", "Monday evening")]
    [InlineData("Sun 9AM - Adults Session", "Jo Ellery", "Sunday 9am · Adults Session")]
    [InlineData("Wed 2nd Session - Youth", "Jo Ellery", "Wednesday 2nd Session · Youth")]
    [InlineData("Christmas Day", "Jo Ellery", "Christmas Day")]
    public void TheServiceIsReadFromTheWebsitesLabel(string label, string speaker, string service) =>
        Assert.Equal(service, ServiceLabels.FromSummary(label, speaker));

    [Theory]
    [InlineData("Morning Service", "2021-04-04", "Sunday Morning")]
    [InlineData("#Sundaynight", "2021-04-25", "Sunday Evening")]
    [InlineData("#Midweek Service", "2021-03-17", "Wednesday Night")]
    [InlineData("Mid week service.", "2021-03-17", "Wednesday Night")]
    [InlineData("Midweek Service - 26th May 2021", "2021-05-26", "Wednesday Night")]
    [InlineData("Morning Service", "2021-03-17", "Morning Service")]
    [InlineData("9AM service", "2021-04-04", "9AM service")]
    public void AnOldStyleLabelIsAStandardServiceOnlyOnItsDay(string label, string day, string service) =>
        Assert.Equal(service, ServiceLabels.FromPage(label, "Jo Ellery", DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void ADescriptionThatOnlyRepeatsTheServiceIsLeftOut()
    {
        Assert.Equal("", SermonImporter.Description("Midweek Service.", "#Midweek Service", "#Midweek Service"));
        Assert.Equal("A word for the week.", SermonImporter.Description("A word for the week.", "Wed PM", "Wednesday Night"));
    }
}
