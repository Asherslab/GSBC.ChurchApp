using Grpc.Core;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class OutreachLimits
{
    public static readonly TimeSpan Day = TimeSpan.FromDays(1);
    public const int WalksPerDay = 20;
    public const int NoJunkMailPerDay = 150;
    public const int DoorsPerDay = 400;
    public const int ReportsPerDay = 20;
    public const int ClaimsPerDay = 20;

    public static void Check(int doneToday, int allowed, string what)
    {
        if (doneToday >= allowed)
            throw new RpcException(new Status(StatusCode.ResourceExhausted, $"This phone has sent {allowed} {what} today. The rest go tomorrow."));
    }
}
