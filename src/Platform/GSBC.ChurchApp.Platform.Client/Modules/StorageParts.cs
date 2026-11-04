using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed class StoragePart([DynamicallyAccessedMembers(StoragePart.ComponentMembers)] Type component)
{
    private const DynamicallyAccessedMemberTypes ComponentMembers = DynamicallyAccessedMemberTypes.All;

    [DynamicallyAccessedMembers(ComponentMembers)]
    public Type Component { get; } = component;
}

public static class StoragePartExtensions
{
    public static IServiceCollection AddStoragePart<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(this IServiceCollection services) =>
        services.AddSingleton(new StoragePart(typeof(TComponent)));
}
