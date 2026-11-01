using System.Security.Claims;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

public static class PasskeyEndpoints
{
    public const string RateLimitPolicy = "passkeys";
    public const string Prefix = "/accounts/passkeys";
    private const int MaximumPasskeyNameLength = 60;
    private static readonly CodePurpose[] Ticket = [CodePurpose.RegisterPasskey];

    public sealed record CreationOptionsRequest(string? Ticket);

    public sealed record RegisterRequest(string Credential, string? Ticket, string? Name);

    public sealed record SignInRequest(string Credential);

    public static IEndpointRouteBuilder MapPasskeyCeremonies(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup(Prefix)
            .AddEndpointFilter(RequireAllowedOrigin)
            .RequireRateLimiting(RateLimitPolicy);

        group.MapPost("/creation-options", CreationOptionsAsync);
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/request-options", RequestOptionsAsync);
        group.MapPost("/sign-in", SignInAsync);
        return endpoints;
    }

    private static async Task<IResult> CreationOptionsAsync(
        CreationOptionsRequest request, HttpContext http, SignInManager<AccountUser> signIn, UserManager<AccountUser> users,
        OneTimeCodes codes, CancellationToken cancellationToken)
    {
        AccountUser? user = request.Ticket is { Length: > 0 } ticket
            ? await UserForTicketAsync(ticket, users, codes, cancellationToken)
            : await users.GetUserAsync(http.User);
        if (user is null)
            return Results.Problem("Sign in first, or start again from the link you were sent.", statusCode: StatusCodes.Status401Unauthorized);
        if (user.Disabled)
            return Results.Problem("This account is disabled.", statusCode: StatusCodes.Status403Forbidden);

        string options = await signIn.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id.ToString(),
            Name = user.Email ?? user.UserName ?? user.Id.ToString(),
            DisplayName = user.DisplayName,
        });
        return Results.Content(options, "application/json");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, HttpContext http, SignInManager<AccountUser> signIn, UserManager<AccountUser> users,
        OneTimeCodes codes, AccountsAudit audit, AccountsDbContext db, CancellationToken cancellationToken)
    {
        PasskeyAttestationResult attestation = await signIn.PerformPasskeyAttestationAsync(request.Credential);
        if (!attestation.Succeeded)
            return Results.Problem(attestation.Failure.Message, statusCode: StatusCodes.Status400BadRequest);

        AccountUser? user = await users.FindByIdAsync(attestation.UserEntity.Id);
        if (user is null || user.Disabled)
            return Results.Problem("That account can't add a passkey.", statusCode: StatusCodes.Status403Forbidden);

        bool fromTicket = request.Ticket is { Length: > 0 };
        if (fromTicket)
        {
            OneTimeCode? code = await codes.FindTokenAsync(request.Ticket!, Ticket, cancellationToken);
            if (code is null || code.UserId != user.Id)
                return Results.Problem("That sign-up has expired. Start again.", statusCode: StatusCodes.Status401Unauthorized);
            codes.Use(code);
        }
        else if (http.User.FindFirstValue(ClaimTypes.NameIdentifier) != user.Id.ToString())
        {
            return Results.Problem("Sign in first.", statusCode: StatusCodes.Status401Unauthorized);
        }

        UserPasskeyInfo passkey = attestation.Passkey;
        passkey.Name = PasskeyName(request.Name);
        IdentityResult added = await users.AddOrUpdatePasskeyAsync(user, passkey);
        if (!added.Succeeded)
            return Results.Problem(string.Join(" ", added.Errors.Select(error => error.Description)), statusCode: StatusCodes.Status400BadRequest);

        string? address = ClientAddress(http);
        audit.Record(AuditActions.PasskeyAdded, user.Id, remoteAddress: address);
        if (fromTicket)
        {
            await signIn.SignInAsync(user, isPersistent: true);
            audit.Record(AuditActions.SignedIn, user.Id, remoteAddress: address);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { signedIn = true });
    }

    private static async Task<IResult> RequestOptionsAsync(SignInManager<AccountUser> signIn) =>
        Results.Content(await signIn.MakePasskeyRequestOptionsAsync(null), "application/json");

    private static async Task<IResult> SignInAsync(
        SignInRequest request, HttpContext http, SignInManager<AccountUser> signIn, UserManager<AccountUser> users,
        AccountsAudit audit, AccountsDbContext db, CancellationToken cancellationToken)
    {
        PasskeyAssertionResult<AccountUser> assertion = await signIn.PerformPasskeyAssertionAsync(request.Credential);
        if (!assertion.Succeeded)
            return Results.Problem("That passkey didn't work. Try again.", statusCode: StatusCodes.Status401Unauthorized);
        if (!await signIn.CanSignInAsync(assertion.User))
            return Results.Problem("This account is disabled.", statusCode: StatusCodes.Status403Forbidden);

        await users.AddOrUpdatePasskeyAsync(assertion.User, assertion.Passkey);
        await signIn.SignInAsync(assertion.User, isPersistent: true);
        audit.Record(AuditActions.SignedIn, assertion.User.Id, remoteAddress: ClientAddress(http));
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { signedIn = true });
    }

    private static async Task<AccountUser?> UserForTicketAsync(
        string ticket, UserManager<AccountUser> users, OneTimeCodes codes, CancellationToken cancellationToken) =>
        await codes.FindTokenAsync(ticket, Ticket, cancellationToken) is { } code
            ? await users.FindByIdAsync(code.UserId.ToString())
            : null;

    private static async ValueTask<object?> RequireAllowedOrigin(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        string[] allowed = http.RequestServices.GetRequiredService<IOptions<AccountsGatewayOptions>>().Value.AllowedOrigins;
        string? origin = http.Request.Headers.Origin;
        return origin is not null && allowed.Contains(origin, StringComparer.OrdinalIgnoreCase)
            ? await next(context)
            : Results.Problem("This request didn't come from the Good Shepherd site.", statusCode: StatusCodes.Status403Forbidden);
    }

    private static string PasskeyName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Passkey" : name.Trim()[..Math.Min(name.Trim().Length, MaximumPasskeyNameLength)];

    private static string? ClientAddress(HttpContext http) =>
        http.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? http.Connection.RemoteIpAddress?.ToString();
}
