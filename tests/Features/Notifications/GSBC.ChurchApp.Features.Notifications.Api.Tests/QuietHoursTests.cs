using System.Globalization;
using GSBC.ChurchApp.Features.Notifications.Contracts;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public sealed class QuietHoursTests
{
    [Theory]
    [InlineData("2026-11-04 20:59", null)]
    [InlineData("2026-11-04 21:00", "2026-11-05 07:00")]
    [InlineData("2026-11-05 01:00", "2026-11-05 07:00")]
    [InlineData("2026-11-05 06:59", "2026-11-05 07:00")]
    [InlineData("2026-11-05 07:00", null)]
    public void PhonesWaitFromNinePmUntilSevenAm(string churchTime, string? heldUntil) =>
        Assert.Equal(heldUntil is null ? null : At(heldUntil), QuietHours.HeldUntil(At(churchTime)));

    private static DateTime At(string text) => DateTime.ParseExact(text, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
