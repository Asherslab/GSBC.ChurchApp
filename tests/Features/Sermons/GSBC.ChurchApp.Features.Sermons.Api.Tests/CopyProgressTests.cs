using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class CopyProgressTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ACopyShowsAsCopiedOnceItHasLandedEvenIfAnEarlierTryFailed()
    {
        Assert.Equal((CopyStates.Waiting, 0L), Copy(new SermonRecord { Origin = SermonSources.Website, AudioBytes = 900 }));
        Assert.Equal((CopyStates.Copying, 300L), Copy(new SermonRecord { Origin = SermonSources.Website, AudioBytes = 900, CopyStartedAt = Now, CopiedBytes = 300 }));
        Assert.Equal((CopyStates.Failed, 300L), Copy(new SermonRecord { Origin = SermonSources.Website, AudioBytes = 900, CopyProblem = "The website answered 500.", CopiedBytes = 300 }));
        Assert.Equal((CopyStates.Copied, 900L), Copy(new SermonRecord { Origin = SermonSources.Website, AudioBytes = 900, AudioCopiedFrom = "https://files.example/a.mp3", CopyProblem = "The website answered 500." }));
        Assert.Null(SermonLibraryService.CopyItem(new SermonRecord { Origin = SermonSources.Website, AudioCopiedFrom = "https://files.example/a.mp3", CopyProblem = "The website answered 500." }).Problem);
    }

    private static (string, long) Copy(SermonRecord sermon)
    {
        CopyItem item = SermonLibraryService.CopyItem(sermon);
        return (item.State, item.CopiedBytes);
    }
}
