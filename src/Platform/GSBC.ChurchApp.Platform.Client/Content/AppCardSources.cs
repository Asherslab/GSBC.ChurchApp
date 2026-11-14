using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Content;

public sealed record AppCardChoice(AppCard Card, string Title, string Line, MediaRef? Picture = null, string? Repeats = null, string? RegularId = null);

public interface IAppCardSource
{
    IReadOnlyList<AppCardKind> Kinds { get; }

    IReadOnlyList<string> Feeds { get; }

    Task<IReadOnlyList<AppCardChoice>> ChoicesAsync(AppCardKind kind, string text);
}

public sealed class AppCardView(AppCardKind kind, [DynamicallyAccessedMembers(AppCardView.ComponentMembers)] Type component)
{
    public const string CardParameter = "Card";
    public const string EditingParameter = "Editing";

    private const DynamicallyAccessedMemberTypes ComponentMembers = DynamicallyAccessedMemberTypes.All;

    public AppCardKind Kind { get; } = kind;

    [DynamicallyAccessedMembers(ComponentMembers)]
    public Type Component { get; } = component;
}

public static class AppCardServiceCollectionExtensions
{
    public static IServiceCollection AddAppCardView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(this IServiceCollection services, AppCardKind kind) =>
        services.AddSingleton(new AppCardView(kind, typeof(TComponent)));

    public static IServiceCollection AddAppCardSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IServiceCollection services) where TSource : class, IAppCardSource =>
        services.AddScoped<IAppCardSource, TSource>();
}
