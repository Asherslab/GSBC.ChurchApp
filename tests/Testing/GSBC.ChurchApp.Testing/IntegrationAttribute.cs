using Xunit.v3;

namespace GSBC.ChurchApp.Testing;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationAttribute(string covers) : Attribute, ITraitAttribute
{
    private const string Kind = "Kind";
    private const string Integration = "Integration";
    private const string Covering = "Covers";

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new(Kind, Integration), new(Covering, covers)];
}
