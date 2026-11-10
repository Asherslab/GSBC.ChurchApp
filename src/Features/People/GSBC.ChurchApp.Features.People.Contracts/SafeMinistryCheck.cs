namespace GSBC.ChurchApp.Features.People.Contracts;

public static class SmcLinkHow
{
    public const string Email = "email";
    public const string NameAndBirthDate = "name-and-birth-date";
    public const string ByHand = "hand";

    public static string Words(string how) => how switch
    {
        Email => "matched by email",
        NameAndBirthDate => "matched by name and birth date",
        _ => "linked by hand",
    };
}

public static class SafeMinistryTraining
{
    public static bool Finished(string progress) => progress.Trim().ToLowerInvariant() is "complete" or "completed";

    public static string Words(string progress) =>
        Finished(progress) ? "Completed" : progress.Trim().Length == 0 ? "Not started" : "Not finished yet";
}
