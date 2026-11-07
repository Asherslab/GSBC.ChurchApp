using System.Net;
using System.Net.Http.Headers;
using GSBC.ChurchApp.Features.Events.Api.Data;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class ICalReadingTests
{
    private const string YouthCalendar = """
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//Example//Calendar//EN
        BEGIN:VEVENT
        UID:school-camp
        DTSTART;VALUE=DATE:20261201
        DTEND;VALUE=DATE:20261204
        SUMMARY:School camp\, Year 6
        END:VEVENT
        BEGIN:VEVENT
        UID:youth-night
        DTSTART;TZID=Australia/Sydney:20261113T190000
        DTEND;TZID=Australia/Sydney:20261113T210000
        RRULE:FREQ=WEEKLY;BYDAY=FR;COUNT=4
        EXDATE;TZID=Australia/Sydney:20261120T190000
        SUMMARY:Youth night
        LOCATION:Church hall
        BEGIN:VALARM
        ACTION:DISPLAY
        DESCRIPTION:Reminder
        TRIGGER:-PT15M
        END:VALARM
        END:VEVENT
        BEGIN:VEVENT
        UID:youth-night
        RECURRENCE-ID;TZID=Australia/Sydney:20261127T190000
        DTSTART;TZID=Australia/Sydney:20261127T200000
        DTEND;TZID=Australia/Sydney:20261127T220000
        SUMMARY:Youth night: games
        END:VEVENT
        END:VCALENDAR
        """;

    [Fact]
    public void AnOutsideCalendarBecomesDatedEventsWithItsRepeatsSkippedDatesAndChangedDatesInChurchTime()
    {
        List<EventRecord> copied = ICalFeed.Expand(ICalFeed.Read(YouthCalendar), new DateOnly(2026, 10, 9), new DateOnly(2027, 11, 8))
            .Select(copy =>
            {
                EventRecord record = new() { SourceUid = copy.SourceUid };
                ICalCopier.Fill(record, copy);
                return record;
            })
            .OrderBy(record => record.StartDate)
            .ToList();

        Assert.Equal(
            [
                ("youth-night/20261113T080000Z", "Youth night", new DateOnly(2026, 11, 13), null, 18 * 60, 20 * 60, "Church hall"),
                ("youth-night/20261127T080000Z", "Youth night: games", new DateOnly(2026, 11, 27), null, 19 * 60, 21 * 60, ""),
                ("school-camp", "School camp, Year 6", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), null, null, ""),
                ("youth-night/20261204T080000Z", "Youth night", new DateOnly(2026, 12, 4), null, 18 * 60, 20 * 60, "Church hall"),
            ],
            copied.Select(record => (record.SourceUid!, record.Fields.Title, record.StartDate, record.EndDate, record.StartMinute, record.EndMinute, record.Fields.Place?.Address ?? "")).ToList());
        Assert.All(copied, record => Assert.Null(record.Fields.Description));
    }

    [Fact]
    public async Task AnUnchangedCalendarIsAskedForWithItsETagAndAnswersNotChanged()
    {
        Address address = new();
        ICalFetcher fetcher = new(new HttpClient(address));
        Uri source = ICalFetcher.AddressOf("webcal://calendar.example.org/youth.ics")!;

        FetchedCalendar first = await fetcher.FetchAsync(source, null, null, TestContext.Current.CancellationToken);
        FetchedCalendar second = await fetcher.FetchAsync(source, first.ETag, first.LastModified, TestContext.Current.CancellationToken);

        Assert.Equal("https://calendar.example.org/youth.ics", source.ToString());
        Assert.True(first.Changed);
        Assert.Equal("\"1\"", Assert.Single(address.Requests[^1].Headers.IfNoneMatch).Tag);
        Assert.False(second.Changed);
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.20.1.1")]
    [InlineData("192.168.1.10")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:10.0.0.5")]
    public void AnAddressOnAPrivateNetworkIsNeverRead(string address) =>
        Assert.False(ICalFetcher.IsPublic(IPAddress.Parse(address)));

    private sealed class Address : HttpMessageHandler
    {
        private const string ETag = "\"1\"";

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Headers.IfNoneMatch.Any(tag => tag.Tag == ETag))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent(YouthCalendar) };
            response.Headers.ETag = new EntityTagHeaderValue(ETag);
            return Task.FromResult(response);
        }
    }
}
