using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[Service("gsbc.accounts.Installs")]
public interface IInstallsService
{
    [Public]
    ValueTask<AttestationChallenge> BeginAttestationAsync(BeginAttestationRequest request, CallContext context = default);

    [Public]
    ValueTask<Done> AttestAsync(AttestRequest request, CallContext context = default);

    [Public]
    ValueTask<ProofMethods> ListProofMethodsAsync(Nothing request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> ClaimAsync(Nothing request, CallContext context = default);
}
