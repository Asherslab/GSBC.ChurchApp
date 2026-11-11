using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Uploads;

public interface IFileShelf
{
    bool IsAvailable { get; }

    ValueTask<StagedFile?> KeepAsync(ElementReference input, string contentType);

    ValueTask ForgetAsync(string token);
}

public sealed class NoFileShelf : IFileShelf
{
    public bool IsAvailable => false;

    public ValueTask<StagedFile?> KeepAsync(ElementReference input, string contentType) => ValueTask.FromResult<StagedFile?>(null);

    public ValueTask ForgetAsync(string token) => ValueTask.CompletedTask;
}
