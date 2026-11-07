using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class SourceRuleTests
{
    [Fact]
    public void UploadsAndNewSeriesAndSpeakersAreMadeHereOnlyWhileSermonsAreUploaded()
    {
        SourceState website = new() { Source = SermonSources.Website }, uploads = new() { Source = SermonSources.Uploads };

        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => SermonUploads.RequireUploads(website)).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => SermonLibraryService.RequireOurs(website, "series")).StatusCode);
        SermonUploads.RequireUploads(uploads);
        SermonLibraryService.RequireOurs(uploads, "series");
    }
}
