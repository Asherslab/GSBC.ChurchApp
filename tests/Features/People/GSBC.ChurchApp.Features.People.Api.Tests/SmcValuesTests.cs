using GSBC.ChurchApp.Features.People.Api.SafeMinistry;
using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class SmcValuesTests
{
    [Fact]
    public void TheBlueCardTrainingAndStatusComeAcrossWithEmptyDatesLeftEmpty()
    {
        SmcValues values = Values(SmcSamples.Person("88888", "Sam", "Example", wwccId: "123456/1", wwccExpiry: "2028-01-31",
            wwccChecked: "2026-05-14 03:56:02", wwccStatus: "Current", status: "Approved", statusWords: "All done", nextAction: "2027-05-14", progress: "complete"));
        SmcValues empty = Values(SmcSamples.Person("99999", "Alex", "Example", wwccExpiry: null, nextAction: "-0001-11-30"));

        Assert.Equal(("123456/1", BlueCards.Current, new DateOnly(2028, 1, 31), new DateOnly(2026, 5, 14)),
            (values.CardNumber, values.CardStatus, values.CardExpires, values.CardChecked));
        Assert.Equal(("complete", new DateOnly(2027, 5, 14), "Approved", "All done"), (values.TrainingProgress, values.TrainingDue, values.Status, values.StatusWords));
        Assert.Empty(values.CheckReasons);
        Assert.Equal((null, null, null), (empty.CardExpires, empty.CardChecked, empty.TrainingDue));
    }

    [Theory]
    [InlineData("police", "pending", SmcReasons.PoliceCheck)]
    [InlineData("referee1", "negative", SmcReasons.Referee)]
    [InlineData("photo", "Pending", SmcReasons.Photo)]
    [InlineData("disclosure", "See file", SmcReasons.Disclosure)]
    [InlineData("wwccNote", "WWCC Status should be verified again", SmcReasons.CardNote)]
    [InlineData("worksWithChildren", "1", SmcReasons.NoCard)]
    public void AProblemInAFieldThatIsNotBroughtAcrossRaisesAGenericReasonOnly(string field, string value, string reason)
    {
        string person = field switch
        {
            "police" => SmcSamples.Person("1", "Sam", "Example", police: value),
            "referee1" => SmcSamples.Person("1", "Sam", "Example", referee1: value),
            "photo" => SmcSamples.Person("1", "Sam", "Example", photo: value),
            "disclosure" => SmcSamples.Person("1", "Sam", "Example", disclosure: value),
            "wwccNote" => SmcSamples.Person("1", "Sam", "Example", wwccNote: value),
            _ => SmcSamples.Person("1", "Sam", "Example", worksWithChildren: value),
        };

        Assert.Equal([reason], Values(person).CheckReasons);
    }

    [Fact]
    public void ClearedPositiveAndNoneAreNotProblems() =>
        Assert.Empty(Values(SmcSamples.Person("1", "Sam", "Example", police: "cleared", referee1: "positive", referee2: "positive_admin",
            photo: "Approved", disclosure: "None", wwccId: "1/1", worksWithChildren: "1")).CheckReasons);

    [Fact]
    public void AStatusThatIsNotApprovedIsAReasonToCheck() =>
        Assert.Equal([SmcReasons.Status("Pending")], Values(SmcSamples.Person("1", "Sam", "Example", status: "Pending")).CheckReasons);

    [Fact]
    public void TheTwoPeopleInTheirSampleReplyBothParse()
    {
        SmcPage page = SmcJson.Page(SmcSamples.Page(1, 1, SmcSamples.Person("1", "Sam", "Example"), SmcSamples.Person("2", "Alex", "Example", dob: null)));

        Assert.Equal(["1", "2"], page.People.Select(person => person.Id));
        Assert.Equal(1, page.Pages);
        Assert.Equal("Sam Example", page.People[0].Name);
    }

    private static SmcValues Values(string person) => SmcValues.From(SmcJson.Person(person));
}
