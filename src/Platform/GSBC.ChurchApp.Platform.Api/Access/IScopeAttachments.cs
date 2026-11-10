using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IScopeAttachments
{
    ScopeKind AttachedTo { get; }

    ValueTask<IReadOnlyList<Scope>> AttachedAsync(string objectId, CancellationToken cancellationToken);
}
