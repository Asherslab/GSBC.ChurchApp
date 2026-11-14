using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Client;

public sealed class NewsWrites(IWrites writes, IServiceProvider services)
{
    public bool Queued => writes.Queued;

    public async Task<NewsPost> SaveAsync(NewsPost post, IReadOnlyList<MediaRef> waiting)
    {
        NewsPost sent = NewsContractsModel.Instance.DeepClone(post);
        if (sent.Id.Length == 0)
            (sent.Id, sent.New) = (Guid.CreateVersion7().ToString(), true);
        sent.Picture = null;
        NewsSave save = new() { Post = sent };
        FileUploads? uploads = services.GetService<FileUploads>();
        List<QueuedUpload> queued = uploads is null ? [] : waiting
            .Select(media => uploads.For(media, MediaSlot.Id(NewsAreas.UploadedInSave), new UploadTarget(media.AltText, Area: NewsAreas.Media, AreaScopeId: NewsAreas.Media)))
            .OfType<QueuedUpload>().ToList();
        if (queued.Count > 0 && services.GetService<IOutbox>() is { } outbox)
            await EnqueueAsync(outbox, NewsMethods.Save, save, queued);
        else
            await SendAsync(NewsMethods.Save, save);
        return sent;
    }

    public Task PublishAsync(string id, DateTime? at) => SendAsync(NewsMethods.Publish, new PublishRequest { Id = id, At = at });

    public Task AskToPublishAsync(string id) => SendAsync(NewsMethods.AskToPublish, new NewsRef { Id = id });

    public Task BackToWritingAsync(string id) => SendAsync(NewsMethods.BackToWriting, new NewsRef { Id = id });

    public Task ArchiveAsync(string id) => SendAsync(NewsMethods.Archive, new NewsRef { Id = id });

    public Task PutBackAsync(string id) => SendAsync(NewsMethods.PutBack, new NewsRef { Id = id });

    public Task DeleteAsync(string id) => SendAsync(NewsMethods.Delete, new NewsRef { Id = id });

    public Task TryOnMyPhoneAsync(string id) => SendAsync(NewsMethods.TryOnMyPhone, new NewsRef { Id = id });

    public Task AnswerAskAsync(string id, bool yes, bool tell) => SendAsync(NewsMethods.AnswerAsk, new AskAnswer { Id = id, Yes = yes, Tell = tell });

    private async Task SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request) =>
        await writes.SendAsync(method, request, NewsContractsModel.Instance);

    private static async Task EnqueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IOutbox outbox, string method, T request, IReadOnlyList<QueuedUpload> uploads)
    {
        using MemoryStream body = new();
        NewsContractsModel.Instance.Serialize(body, request);
        await outbox.EnqueueAsync(method, body.ToArray(), uploads);
    }
}
