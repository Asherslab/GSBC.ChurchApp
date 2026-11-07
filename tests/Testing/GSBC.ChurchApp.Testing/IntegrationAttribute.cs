using Xunit.v3;

namespace GSBC.ChurchApp.Testing;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationAttribute(string covers) : Attribute, ITraitAttribute
{
    public const string TraitName = "Kind";
    public const string TraitValue = "Integration";

    public string Covers { get; } = covers;

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new(TraitName, TraitValue)];
}
