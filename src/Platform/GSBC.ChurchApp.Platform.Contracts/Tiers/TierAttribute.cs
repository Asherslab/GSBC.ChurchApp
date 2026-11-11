namespace GSBC.ChurchApp.Platform.Contracts.Tiers;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public abstract class TierAttribute : Attribute;

public sealed class PublicAttribute : TierAttribute;

public sealed class AppInstallAttribute : TierAttribute;

public sealed class SignedInAttribute : TierAttribute;

public sealed class AppInstallOrSignedInAttribute : TierAttribute;

public sealed class RequiresAttribute(string node) : TierAttribute
{
    public string Node { get; } = node;
}
