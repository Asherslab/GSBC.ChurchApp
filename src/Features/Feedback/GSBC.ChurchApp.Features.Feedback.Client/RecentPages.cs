using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public sealed class RecentPages(NavigationManager navigation) : IClientStartupTask
{
    private const int MostRemembered = 24;
    private readonly List<string> _recent = [];

    public IReadOnlyList<string> Recent => _recent;

    public IReadOnlyList<string> Trail => _recent.Take(FeedbackPages.MostInATrail).ToList();

    public string Current => FeedbackPages.PathOf(navigation.ToBaseRelativePath(navigation.Uri));

    public Task RunAsync()
    {
        Remember(navigation.Uri);
        navigation.LocationChanged += Moved;
        return Task.CompletedTask;
    }

    private void Moved(object? sender, LocationChangedEventArgs args) => Remember(args.Location);

    private void Remember(string uri)
    {
        string path = FeedbackPages.PathOf(navigation.ToBaseRelativePath(uri));
        if (_recent.FirstOrDefault() == path)
            return;
        _recent.Insert(0, path);
        if (_recent.Count > MostRemembered)
            _recent.RemoveAt(MostRemembered);
    }
}
