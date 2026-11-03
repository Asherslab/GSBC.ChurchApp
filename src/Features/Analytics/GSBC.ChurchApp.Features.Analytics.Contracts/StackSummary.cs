using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public static partial class StackSummary
{
    public const int MaximumFrames = 12;
    public const int MaximumFrameLength = 300;
    private const string OurNamespace = "GSBC.";
    private const int FramesWhenNoneAreOurs = 3;

    public static string TypeOf(Exception exception) => Innermost(exception).GetType().FullName ?? Innermost(exception).GetType().Name;

    public static List<string> FramesOf(Exception exception) => FramesOf(Innermost(exception).StackTrace);

    public static List<string> FramesOf(string? stackTrace)
    {
        List<string> frames = (stackTrace ?? "")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("at ", StringComparison.Ordinal))
            .Select(Frame)
            .ToList();
        List<string> ours = frames.Where(frame => frame.StartsWith(OurNamespace, StringComparison.Ordinal)).ToList();
        return (ours.Count > 0 ? ours : frames.Take(FramesWhenNoneAreOurs)).Take(MaximumFrames).ToList();
    }

    private static Exception Innermost(Exception exception) => exception switch
    {
        AggregateException { InnerExceptions.Count: 1 } aggregate => Innermost(aggregate.InnerExceptions[0]),
        System.Reflection.TargetInvocationException { InnerException: { } inner } => Innermost(inner),
        _ => exception,
    };

    private static string Frame(string line)
    {
        string frame = line[3..];
        int location = frame.IndexOf(" in ", StringComparison.Ordinal);
        if (location >= 0)
            frame = frame[..location];
        int arguments = frame.IndexOf('(');
        if (arguments >= 0)
            frame = frame[..arguments];
        frame = AsyncStateMachine().Replace(frame, "$1");
        frame = GeneratedNumbers().Replace(frame, "$1");
        return frame.Length > MaximumFrameLength ? frame[..MaximumFrameLength] : frame;
    }

    [GeneratedRegex(@"<(\w+)>d__\d+\.MoveNext")]
    private static partial Regex AsyncStateMachine();

    [GeneratedRegex(@"(DisplayClass|b__|d__)\d+(_\d+)*")]
    private static partial Regex GeneratedNumbers();
}
