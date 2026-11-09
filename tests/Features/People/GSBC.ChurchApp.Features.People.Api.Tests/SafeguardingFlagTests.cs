using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class SafeguardingFlagTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    [Fact]
    public void NothingRecordedIsNotFlagged() => Assert.Empty(SafeguardingFlags.For(new SafeguardingRecord(), Today));

    [Theory]
    [InlineData("2026-11-09", FlagKinds.RunOut)]
    [InlineData("2026-11-10", FlagKinds.DueSoon)]
    [InlineData("2027-01-09", FlagKinds.DueSoon)]
    public void ACardIsFlaggedOnceItHasExpiredOrExpiresWithin60Days(string expires, string kind) =>
        Assert.Equal(kind, Card(Record(cardExpires: expires))?.Kind);

    [Fact]
    public void ACardExpiringIn61DaysIsNotFlagged() => Assert.Null(Card(Record(cardExpires: "2027-01-10")));

    [Theory]
    [InlineData(BlueCards.Suspended)]
    [InlineData(BlueCards.Cancelled)]
    [InlineData(BlueCards.Applied)]
    public void ACardThatIsNotCurrentIsFlaggedWhateverItsDate(string status) =>
        Assert.Equal(FlagKinds.RunOut, Card(Record(cardStatus: status, cardExpires: "2029-01-01"))?.Kind);

    [Fact]
    public void AnExemptionCardCountsAsHeld() => Assert.Null(Card(Record(cardStatus: BlueCards.Exemption, cardExpires: "2029-01-01")));

    [Fact]
    public void TrainingWithoutACardFlagsTheMissingCard()
    {
        SafeguardingFlag flag = Assert.Single(SafeguardingFlags.For(Record(trainingDue: "2028-01-01"), Today));
        Assert.Equal((FlagSubject.BlueCard, FlagKinds.NotRecorded), (flag.Subject, flag.Kind));
    }

    [Fact]
    public void ACardWithoutTrainingFlagsTheMissingTraining()
    {
        SafeguardingFlag flag = Assert.Single(SafeguardingFlags.For(Record(cardExpires: "2029-01-01", training: false), Today));
        Assert.Equal((FlagSubject.Training, FlagKinds.NotRecorded), (flag.Subject, flag.Kind));
    }

    [Fact]
    public void TrainingIsDueThreeYearsAfterItWasDoneUnlessADueDateIsGiven()
    {
        Assert.Equal(new DateOnly(2026, 11, 20), FamilyRules.TrainingDue(new DateOnly(2023, 11, 20), null));
        Assert.Equal(new DateOnly(2025, 1, 1), FamilyRules.TrainingDue(new DateOnly(2023, 11, 20), new DateOnly(2025, 1, 1)));
    }

    private static SafeguardingFlag? Card(SafeguardingRecord record) =>
        SafeguardingFlags.For(record, Today).FirstOrDefault(flag => flag.Subject == FlagSubject.BlueCard);

    private static SafeguardingRecord Record(string cardStatus = "", string? cardExpires = null, string? trainingDue = null, bool training = true) => new()
    {
        CardNumber = cardExpires is null && cardStatus.Length == 0 ? "" : "1234567/2",
        CardStatus = cardStatus,
        CardExpires = Day(cardExpires),
        TrainingDue = Day(trainingDue) ?? (training && (cardExpires is not null || cardStatus.Length > 0) ? Day("2029-06-01") : null),
    };

    private static DateTime? Day(string? day) => day is null ? null : WireDates.ToWire(DateOnly.Parse(day));
}
