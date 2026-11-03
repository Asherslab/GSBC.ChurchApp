using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Platform.Api.Destinations;

public interface IDestinationSource
{
    DestinationKind Kind { get; }

    Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken);

    Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken);

    Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken);
}

public sealed record FoundPlace(string Id, string Title, string Where, Audience Audience, string? AudienceScope = null, DateTime? When = null);
