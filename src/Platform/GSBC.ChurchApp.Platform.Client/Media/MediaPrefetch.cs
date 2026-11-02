using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Client.Media;

public sealed record MediaPrefetch(FeedPageQuery Records, Func<StoredRecord, IEnumerable<MediaRef>> MediaIn);

public sealed class MediaPrefetcher(ILocalFeeds feeds, IMediaFiles files, IEnumerable<MediaPrefetch> rules)
{
    private readonly SemaphoreSlim _running = new(1, 1);

    public void Start()
    {
        if (!feeds.IsAvailable || !rules.Any())
            return;
        feeds.FeedsChanged += changed => _ = PrefetchAsync(rules.Where(rule => changed.Contains(rule.Records.Feed)).ToList());
        _ = PrefetchAsync(rules.ToList());
    }

    private async Task PrefetchAsync(IReadOnlyList<MediaPrefetch> due)
    {
        if (due.Count == 0)
            return;

        await _running.WaitAsync();
        try
        {
            List<MediaRef> media = [];
            foreach (MediaPrefetch rule in due)
                media.AddRange((await feeds.ReadPageAsync(rule.Records)).SelectMany(rule.MediaIn));
            await files.PrefetchAsync(media);
        }
        catch (Exception exception) when (exception is Microsoft.JSInterop.JSException or Grpc.Core.RpcException or InvalidOperationException)
        {
        }
        finally
        {
            _running.Release();
        }
    }
}

public static class MediaPrefetchServiceCollectionExtensions
{
    public static IServiceCollection AddMediaPrefetch(this IServiceCollection services, Func<IServiceProvider, MediaPrefetch> rule)
    {
        services.TryAddSingleton<MediaPrefetcher>();
        return services.AddSingleton(rule);
    }
}
