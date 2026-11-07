using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class SourceRuleTests
{
    [Fact]
    public void UploadsAreRefusedWhileSermonsComeFromTheWebsite()
    {
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => SermonUploads.RequireUploads(new SourceState { Source = SermonSources.Website })).StatusCode);
        SermonUploads.RequireUploads(new SourceState { Source = SermonSources.Uploads });
    }

    [Fact]
    public void NewSeriesAndSpeakersAreMadeHereOnlyWhileSermonsAreUploadedButTheWebsitesCanStillBeTidied()
    {
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => SermonLibraryService.MayMake(SermonSources.Website, isNew: true, "series")).StatusCode);
        SermonLibraryService.MayMake(SermonSources.Website, isNew: false, "series");
        SermonLibraryService.MayMake(SermonSources.Uploads, isNew: true, "speakers");
    }

    [Fact]
    public void AnUploadedSermonWaitsForItsAudioToFinishArriving()
    {
        Assert.Equal(StatusCode.InvalidArgument, Assert.Throws<RpcException>(() => SermonUploads.Arrived(null)).StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, Assert.Throws<RpcException>(() => SermonUploads.Arrived(new BlobInfo(0, "audio/mpeg"))).StatusCode);
        Assert.Equal(16_000_000, SermonUploads.Arrived(new BlobInfo(16_000_000, "audio/mpeg")).Bytes);
    }
}
