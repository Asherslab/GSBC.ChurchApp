using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class PhoneLinkService(
    AccountsDbContext db, UserManager<AccountUser> users, OneTimeCodes codes, AccountsAudit audit, IHostEnvironment environment, TimeProvider time)
    : IPhoneLinkService
{
    private const int MaximumDeviceName = 200;
    private static readonly CodePurpose[] LinkCodes = [CodePurpose.LinkPhone];

    public async ValueTask<CodeReply> StartAsync(Nothing request, CallContext context = default)
    {
        RefuseOutsideDevelopment();
        ClaimsPrincipal caller = context.ServerCallContext!.GetHttpContext().User;
        Guid userId = Guid.TryParse(caller.FindFirstValue(AccountClaims.UserId), out Guid id)
            ? id
            : throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

        string code = codes.IssueShortCode(userId, CodePurpose.LinkPhone, OneTimeCodes.PhoneLinkLifetime);
        await db.SaveChangesAsync(context.CancellationToken);
        return new CodeReply { Code = code, ExpiresAt = time.GetUtcNow().UtcDateTime + OneTimeCodes.PhoneLinkLifetime };
    }

    [EnableRateLimiting(RateLimits.PublicWrites)]
    public async ValueTask<LinkedPhone> LinkAsync(LinkPhoneRequest request, CallContext context = default)
    {
        RefuseOutsideDevelopment();
        CancellationToken cancellationToken = context.CancellationToken;
        OneTimeCode code = await codes.FindTokenAsync(OneTimeCodes.NormaliseShortCode(request.Code), LinkCodes, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That code has expired or was already used. Show a new one in the admin panel."));
        AccountUser user = await users.FindByIdAsync(code.UserId.ToString())
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That account no longer exists."));
        if (user.Disabled)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "This account is disabled."));

        codes.Use(code);
        string secret = AppSessions.NewSecret();
        DateTime now = time.GetUtcNow().UtcDateTime;
        string device = request.DeviceName.Trim();
        db.Sessions.Add(new AccountSession
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Client = SessionClient.App,
            DeviceName = device.Length == 0 ? "A phone" : device[..Math.Min(device.Length, MaximumDeviceName)],
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now + AppSessions.Lifetime,
            SecretHash = AppSessions.HashOf(secret),
        });
        audit.Record(AuditActions.PhoneLinked, user.Id, remoteAddress: context.ServerCallContext is { } call ? RateLimits.ClientAddress(call.GetHttpContext()) : null);
        await db.SaveChangesAsync(cancellationToken);
        return new LinkedPhone { Secret = secret, DisplayName = user.DisplayName };
    }

    private void RefuseOutsideDevelopment()
    {
        if (!environment.IsDevelopment())
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Linking a phone with a code is only for development builds. Sign in with a passkey."));
    }
}
