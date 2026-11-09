using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class ServiceNames(SermonsDbContext db)
{
    public async Task<ServiceList> ListAsync(CancellationToken cancellationToken)
    {
        var used = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden)
            .Select(sermon => new { Service = sermon.Service, sermon.PreachedAt }).ToListAsync(cancellationToken);
        ILookup<string, DateTime> byName = used.Where(sermon => sermon.Service.Length > 0).ToLookup(sermon => sermon.Service, sermon => sermon.PreachedAt);
        IEnumerable<ServiceChoice> standard = SermonServices.Standard.Select(name => new ServiceChoice { Name = name, Standard = true, Count = byName[name].Count() });
        IEnumerable<ServiceChoice> custom = byName.Where(group => !SermonServices.IsStandard(group.Key)).OrderByDescending(group => group.Max())
            .Select(group => new ServiceChoice { Name = group.Key, Count = group.Count() });
        return new ServiceList { Services = [.. standard, .. custom] };
    }

    public async Task<string> NameAsync(string typed, CancellationToken cancellationToken)
    {
        if (typed.Trim().Length > SermonServices.NameLimit)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Keep the service to {SermonServices.NameLimit} characters."));
        List<string> known = await db.Sermons.Select(sermon => sermon.Service).Distinct().ToListAsync(cancellationToken);
        return SermonServices.Named(typed, known);
    }
}
