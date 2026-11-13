using GSBC.ChurchApp.Features.News.Contracts;

namespace GSBC.ChurchApp.Features.News.Client;

public interface INewsReads
{
    IReadOnlyList<string> Feeds { get; }

    Task<IReadOnlyList<NewsPost>> PublishedAsync();

    Task<IReadOnlyList<NewsPost>> DeskAsync();
}
