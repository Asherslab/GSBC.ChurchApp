using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

public static class AccountsGatewayExtensions
{
    private const string SessionCookie = "__Host-gsbc-session";
    private const string EitherSession = "Session";

    public static IServiceCollection AddAccountsGateway(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AccountsGatewayOptions>(configuration.GetSection(AccountsGatewayOptions.Section));
        services.AddAccountsData();
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DatabaseTicketStore>();
        services.AddSingleton<ApiTokenIssuer>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultAuthenticateScheme = EitherSession;
            })
            .AddPolicyScheme(EitherSession, "The browser's cookie or the app's session", policy => policy.ForwardDefaultSelector = context =>
                context.Request.Headers.ContainsKey(AppSessions.Header) ? AppSessionHandler.SchemeName : IdentityConstants.ApplicationScheme)
            .AddScheme<AuthenticationSchemeOptions, AppSessionHandler>(AppSessionHandler.SchemeName, null)
            .AddIdentityCookies();
        services
            .AddIdentityCore<AccountUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AccountsDbContext>()
            .AddClaimsPrincipalFactory<AccountClaimsPrincipalFactory>()
            .AddSignInManager<AccountSignInManager>();

        services.AddOptions<IdentityPasskeyOptions>().Configure<IOptions<AccountsGatewayOptions>>((passkeys, gateway) =>
        {
            passkeys.ServerDomain = gateway.Value.RelyingPartyId;
            passkeys.UserVerificationRequirement = "required";
            passkeys.ResidentKeyRequirement = "required";
            passkeys.ValidateOrigin = context => ValueTask.FromResult(
                gateway.Value.AllowedOrigins.Contains(context.Origin, StringComparer.OrdinalIgnoreCase));
        });

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme).Configure<DatabaseTicketStore>((cookie, store) =>
        {
            cookie.Cookie.Name = SessionCookie;
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            cookie.Cookie.SameSite = SameSiteMode.Strict;
            cookie.ExpireTimeSpan = DatabaseTicketStore.BrowserLifetime;
            cookie.SlidingExpiration = true;
            cookie.SessionStore = store;
            cookie.Events.OnRedirectToLogin = context => Respond(context.Response, StatusCodes.Status401Unauthorized);
            cookie.Events.OnRedirectToAccessDenied = context => Respond(context.Response, StatusCodes.Status403Forbidden);
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PasskeyEndpoints.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }

    public static IReverseProxyBuilder AddApiTokens(this IReverseProxyBuilder proxy) =>
        proxy.AddTransforms(transforms => transforms.AddRequestTransform(transform =>
        {
            transform.ProxyRequest.Headers.Authorization = null;
            transform.ProxyRequest.Headers.Remove(AppSessions.Header);
            string? token = transform.HttpContext.RequestServices.GetRequiredService<ApiTokenIssuer>().Issue(transform.HttpContext.User);
            if (token is not null)
                transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return ValueTask.CompletedTask;
        }));

    private static Task Respond(HttpResponse response, int status)
    {
        response.StatusCode = status;
        return Task.CompletedTask;
    }
}
