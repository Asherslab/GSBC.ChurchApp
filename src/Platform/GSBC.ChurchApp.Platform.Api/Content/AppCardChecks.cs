using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Api.Content;

public sealed class AppCardChecks(IEnumerable<IAppCardSource> sources, PlaceAccess access)
{
    public async Task<string?> ProblemAsync(ClaimsPrincipal writer, RichText content, CancellationToken cancellationToken)
    {
        foreach (AppCard card in RichTextParser.Parse(content.Markdown).AppCards.DistinctBy(card => (card.Kind, card.Id, card.Shows)))
        {
            FoundPlace? place = sources.FirstOrDefault(source => source.Kinds.Contains(card.Kind)) is { } source ? await source.FindAsync(card, cancellationToken) : null;
            if (place is null || !await access.MayOpenAsync(writer, place.Audience, place.AudienceScope, cancellationToken))
                return $"A card in it is for {AppCards.WithArticle(card.Kind)} that isn't in the app any more. Take it out or choose another.";
        }
        return null;
    }
}
