using System.Reflection;
using Grpc.Core;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.AspNetCore.Components;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public sealed record BrowserHere(string Browser, string System, string Window, string ColourScheme, string Title);

[AdminOnly]
public sealed class FeedbackFromAdmin(ChannelBase channel, IDirectUploads uploads, RecentPages recent, NavigationManager navigation)
{
    private const int ShortCommit = 7;
    private readonly IFeedbackService _feedback = channel.CreateGrpcService<IFeedbackService>();

    public string Page => "/" + navigation.ToBaseRelativePath(navigation.Uri).Split('?', '#')[0];

    private static string Version
    {
        get
        {
            string version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            string[] parts = version.Split('+', 2);
            return parts is [_, { Length: > ShortCommit }] ? $"{parts[0]}+{parts[1][..ShortCommit]}" : version;
        }
    }

    public Task<MediaRef> UploadAsync(string ticketId, Stream content, string fileName, string contentType, long bytes, CancellationToken cancellationToken = default) =>
        uploads.UploadAsync(content, fileName, contentType, bytes,
            new UploadTarget(PrivateTo: FeedbackPermissions.TriageNode, PrivateToScope: ticketId, Area: FeedbackAreas.Attachments, AreaScopeId: ticketId), cancellationToken);

    public async Task SendAsync(string ticketId, string text, IReadOnlyList<MediaRef> attachments, BrowserHere here)
    {
        BrowserDetails browser = new()
        {
            Browser = here.Browser, System = here.System, Window = here.Window, ColourScheme = here.ColourScheme, Version = Version, PageTitle = here.Title,
            RecentPages = recent.Recent.ToList(),
        };
        await _feedback.SendAsync(new FeedbackSend
        {
            Id = ticketId, Text = text.Trim(), AttachmentIds = attachments.Select(media => media.Id).ToList(), Page = Page, Source = FeedbackSource.AdminPanel,
            Browser = browser,
        });
    }
}
