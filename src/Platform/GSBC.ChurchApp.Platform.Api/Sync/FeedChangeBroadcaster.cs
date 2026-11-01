using System.Threading.Channels;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public sealed class FeedChangeBroadcaster
{
    private readonly Lock _gate = new();
    private readonly List<Channel<string?>> _subscribers = [];

    public Subscription Subscribe()
    {
        Channel<string?> channel = Channel.CreateUnbounded<string?>(new UnboundedChannelOptions { SingleReader = true });
        lock (_gate)
            _subscribers.Add(channel);
        return new Subscription(this, channel);
    }

    public void PublishChange(string feed) => Publish(feed);

    public void PublishAllMayHaveChanged() => Publish(null);

    private void Publish(string? feed)
    {
        lock (_gate)
        {
            foreach (Channel<string?> subscriber in _subscribers)
                subscriber.Writer.TryWrite(feed);
        }
    }

    private void Remove(Channel<string?> channel)
    {
        lock (_gate)
            _subscribers.Remove(channel);
    }

    public sealed class Subscription(FeedChangeBroadcaster owner, Channel<string?> channel) : IDisposable
    {
        public async Task<ChangedFeeds?> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using CancellationTokenSource timer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timer.CancelAfter(timeout);
            try
            {
                await channel.Reader.WaitToReadAsync(timer.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            HashSet<string> feeds = [];
            bool all = false;
            while (channel.Reader.TryRead(out string? feed))
            {
                if (feed is null)
                    all = true;
                else
                    feeds.Add(feed);
            }

            return new ChangedFeeds(all, feeds);
        }

        public void Dispose() => owner.Remove(channel);
    }
}

public sealed record ChangedFeeds(bool All, IReadOnlySet<string> Names)
{
    public bool Includes(string feed) => All || Names.Contains(feed);
}
