using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.News;

public sealed record NewsItem(
    string Id, string Title, RichText Body, MediaRef? Picture, DateTime PostedAt, DateTime? EditedAt, string Author, string From, string Href,
    bool Urgent = false, string Status = "");

public sealed record NewsFromSource(bool Applies, IReadOnlyList<NewsItem> Items)
{
    public static readonly NewsFromSource None = new(false, []);
}

public interface INewsSource
{
    IReadOnlyList<string> Feeds { get; }

    Task<NewsFromSource> LoadAsync();
}

public enum ChurchNewsStanding
{
    None,
    Asked,
    InChurchNews,
    Kept,
}

public sealed record MinistryNewsItem(NewsItem Item, string MinistryId, ChurchNewsStanding Standing);

public interface IEveryMinistrysNews
{
    IReadOnlyList<string> Feeds { get; }

    Task<IReadOnlyList<MinistryNewsItem>> LoadAsync();

    Task PutInChurchNewsAsync(MinistryNewsItem post, bool tell);
}

public static class NewsSources
{
    public static IServiceCollection AddEveryMinistrysNews<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IServiceCollection services)
        where TSource : class, IEveryMinistrysNews =>
        services.AddSingleton<IEveryMinistrysNews, TSource>();

    public static IServiceCollection AddNewsSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IServiceCollection services)
        where TSource : class, INewsSource =>
        services.AddSingleton<INewsSource, TSource>();
}
