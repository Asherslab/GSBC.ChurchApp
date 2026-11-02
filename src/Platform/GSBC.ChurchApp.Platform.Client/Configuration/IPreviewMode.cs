namespace GSBC.ChurchApp.Platform.Client.Configuration;

public enum PreviewViewer
{
    Me,
    Visitor,
    Member,
}

public sealed record PreviewChoice(string? DraftId, string Name, PreviewViewer As = PreviewViewer.Me, DateTime? At = null)
{
    public bool ChangesAnything => DraftId is not null || As != PreviewViewer.Me || At is not null;
}

public interface IPreviewMode
{
    PreviewChoice? Current { get; }

    event Action? Changed;

    Task LoadAsync();

    Task ChooseAsync(PreviewChoice? choice);
}
