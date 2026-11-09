using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class AgeTests
{
    [Theory]
    [InlineData("2008-11-10", "2026-11-09", true)]
    [InlineData("2008-11-10", "2026-11-10", false)]
    public void SomeoneIsUnder18UntilTheirEighteenthBirthday(string born, string on, bool minor) =>
        Assert.Equal(minor, Ages.IsMinor(DateOnly.Parse(born), DateOnly.Parse(on)));

    [Fact]
    public void NoBirthDateIsNeverTakenAsUnder18() => Assert.False(Ages.IsMinor(null, new DateOnly(2026, 11, 10)));

    [Fact]
    public void AParentLooksAfterTheirChildUntilMidnightOnTheirEighteenthBirthdayInBrisbane() =>
        Assert.Equal(new DateTime(2026, 11, 9, 14, 0, 0, DateTimeKind.Utc), FamilyRules.ParentRoleEnds(new DateOnly(2008, 11, 10)));
}
