using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

[Integration("Uploading into a feature's area is allowed by that area's permission or its own check, and its files are linked only to whom the area says")]
public sealed class AreaUploadTests : MediaLibraryHost
{
    private const string Chat = "chat-7";

    [Fact]
    public async Task SomeoneWhoMayAddFilesToAMinistryUploadsThereWithoutTheMediaPermissionAndOthersCannot()
    {
        ClaimsPrincipal leader = Person(), stranger = Person();
        Access.Grant(leader, PostFiles.Node, Ministry);
        Access.Grant(leader, ReadFiles.Node, Ministry);
        BeginUploadRequest guide = new()
        {
            FileName = "helpers-guide.pdf", ContentType = "application/pdf", Bytes = SamplePdf.Length,
            PrivateTo = new MediaAudience { Permission = ReadFiles.Node, ScopeId = Ministry }, Area = MinistryFiles.Key, AreaScopeId = Ministry,
        };

        MediaRef added = (await SendAsync(leader, SamplePdf, guide)).Item!.Media!;

        Assert.True(added.IsPrivate);
        Assert.Equal(MinistryFiles.Key, await AreaFolderOfAsync(added.Id));
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.BeginUploadAsync(stranger, null, guide, Cancellation)));
        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        RpcException noCheck = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library =>
            library.FindByHashAsync(stranger, null, new FindByHashRequest { Sha256 = new string('a', 64), ContentType = "application/pdf", Area = MinistryFiles.Key, AreaScopeId = Ministry }, Cancellation)));
        Assert.Equal(StatusCode.PermissionDenied, noCheck.StatusCode);
    }

    [Fact]
    public async Task AnAreaWithItsOwnCheckDecidesWhoUploadsAndWhoGetsALinkEvenOverTheAudiencePermission()
    {
        ClaimsPrincipal member = Person(), outsider = Person();
        Members.Add(member, Chat);
        Access.Grant(outsider, ReadFiles.Node, Chat);
        BeginUploadRequest photo = new() { FileName = "camp.jpg", ContentType = "image/jpeg", Bytes = Jpeg(seed: 31).Length, Area = Chats.Key, AreaScopeId = Chat };

        MediaRef shared = (await SendAsync(member, Jpeg(seed: 31), photo)).Item!.Media!;

        Assert.True(shared.IsPrivate);
        MediaLink link = await LibraryCallAsync(library => library.GetLinkAsync(member, new LinkRequest { Id = shared.Id }, Cancellation));
        Assert.NotEmpty(link.Url);
        RpcException notAMember = await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.GetLinkAsync(outsider, new LinkRequest { Id = shared.Id }, Cancellation)));
        Assert.Equal(StatusCode.PermissionDenied, notAMember.StatusCode);
        await Assert.ThrowsAsync<RpcException>(() => LibraryCallAsync(library => library.BeginUploadAsync(outsider, null, photo, Cancellation)));
    }

    private async Task<string?> AreaFolderOfAsync(string mediaId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        MediaDbContext db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        Guid id = Guid.Parse(mediaId);
        return await db.Items.Where(item => item.Id == id)
            .Join(db.Folders, item => item.FolderId, folder => folder.Id, (_, folder) => folder.SystemArea)
            .SingleAsync(Cancellation);
    }

    private static readonly byte[] SamplePdf = "%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n"u8.ToArray();
}
