namespace GSBC.ChurchApp.Platform.Client.Sessions;

public interface IDeviceSession
{
    ValueTask ForgetAsync();
}

internal sealed class CookieSession : IDeviceSession
{
    public ValueTask ForgetAsync() => ValueTask.CompletedTask;
}
