namespace GSBC.ChurchApp.Features.Accounts.Client;

public interface ISignInCodes
{
    bool IsAvailable { get; }

    ValueTask<CeremonyResult> SignInAsync(string email, string code);
}

public sealed class NoSignInCodes : ISignInCodes
{
    public bool IsAvailable => false;

    public ValueTask<CeremonyResult> SignInAsync(string email, string code) =>
        ValueTask.FromResult(new CeremonyResult(false, "Sign-in codes work in the app."));
}
