using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Native;

public sealed class NativeBridge(IJSRuntime js)
{
    public bool IsNative { get; private set; }

    public bool IsDevelopmentBuild { get; private set; }

    public async Task InitialiseAsync()
    {
        IsNative = await js.InvokeAsync<bool>("gsbcNative.isNative");
        IsDevelopmentBuild = IsNative && await js.InvokeAsync<bool>("gsbcNative.isDevelopmentBuild");
    }

    public ValueTask<NativeUnaryResult> UnaryAsync(string method, byte[] request, CancellationToken cancellationToken) =>
        js.InvokeAsync<NativeUnaryResult>("gsbcNative.unary", cancellationToken, method, Convert.ToBase64String(request));
}

public sealed record NativeUnaryResult(int Status, string? Message, string? Response);
