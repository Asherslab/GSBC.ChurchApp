using System.Text;
using GSBC.ChurchApp.Features.Events.Api;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Unit.Tests;

public sealed class ICalWritingTests
{
    [Fact]
    public void EachDateIsItsOwnEventWithTextEscapedLinesFoldedAndAllDayEndsExclusive()
    {
        EventOccurrence camp = new()
        {
            Id = "camp", Date = "2026-12-01", EndDate = "2026-12-03", AllDay = true,
            Fields = new EventFields { Title = "Kids camp; bring a sleeping bag, pillow", Place = new EventPlace { Name = "Teralba Park", Address = "Teralba Park" } },
            Missing = [EventFieldKeys.Contacts],
        };
        EventOccurrence service = new()
        {
            Id = "service", Date = "2026-11-08",
            StartsAt = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 18, 0, 0)), EndsAt = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 19, 30, 0)),
            Fields = new EventFields { Title = new string('é', 60) },
        };

        string text = ICalWriter.Write("Kids", [camp, service], new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc));
        string[] lines = text.Split("\r\n");

        Assert.Contains("UID:camp@calendar.gsbc-churchapp", lines);
        Assert.Contains("DTSTART;VALUE=DATE:20261201", lines);
        Assert.Contains("DTEND;VALUE=DATE:20261204", lines);
        Assert.Contains("""SUMMARY:Kids camp\; bring a sleeping bag\, pillow""", lines);
        Assert.Contains("LOCATION:Teralba Park", lines);
        Assert.Contains("DESCRIPTION:Contact to be announced", lines);
        Assert.Contains("DTSTART:20261108T080000Z", lines);
        Assert.Contains("DTEND:20261108T093000Z", lines);
        Assert.All(lines, line => Assert.True(Encoding.UTF8.GetByteCount(line) <= 75, line));
        Assert.Contains(lines, line => line.StartsWith(' '));
    }
}
