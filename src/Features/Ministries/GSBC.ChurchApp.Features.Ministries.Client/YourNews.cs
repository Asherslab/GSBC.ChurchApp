using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed record YourNews(bool InAny, IReadOnlyList<(Post Post, string Ministry)> Posts)
{
    public static readonly YourNews None = new(false, []);
}
