using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryPostMedia(IMediaReferences references, IMediaSystemFolders folders, IMediaAudiences audiences)
{
    public static readonly MediaSystemArea Area = new(MinistryFileArea.PostsKey, "Ministry posts",
        "Made by the app: the pictures, video, audio and files in ministries' news posts", MinistriesPermissions.TeamView, StaffDeleteUnused: true,
        Uploaders: MinistriesPermissions.Post, Files: PostMedia.Types);

    public async Task<RichText> ResolvedAsync(RichText content, IReadOnlyList<string> uploaded, CancellationToken cancellationToken) =>
        uploaded.Count == 0
            ? content
            : UploadedMedia.Resolve(content, await references.VerifyAsync(uploaded.Select(id => new MediaRef { Id = id }).ToList(), cancellationToken));

    public async Task<RichText> VerifiedAsync(RichText content, string audience, CancellationToken cancellationToken)
    {
        RichText used = RichTextRules.WithOnlyUsedMedia(content);
        if (used.Media.Count == 0)
            return new RichText { Markdown = content.Markdown.Trim() };
        IReadOnlyList<MediaRef> verified = await references.VerifyAsync(used.Media, cancellationToken);
        if (audience == Audiences.Everyone && verified.FirstOrDefault(media => media.IsPrivate) is { } kept)
            throw MinistriesService.Invalid($"'{kept.FileName}' was added for fewer people, so it can't go in a post for everyone. Add it again.");
        return new RichText { Markdown = content.Markdown.Trim(), Media = [.. verified] };
    }

    public async Task UseAsync(PostRecord post, RichText content, MinistryRecord ministry, CancellationToken cancellationToken)
    {
        await references.UseAsync(Owner(post.Id), content.Media, cancellationToken);
        if (content.Media.Count == 0)
            return;
        await folders.FileAsync(Area, ministry.Id.ToString(), ministry.Name, content.Media, cancellationToken);
        foreach (MediaRef media in content.Media.Where(media => media.IsPrivate))
            await audiences.ShareAsync(Area, media.Id, Owner(post.Id), Audiences.NodeOf(post.Audience), ministry.Id.ToString(), cancellationToken);
    }

    public Task ReleaseAsync(Guid postId, CancellationToken cancellationToken) => references.UseAsync(Owner(postId), [], cancellationToken);

    private static string Owner(Guid postId) => $"ministries.post/{postId}";
}
