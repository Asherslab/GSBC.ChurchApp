using System.Collections.Concurrent;
using System.Reflection;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Api.Tiers;

public sealed class TierCatalog
{
    private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, TierAttribute[]>> _byImplementation = new();

    public TierAttribute[] TiersOf(Type implementationType, string methodPath) =>
        _byImplementation.GetOrAdd(implementationType, Build).TryGetValue(methodPath, out TierAttribute[]? tiers)
            ? tiers
            : [];

    public static IEnumerable<(string Path, MethodInfo Method, TierAttribute[] Tiers)> Describe(Type contractType)
    {
        ServiceBinder binder = ServiceBinder.Default;
        if (!binder.IsServiceContract(contractType, out string? serviceName))
            yield break;

        foreach (MethodInfo method in contractType.GetMethods())
        {
            if (binder.IsOperationContract(method, out string? operationName))
                yield return ($"/{serviceName}/{operationName}", method, method.GetCustomAttributes<TierAttribute>().ToArray());
        }
    }

    private static IReadOnlyDictionary<string, TierAttribute[]> Build(Type implementationType) =>
        implementationType.GetInterfaces()
            .SelectMany(Describe)
            .ToDictionary(operation => operation.Path, operation => operation.Tiers);
}
