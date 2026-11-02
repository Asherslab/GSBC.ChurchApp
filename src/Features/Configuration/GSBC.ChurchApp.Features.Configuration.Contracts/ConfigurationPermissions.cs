using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class ConfigurationPermissions
{
    public const string DraftReviewerRole = "draft-reviewer";

    public static readonly Permission PreviewDraft =
        new("config.drafts.preview", ScopeKind.Draft, "Preview a draft on their phone; given everywhere, see every draft");
}
