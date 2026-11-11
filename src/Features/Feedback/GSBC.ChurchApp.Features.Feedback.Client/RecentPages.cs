using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public sealed class RecentPages(NavigationManager navigation) : IClientStartupTask
{
    private static readonly string[] NotWhereTheyWere = ["more", "feedback"];

    public string Last { get; private set; } = "";

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
        if (!NotWhereTheyWere.Contains(path.Trim('/').Split('/')[0], StringComparer.OrdinalIgnoreCase))
            Last = path;
    }
}
