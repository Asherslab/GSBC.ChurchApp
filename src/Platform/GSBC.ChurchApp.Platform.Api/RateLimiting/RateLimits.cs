using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Api.RateLimiting;

public static class RateLimits
{
    public const string PublicWrites = "public-writes";

    public static IServiceCollection AddPlatformRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PublicWrites, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientAddress(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });

    public static string ClientAddress(HttpContext context) =>
        context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
        ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
