namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesFeeds
{
    public const string Ministries = "ministries";
    public const string Directory = "ministries.directory";
    public const string Posts = "ministries.posts";
    public const string PrivatePosts = "ministries.posts.private";
    public const string Files = "ministries.files";
    public const string PrivateFiles = "ministries.files.private";
    public const string Calendars = "ministries.calendars";
    public const string PrivateCalendars = "ministries.calendars.private";
    public const string Rounds = "ministries.rounds";
    public const string SittingOut = "ministries.sitting-out";
    public const string LifeGroups = "ministries.lifegroups";
    public const string People = "ministries.people";
    public const string Yours = "ministries.yours";

    public static readonly IReadOnlyList<string> OnPhones = [Directory, Posts, Files, Calendars, Rounds, SittingOut, LifeGroups];
    public static readonly IReadOnlyList<string> PrivateOnPhones = [PrivatePosts, PrivateFiles, PrivateCalendars, People, Yours];
    public static readonly IReadOnlyList<string> Published = [.. OnPhones, .. PrivateOnPhones];
}
