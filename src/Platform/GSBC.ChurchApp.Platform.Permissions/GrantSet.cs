namespace GSBC.ChurchApp.Platform.Permissions;

public sealed class GrantSet(IReadOnlyList<Grant> grants)
{
    public static readonly GrantSet Empty = new([]);

    public IReadOnlyList<Grant> Grants { get; } = grants;

    public bool IsAllowed(Permission permission, string? objectId = null)
    {
        int bestSpecificity = -1;
        bool allowed = false;

        foreach (Grant grant in Grants)
        {
            if (!grant.AppliesTo(permission, objectId) || !grant.Pattern.Matches(permission.Node))
                continue;

            int specificity = grant.Pattern.Specificity;
            if (specificity > bestSpecificity)
            {
                bestSpecificity = specificity;
                allowed = !grant.Pattern.IsDeny;
            }
            else if (specificity == bestSpecificity && grant.Pattern.IsDeny)
            {
                allowed = false;
            }
        }

        return allowed;
    }
}
