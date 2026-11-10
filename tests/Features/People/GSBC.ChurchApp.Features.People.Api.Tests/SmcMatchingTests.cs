using GSBC.ChurchApp.Features.People.Api.SafeMinistry;
using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class SmcMatchingTests
{
    private static readonly DateOnly Born = new(1990, 5, 15);
    private readonly AppPerson _sam = new(Guid.CreateVersion7(), "Sam Example", "sam@example.com", null);
    private readonly AppPerson _zoe = new(Guid.CreateVersion7(), "Zoë O'Neil-Example", "", Born);

    [Fact]
    public void AnEmailMatchesWhateverItsCase() =>
        Assert.Equal([new SmcMatch("1", _sam.Id, SmcLinkHow.Email)], Match([Record("1", email: "SAM@Example.com ")], [_sam, _zoe]).Found);

    [Fact]
    public void WithoutAnEmailANameAndBirthDateMatchIgnoringAccentsPunctuationAndAMiddleName() =>
        Assert.Equal([new SmcMatch("1", _zoe.Id, SmcLinkHow.NameAndBirthDate)],
            Match([Record("1", first: "Zoe", middle: "", surname: "ONeil Example", born: Born)], [_sam, _zoe]).Found);

    [Fact]
    public void ANameWithoutABirthDateDoesNotMatch() =>
        Assert.Equal(SmcWhy.NoOne, Match([Record("1", first: "Zoe", surname: "O'Neil-Example")], [_zoe]).Unmatched["1"]);

    [Fact]
    public void AnEmailTwoPeopleShareIsLeftForStaff()
    {
        AppPerson twin = _sam with { Id = Guid.CreateVersion7(), Name = "Sam Other" };

        SmcMatches matches = Match([Record("1", email: "sam@example.com")], [_sam, twin]);

        Assert.Empty(matches.Found);
        Assert.Equal(SmcWhy.SharedEmail, matches.Unmatched["1"]);
    }

    [Fact]
    public void TwoRecordsMatchingOnePersonAreBothLeftForStaff()
    {
        SmcMatches matches = Match([Record("1", email: "sam@example.com"), Record("2", first: "Sam", surname: "Example", email: "SAM@EXAMPLE.COM")], [_sam]);

        Assert.Empty(matches.Found);
        Assert.Equal([SmcWhy.SamePerson, SmcWhy.SamePerson], [matches.Unmatched["1"], matches.Unmatched["2"]]);
    }

    [Fact]
    public void ALinkIsKeptAndTheLinkedPersonIsNotMatchedAgain()
    {
        SmcMatches matches = Match([Record("1", email: "changed@example.com"), Record("2", email: "sam@example.com")], [_sam],
            new Dictionary<string, Guid> { ["1"] = _sam.Id });

        Assert.Empty(matches.Found);
        Assert.False(matches.Unmatched.ContainsKey("1"));
        Assert.Equal(SmcWhy.NoOne, matches.Unmatched["2"]);
    }

    [Fact]
    public void ARecordStaffUnlinkedIsOnlyLinkedByHand() =>
        Assert.Equal(SmcWhy.ByHandOnly, Match([Record("1", email: "sam@example.com", byHandOnly: true)], [_sam]).Unmatched["1"]);

    private static SmcMatches Match(IReadOnlyCollection<SmcCandidate> records, IReadOnlyCollection<AppPerson> people, Dictionary<string, Guid>? linked = null) =>
        SmcMatching.Match(records, people, linked ?? []);

    private static SmcCandidate Record(string id, string first = "Someone", string middle = "", string surname = "Else", string email = "", DateOnly? born = null, bool byHandOnly = false) =>
        new(id, first, middle, surname, email, born, byHandOnly);
}
