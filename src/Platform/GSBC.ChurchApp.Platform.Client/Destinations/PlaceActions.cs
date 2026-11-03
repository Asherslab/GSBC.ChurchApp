using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

public sealed record PlaceAction(string Label, string Icon, Func<Destination, string?> HrefFor, Permission? Requires = null, bool Leads = false);

public sealed class PlacePreview(Func<Destination, bool> shows, [DynamicallyAccessedMembers(PlacePreview.ComponentMembers)] Type component)
{
    private const DynamicallyAccessedMemberTypes ComponentMembers = DynamicallyAccessedMemberTypes.All;

    public Func<Destination, bool> Shows { get; } = shows;

    [DynamicallyAccessedMembers(ComponentMembers)]
    public Type Component { get; } = component;
}
