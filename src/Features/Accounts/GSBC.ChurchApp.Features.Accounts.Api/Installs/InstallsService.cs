using System.Security.Cryptography;
using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Accounts.Api.Installs;

internal sealed class InstallsService(AccountsDbContext db, AppAttest appAttest, IPhoneRoles phoneRoles, TimeProvider time) : IInstallsService
{
    private const int ChallengeLength = 32;
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public async ValueTask<AttestationChallenge> BeginAttestationAsync(BeginAttestationRequest request, CallContext context = default)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        byte[] challenge = RandomNumberGenerator.GetBytes(ChallengeLength);
        await db.InstallChallenges.Where(old => old.ExpiresAt < now).ExecuteDeleteAsync(context.CancellationToken);
        db.InstallChallenges.Add(new InstallChallenge { Challenge = challenge, ExpiresAt = now + ChallengeLifetime });
        await db.SaveChangesAsync(context.CancellationToken);
        return new AttestationChallenge { Challenge = challenge };
    }

    public async ValueTask<Done> AttestAsync(AttestRequest request, CallContext context = default)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        int used = await db.InstallChallenges
            .Where(challenge => challenge.Challenge == request.Challenge && challenge.ExpiresAt >= now)
            .ExecuteDeleteAsync(context.CancellationToken);
        if (used == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "That challenge has expired. Start again."));

        AttestedKey key = appAttest.VerifyAttestation(request.KeyId, request.Challenge, request.Attestation, now)
            ?? throw new RpcException(new Status(StatusCode.PermissionDenied, "This isn't a genuine copy of the app."));

        if (!await db.Installs.AnyAsync(install => install.KeyId == request.KeyId, context.CancellationToken))
        {
            db.Installs.Add(new AppInstall
            {
                Id = Guid.CreateVersion7(),
                KeyId = request.KeyId,
                PublicKey = key.PublicKey,
                AppId = key.AppId,
                Environment = key.Environment,
                AttestedAt = now,
                LastSeenAt = now,
            });
            await db.SaveChangesAsync(context.CancellationToken);
        }
        return new Done();
    }

    public async ValueTask<Done> ClaimAsync(Nothing request, CallContext context = default)
    {
        Guid userId = Caller.UserId(context.ServerCallContext!.GetHttpContext().User)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
        await phoneRoles.MoveToPersonAsync(CallingInstall.Of(context), userId, context.CancellationToken);
        return new Done();
    }
}
