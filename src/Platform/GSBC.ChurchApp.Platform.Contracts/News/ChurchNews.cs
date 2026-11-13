using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.News;

public static class ChurchNews
{
    public const string WriteNode = "news.write";
    public const string PublishNode = "news.publish";

    public const string FromMinistries = "ministries";

    public static readonly Permission Publishing = new(PublishNode, ScopeKind.Global, "Publish church news");
}
