namespace GSBC.ChurchApp.Hosts.Website;

public static class Caching
{
    public const string Page = "public, max-age=60";
    public const string Revalidate = "no-cache";
    public const string NotStored = "no-store";

    public static string For(bool development, int status, string? fromStaticFiles) =>
        development ? NotStored
        : !string.IsNullOrEmpty(fromStaticFiles) ? fromStaticFiles
        : status == StatusCodes.Status200OK ? Page
        : Revalidate;

    public static string FramedBy(string ancestors) =>
        $"frame-ancestors {(string.IsNullOrWhiteSpace(ancestors) ? "'none'" : ancestors.Trim())}";

    public static IApplicationBuilder UseWebsiteHeaders(this IApplicationBuilder app, bool development, WebsiteOptions options) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                HttpResponse response = context.Response;
                response.Headers.CacheControl = For(development, response.StatusCode, response.Headers.CacheControl);
                response.Headers.ContentSecurityPolicy = FramedBy(options.FrameAncestors);
                response.Headers.XContentTypeOptions = "nosniff";
                response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });
            return next(context);
        });
}
