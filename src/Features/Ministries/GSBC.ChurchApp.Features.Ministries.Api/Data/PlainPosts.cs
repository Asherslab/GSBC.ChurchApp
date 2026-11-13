namespace GSBC.ChurchApp.Features.Ministries.Api.Data;

internal static class PlainPosts
{
    public const string ToContent = """
        UPDATE ministries.posts
        SET content = jsonb_build_object('markdown',
            regexp_replace(regexp_replace(body, '([\\*_\[\]()!#>`-])', '\\\1', 'g'), '(^|\n)([ \t]*)(\d+)\. ', '\1\2\3\\. ', 'g'))
        WHERE content IS NULL;
        """;
}
