using System.Globalization;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public sealed record Timed(EventFields Fields, int? StartMinute, int? EndMinute);

public static class FieldValues
{
    private const int MostNotes = 2;

    public static EventFields Overlay(EventFields regular, EventFields special, IReadOnlyCollection<string> changed) => new()
    {
        Title = changed.Contains(EventFieldKeys.Title) ? special.Title : regular.Title,
        Label = changed.Contains(EventFieldKeys.Label) ? special.Label : regular.Label,
        Picture = changed.Contains(EventFieldKeys.Picture) ? special.Picture : regular.Picture,
        Description = changed.Contains(EventFieldKeys.Description) ? special.Description : regular.Description,
        Place = changed.Contains(EventFieldKeys.Place) ? special.Place : regular.Place,
        Contacts = changed.Contains(EventFieldKeys.Contacts) ? special.Contacts : regular.Contacts,
        RegisterLink = changed.Contains(EventFieldKeys.Register) ? special.RegisterLink : regular.RegisterLink,
        Links = changed.Contains(EventFieldKeys.Links) ? special.Links : regular.Links,
    };

    public static List<string> Differences(Timed before, Timed after) =>
        EventFieldKeys.All.Where(key => Value(before, key) != Value(after, key)).ToList();

    public static bool IsSet(EventFields fields, string key) => key switch
    {
        EventFieldKeys.Place => fields.Place is { } place && (place.PlaceId is { Length: > 0 } || place.Name.Length > 0 || place.Address.Length > 0),
        EventFieldKeys.Contacts => fields.Contacts.Count > 0,
        EventFieldKeys.Description => fields.Description is { IsEmpty: false },
        EventFieldKeys.Picture => fields.Picture is not null,
        EventFieldKeys.Register => fields.RegisterLink.Length > 0,
        EventFieldKeys.Links => fields.Links.Count > 0,
        EventFieldKeys.Label => fields.Label.Length > 0,
        _ => fields.Title.Length > 0,
    };

    public static string Words(Timed value, string key) => key switch
    {
        EventFieldKeys.Time => EventDates.Times(value.StartMinute, value.EndMinute),
        EventFieldKeys.Title => value.Fields.Title,
        EventFieldKeys.Label => value.Fields.Label,
        EventFieldKeys.Place => PlaceWords(value.Fields.Place),
        EventFieldKeys.Picture => value.Fields.Picture is null ? "no picture" : "its own picture",
        EventFieldKeys.Description => "its own description",
        EventFieldKeys.Contacts => string.Join(", ", value.Fields.Contacts.Select(contact => contact.Name)),
        EventFieldKeys.Register => value.Fields.RegisterLink,
        _ => "its own links",
    };

    public static string ChangeNote(Timed before, Timed after, DateOnly? dateBefore = null, DateOnly? dateAfter = null)
    {
        List<string> notes = [];
        if (dateBefore != dateAfter && dateAfter is { } day)
            notes.Add($"Now on {day.ToString("dddd d MMMM", CultureInfo.InvariantCulture)}");
        List<string> changed = Differences(before, after);
        if (changed.Contains(EventFieldKeys.Time))
            notes.Add($"Now {EventDates.Times(after.StartMinute, after.EndMinute)}");
        if (changed.Contains(EventFieldKeys.Place))
            notes.Add(IsSet(after.Fields, EventFieldKeys.Place) ? $"Now at {PlaceWords(after.Fields.Place)}" : EventFieldKeys.Missing(EventFieldKeys.Place));
        if (changed.Contains(EventFieldKeys.Title))
            notes.Add($"Now called “{after.Fields.Title}”");
        if (notes.Count == 0 && changed.Count > 0)
            notes.Add("Details updated");
        return string.Join(" · ", notes.Take(MostNotes));
    }

    public static string PlaceWords(EventPlace? place) =>
        place is null ? "" : place.Name.Length > 0 ? place.Name : place.Address;

    private static string Value(Timed value, string key) => key switch
    {
        EventFieldKeys.Time => $"{value.StartMinute}-{value.EndMinute}",
        EventFieldKeys.Title => value.Fields.Title,
        EventFieldKeys.Label => value.Fields.Label,
        EventFieldKeys.Picture => value.Fields.Picture?.Id ?? "",
        EventFieldKeys.Description => value.Fields.Description is { IsEmpty: false } description ? description.Markdown + "|" + string.Join(",", description.Media.Select(media => media.Id)) : "",
        EventFieldKeys.Place => IsSet(value.Fields, EventFieldKeys.Place) ? $"{value.Fields.Place!.PlaceId}|{value.Fields.Place.Name}|{value.Fields.Place.Address}" : "",
        EventFieldKeys.Contacts => string.Join("\n", value.Fields.Contacts.Select(contact => $"{contact.PersonId}|{contact.Name}|{contact.Phone}|{contact.Email}")),
        EventFieldKeys.Register => value.Fields.RegisterLink,
        _ => string.Join("\n", value.Fields.Links.Select(link => $"{link.Title}|{link.Url}")),
    };
}
