using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class MinistryLookups(ChannelBase channel)
{
    private readonly IMinistryLeadingService _leading = channel.CreateGrpcService<IMinistryLeadingService>();

    public async Task<IReadOnlyList<FoundPerson>> FindAsync(string ministryId, string query) =>
        (await _leading.FindAsync(new FindRequest { MinistryId = ministryId, Query = query })).People;

    public async Task<DeletionPreview> DeletionAsync(string ministryId) => await _leading.DeletionAsync(new MinistryRef { MinistryId = ministryId });

    public async Task<MovePreview> MovingAsync(string ministryId, string underId) =>
        await _leading.MovingAsync(new MoveRequest { MinistryId = ministryId, UnderId = underId });

    public async Task<CalendarRemoval> CalendarRemovalAsync(string ministryId, string id) =>
        await _leading.CalendarRemovalAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public static string Problem(RpcException exception) =>
        exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded
            ? "Couldn't reach the church. Check you're online and try again."
            : exception.Status.Detail.Length > 0 ? exception.Status.Detail : "That didn't work. Try again.";
}
