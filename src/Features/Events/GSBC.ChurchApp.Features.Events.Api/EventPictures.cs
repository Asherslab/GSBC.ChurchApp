using System.Text.RegularExpressions;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed partial class EventPictures(EventsDbContext db, IMediaReferences references, EventsPublisher publisher, ILogger<EventPictures> logger) : IMediaHolder
{
    public bool Holds(string owner) => OwnerPattern().IsMatch(owner);

    public async Task RefreshAsync(string owner, CancellationToken cancellationToken)
    {
        if (OwnerPattern().Match(owner) is not { Success: true } match)
            return;
        Guid id = Guid.Parse(match.Groups["id"].Value);
        if (match.Groups["date"].Success && EventDates.Parse(match.Groups["date"].Value) is { } date)
        {
            if (await db.Specials.SingleOrDefaultAsync(special => special.RegularEventId == id && special.Date == date, cancellationToken) is { } special)
                special.Fields = await CurrentAsync(special.Fields, cancellationToken);
        }
        else if (match.Groups["kind"].Value == "event" && await db.Events.FindAsync([id], cancellationToken) is { } record)
            record.Fields = await CurrentAsync(record.Fields, cancellationToken);
        else if (match.Groups["kind"].Value == "regular" && await db.RegularEvents.FindAsync([id], cancellationToken) is { } regular)
            regular.Fields = await CurrentAsync(regular.Fields, cancellationToken);
        else
            return;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    private async Task<EventFields> CurrentAsync(EventFields fields, CancellationToken cancellationToken)
    {
        List<MediaRef> held = new[] { fields.Picture }.OfType<MediaRef>().Concat(fields.Description?.Media ?? []).ToList();
        if (held.Count == 0)
            return fields;
        try
        {
            IReadOnlyList<MediaRef> current = await references.VerifyAsync(held, cancellationToken);
            int pictures = fields.Picture is null ? 0 : 1;
            return new EventFields
            {
                Title = fields.Title, Label = fields.Label, Place = fields.Place, Contacts = fields.Contacts, RegisterLink = fields.RegisterLink, Links = fields.Links,
                Picture = fields.Picture is null ? null : current[0],
                Description = fields.Description is null ? null : new RichText { Markdown = fields.Description.Markdown, Media = current.Skip(pictures).ToList() },
            };
        }
        catch (RpcException exception)
        {
            logger.LogWarning(exception, "Media no longer has an event's picture; keeping what was saved");
            return fields;
        }
    }

    [GeneratedRegex("^events/(?<kind>event|regular)/(?<id>[0-9a-f-]{36})(/(?<date>\\d{4}-\\d{2}-\\d{2}))?$")]
    private static partial Regex OwnerPattern();
}
