using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class StackSummaryTests
{
    [Fact]
    public void FramesKeepOnlyOurMethodNamesWithNoPathsArgumentsOrBuildSpecificNumbers()
    {
        const string trace = """
               at System.ThrowHelper.ThrowNullReference()
               at GSBC.ChurchApp.Features.Sermons.Client.Player.<StartAsync>d__12.MoveNext() in /Users/someone/src/Player.cs:line 40
            --- End of stack trace from previous location ---
               at GSBC.ChurchApp.Features.Sermons.Client.SermonPage.<>c__DisplayClass7_0.<OnPlay>b__0(String sermonId, Int32 offset)
               at Microsoft.AspNetCore.Components.ComponentBase.CallStateHasChangedOnAsyncCompletion(Task task)
            """;

        List<string> frames = StackSummary.FramesOf(trace);

        Assert.Equal(
        [
            "GSBC.ChurchApp.Features.Sermons.Client.Player.StartAsync",
            "GSBC.ChurchApp.Features.Sermons.Client.SermonPage.<>c__DisplayClass.<OnPlay>b__",
        ], frames);
    }
}
