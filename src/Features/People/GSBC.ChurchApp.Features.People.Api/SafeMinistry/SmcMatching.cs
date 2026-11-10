using System.Text;
using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal sealed record AppPerson(Guid Id, string Name, string? Email, DateOnly? BirthDate);

internal sealed record SmcCandidate(string SmcId, string FirstName, string MiddleName, string Surname, string Email, DateOnly? BirthDate, bool ByHandOnly)
{
    public static SmcCandidate From(SmcRow row, bool byHandOnly) =>
        new(row.Id, row["firstname"], row["middlename"], row["surname"], row["email"], row.Day("dob"), byHandOnly);
}

internal sealed record SmcMatch(string SmcId, Guid PersonId, string How);

internal sealed record SmcMatches(IReadOnlyList<SmcMatch> Found, IReadOnlyDictionary<string, string> Unmatched);

internal static class SmcWhy
{
    public const string NoOne = "No one in the app has this email, or this name and birth date";
    public const string SharedEmail = "More than one person in the app has this email";
    public const string SharedName = "More than one person in the app has this name and birth date";
    public const string SamePerson = "Another Safe Ministry Check record matches the same person in the app";
    public const string ByHandOnly = "Unlinked by staff, so it's only linked by hand";
}

internal static class SmcMatching
{
    public static SmcMatches Match(IReadOnlyCollection<SmcCandidate> records, IReadOnlyCollection<AppPerson> people, IReadOnlyDictionary<string, Guid> linked)
    {
        HashSet<Guid> taken = [.. linked.Values];
        List<AppPerson> free = people.Where(person => !taken.Contains(person.Id)).ToList();
        ILookup<string, AppPerson> byEmail = free.Where(person => Email(person.Email).Length > 0).ToLookup(person => Email(person.Email));
        ILookup<string, AppPerson> byNameAndBirth = free.Where(person => person.BirthDate is not null && Name(person.Name).Length > 0)
            .ToLookup(person => Key(Name(person.Name), person.BirthDate!.Value));

        List<SmcMatch> proposed = [];
        Dictionary<string, string> unmatched = [];
        foreach (SmcCandidate record in records.Where(record => !linked.ContainsKey(record.SmcId)))
        {
            if (record.ByHandOnly)
            {
                unmatched[record.SmcId] = SmcWhy.ByHandOnly;
                continue;
            }

            List<AppPerson> sameEmail = Email(record.Email) is { Length: > 0 } email ? byEmail[email].ToList() : [];
            List<AppPerson> sameNameAndBirth = record.BirthDate is { } born
                ? Names(record).SelectMany(name => byNameAndBirth[Key(name, born)]).DistinctBy(person => person.Id).ToList()
                : [];
            switch (sameEmail.Count, sameNameAndBirth.Count)
            {
                case (1, _):
                    proposed.Add(new SmcMatch(record.SmcId, sameEmail[0].Id, SmcLinkHow.Email));
                    break;
                case (> 1, _):
                    unmatched[record.SmcId] = SmcWhy.SharedEmail;
                    break;
                case (0, 1):
                    proposed.Add(new SmcMatch(record.SmcId, sameNameAndBirth[0].Id, SmcLinkHow.NameAndBirthDate));
                    break;
                case (0, > 1):
                    unmatched[record.SmcId] = SmcWhy.SharedName;
                    break;
                default:
                    unmatched[record.SmcId] = SmcWhy.NoOne;
                    break;
            }
        }

        List<SmcMatch> found = [];
        foreach (IGrouping<Guid, SmcMatch> forPerson in proposed.GroupBy(match => match.PersonId))
        {
            if (forPerson.Count() == 1)
                found.Add(forPerson.Single());
            else
                foreach (SmcMatch clash in forPerson)
                    unmatched[clash.SmcId] = SmcWhy.SamePerson;
        }
        return new SmcMatches(found, unmatched);
    }

    private static string Email(string? email) => email?.Trim().ToUpperInvariant() ?? "";

    private static string Name(string name)
    {
        StringBuilder plain = new();
        foreach (char letter in name.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(letter))
                plain.Append(char.ToLowerInvariant(letter));
            else if ((char.IsWhiteSpace(letter) || letter == '-') && plain.Length > 0 && plain[^1] != ' ')
                plain.Append(' ');
        }
        return plain.ToString().TrimEnd();
    }

    private static IEnumerable<string> Names(SmcCandidate record) =>
        new[] { $"{record.FirstName} {record.Surname}", $"{record.FirstName} {record.MiddleName} {record.Surname}" }
            .Select(Name).Where(name => name.Length > 0).Distinct();

    private static string Key(string name, DateOnly born) => $"{name}|{born:yyyy-MM-dd}";
}
