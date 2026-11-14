using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Ministries;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachMinistries(IMinistryFacts ministries)
{
    private IReadOnlyList<MinistryFacts>? _running;

    public async Task<IReadOnlyList<MinistryFacts>> RunningAsync(CancellationToken cancellationToken) =>
        _running ??= (await ministries.UsingAsync(OutreachParts.Key, cancellationToken)).Where(ministry => !ministry.Archived)
            .OrderBy(ministry => ministry.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public async Task<IReadOnlyList<string?>> ScopesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<MinistryFacts> running = await RunningAsync(cancellationToken);
        return running.Count == 0 ? [null] : running.Select(ministry => (string?)ministry.Id.ToString()).ToList();
    }

    public async Task CheckAsync(string ministryId, CancellationToken cancellationToken)
    {
        if (ministryId.Length == 0)
            return;
        if (!Guid.TryParse(ministryId, out Guid id) || await ministries.FindAsync(id, cancellationToken) is not { Archived: false } ministry)
            throw new RpcException(new Status(StatusCode.NotFound, "That ministry isn't there any more."));
        if (!ministry.Uses(OutreachParts.Key))
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                $"{ministry.Name} doesn't run outreach. Turn Outreach on in its settings first."));
    }
}
