using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public interface ISignUpQuestion
{
    int Order { get; }

    bool Answered { get; }

    RenderFragment Ask(EventCallback changed);

    Task SaveAsync();
}
