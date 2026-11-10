using Grpc.Core;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public static class RosterProblems
{
    public static async Task<string?> TryAsync(Func<Task> work)
    {
        try
        {
            await work();
            return null;
        }
        catch (RpcException exception)
        {
            return exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded
                ? "Couldn't reach the church. Check you're online and try again."
                : exception.Status.Detail.Length > 0 ? exception.Status.Detail : "That didn't work. Try again.";
        }
    }
}
