using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

public static partial class AppAssociation
{
    public const string Section = "Apple";
    public const string RequestPath = "/.well-known/apple-app-site-association";
    public const string DefaultBundleId = "au.com.baptist.app";
    private const string GeneratedDirectory = "generated";
    private static readonly string[] LinksIntoTheApp = ["/welcome", "/welcome/*"];

    public static string Json(string teamId, string bundleId)
    {
        if (!TeamIdFormat().IsMatch(teamId))
            throw new InvalidOperationException($"'{teamId}' isn't an Apple team id (ten capital letters and digits).");

        string app = $"{teamId}.{bundleId}";
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["applinks"] = new Dictionary<string, object>
            {
                ["details"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["appIDs"] = new[] { app },
                        ["components"] = LinksIntoTheApp.Select(path => new Dictionary<string, string> { ["/"] = path }).ToArray(),
                    },
                },
            },
            ["webcredentials"] = new Dictionary<string, object> { ["apps"] = new[] { app } },
        });
    }

    public static IApplicationBuilder UseAppAssociation(this IApplicationBuilder app)
    {
        IConfigurationSection apple = app.ApplicationServices.GetRequiredService<IConfiguration>().GetSection(Section);
        if (apple["TeamId"] is not { Length: > 0 } teamId)
            return app;

        string root = Path.Combine(app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath, GeneratedDirectory);
        string file = Path.Combine(root, RequestPath.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, Json(teamId.Trim(), apple["BundleId"] is { Length: > 0 } bundleId ? bundleId : DefaultBundleId));

        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root, ExclusionFilters.None),
            ContentTypeProvider = new AssociationContentType(),
        });
    }

    [GeneratedRegex("^[A-Z0-9]{10}$")]
    private static partial Regex TeamIdFormat();

    private sealed class AssociationContentType : IContentTypeProvider
    {
        public bool TryGetContentType(string subpath, [MaybeNullWhen(false)] out string contentType)
        {
            contentType = subpath == RequestPath ? "application/json" : null;
            return contentType is not null;
        }
    }
}
