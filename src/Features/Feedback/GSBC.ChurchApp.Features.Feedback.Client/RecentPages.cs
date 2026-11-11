using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public sealed class RecentPages(NavigationManager navigation) : IClientStartupTask
{
    private const int MostRemembered = 8;
    private static readonly string[] NotWhereTheyWere = ["more", "feedback"];
    private readonly List<string> _recent = [];

    public string Last { get; private set; } = "";

    public IReadOnlyList<string> Recent => _recent;

    public Task RunAsync()
    {
        Remember(navigation.Uri);
        navigation.LocationChanged += Moved;
        return Task.CompletedTask;
    }

    private void Moved(object? sender, LocationChangedEventArgs args) => Remember(args.Location);

    private void Remember(string uri)
    {
        string path = "/" + navigation.ToBaseRelativePath(uri).Split('?', '#')[0];
        if (_recent.FirstOrDefault() != path)
        {
            _recent.Insert(0, path);
            if (_recent.Count > MostRemembered)
                _recent.RemoveAt(MostRemembered);
        }
        if (!NotWhereTheyWere.Contains(path.Trim('/').Split('/')[0], StringComparer.OrdinalIgnoreCase))
            Last = path;
    }
}
