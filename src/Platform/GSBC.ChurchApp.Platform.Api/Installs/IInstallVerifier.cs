namespace GSBC.ChurchApp.Platform.Api.Installs;

public sealed record InstallProof(string KeyId, byte[] Assertion, byte[] ClientData);

public interface IInstallVerifier
{
    ValueTask<Guid?> VerifyAsync(InstallProof proof, CancellationToken cancellationToken);
}

internal sealed class NoInstallVerifier : IInstallVerifier
{
    public ValueTask<Guid?> VerifyAsync(InstallProof proof, CancellationToken cancellationToken) => ValueTask.FromResult<Guid?>(null);
}
