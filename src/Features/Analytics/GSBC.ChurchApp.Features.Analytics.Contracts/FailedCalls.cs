using Grpc.Core;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public static class FailedCalls
{
    public static readonly IReadOnlyList<int> Failures =
    [
        (int)StatusCode.Unknown, (int)StatusCode.DeadlineExceeded, (int)StatusCode.ResourceExhausted, (int)StatusCode.Aborted,
        (int)StatusCode.Unimplemented, (int)StatusCode.Internal, (int)StatusCode.Unavailable, (int)StatusCode.DataLoss,
    ];

    public static bool IsAFailure(StatusCode status) => Failures.Contains((int)status);
}
