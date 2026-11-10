using GSBC.ChurchApp.Features.People.Api.SafeMinistry;
using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class SmcValuesTests
{
    [Fact]
    public void TheBlueCardTrainingAndStatusComeAcrossWithEmptyDatesLeftEmpty()
    {
        SmcValues values = Values(SmcSamples.Person("88888", "Sam", "Example", wwccId: "123456/1", wwccExpiry: "2028-01-31",
            wwccChecked: "2026-05-14 03:56:02", wwccStatus: "CLEARED", status: "Cleared", statusWords: "All done", nextAction: "2027-05-14", progress: "complete"));
        SmcValues empty = Values(SmcSamples.Person("99999", "Alex", "Example", wwccExpiry: null, nextAction: "-0001-11-30"));

        Assert.Equal(("123456/1", BlueCards.Current, new DateOnly(2028, 1, 31), new DateOnly(2026, 5, 14)),
            (values.CardNumber, values.CardStatus, values.CardExpires, values.CardChecked));
        Assert.Equal(("complete", new DateOnly(2027, 5, 14), "Cleared", "All done"), (values.TrainingProgress, values.TrainingDue, values.Status, values.StatusWords));
        Assert.Empty(values.CheckReasons);
        Assert.Equal((null, null, null), (empty.CardExpires, empty.CardChecked, empty.TrainingDue));
    }

    [Theory]
    [InlineData("police", "pending", SmcReasons.PoliceCheck)]
    [InlineData("referee1", "negative", SmcReasons.Referee)]
    [InlineData("wwccStatus", "NOTFOUND", SmcReasons.CardNotVerified)]
    [InlineData("worksWithChildren", "1", SmcReasons.NoCard)]
    public void AClearProblemInAFieldThatIsNotBroughtAcrossRaisesAGenericReasonOnly(string field, string value, string reason)
    {
        string person = field switch
        {
            "police" => SmcSamples.Person("1", "Sam", "Example", police: value),
            "referee1" => SmcSamples.Person("1", "Sam", "Example", referee1: value),
            "wwccStatus" => SmcSamples.Person("1", "Sam", "Example", wwccId: "1/1", wwccStatus: value),
            _ => SmcSamples.Person("1", "Sam", "Example", worksWithChildren: value),
        };

        Assert.Equal([reason], Values(person).CheckReasons);
    }

    [Fact]
    public void WhatIsUsualForSomeoneClearedIsNotAProblem() =>
        Assert.Empty(Values(SmcSamples.Person("1", "Sam", "Example", status: "Cleared", wwccId: "1/1", wwccStatus: "CLEARED",
            wwccNote: "WWCC Status should be verified again", photo: "Pending", police: "", referee1: "positive", referee2: "positive_admin",
            disclosure: "None", worksWithChildren: "1")).CheckReasons);

    [Fact]
    public void ADisclosureIsOnlyFlaggedUntilTheyAreCleared()
    {
        Assert.Empty(Values(SmcSamples.Person("1", "Sam", "Example", status: "Cleared", disclosure: "See their file")).CheckReasons);
        Assert.Equal([SmcReasons.Status("Pending"), SmcReasons.Disclosure],
            Values(SmcSamples.Person("1", "Sam", "Example", status: "Pending", disclosure: "See their file")).CheckReasons);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Error")]
    public void AStatusOtherThanClearedIsAReasonToCheck(string status) =>
        Assert.Equal([SmcReasons.Status(status)], Values(SmcSamples.Person("1", "Sam", "Example", status: status)).CheckReasons);

    [Theory]
    [InlineData("2026-01-31", "complete")]
    [InlineData("2027-01-31", "in progress")]
    public void PendingOnlyBecauseOfTrainingIsLeftToTheTrainingFlag(string nextAction, string progress)
    {
        Assert.Empty(Values(SmcSamples.Person("1", "Sam", "Example", status: "Pending", nextAction: nextAction, progress: progress)).CheckReasons);
        Assert.Equal([SmcReasons.PoliceCheck],
            Values(SmcSamples.Person("1", "Sam", "Example", status: "Pending", nextAction: nextAction, progress: progress, police: "pending")).CheckReasons);
    }

    [Fact]
    public void TheirBlueCardWordsMapToOurStatuses()
    {
        Assert.Equal(BlueCards.Current, Values(SmcSamples.Person("1", "Sam", "Example", wwccId: "1/1", wwccStatus: "CLEARED")).CardStatus);
        Assert.Equal(BlueCards.Expired, Values(SmcSamples.Person("1", "Sam", "Example", wwccId: "1/1", wwccStatus: "EXPIRED")).CardStatus);
    }

    [Fact]
    public void TheTwoPeopleInTheirSampleReplyBothParse()
    {
        SmcPage page = SmcJson.Page(SmcSamples.Page(1, 1, SmcSamples.Person("1", "Sam", "Example"), SmcSamples.Person("2", "Alex", "Example", dob: null)));

        Assert.Equal(["1", "2"], page.People.Select(person => person.Id));
        Assert.Equal(1, page.Pages);
        Assert.Equal("Sam Example", page.People[0].Name);
    }

    private static SmcValues Values(string person) => SmcValues.From(SmcJson.Person(person), new DateOnly(2026, 11, 10));
}
