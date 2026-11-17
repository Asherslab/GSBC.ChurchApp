using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Common;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Events.Admin;

internal sealed class OnlineEventCards(CalendarEditing editing, ChannelBase channel, IAccess access, TimeProvider time) : IEventCards
{
    private static readonly TimeSpan KeptFor = TimeSpan.FromMinutes(1);

    private readonly IEventsService _events = channel.CreateGrpcService<IEventsService>();
    private Task<Diary>? _loading;
    private DateTimeOffset _loadedAt;
    private bool _loadedSignedIn;

    public IReadOnlyList<string> Feeds => [];

    public Task<Diary> DiaryAsync()
    {
        DateTimeOffset now = time.GetUtcNow();
        if (_loading is null || _loading.IsFaulted || now - _loadedAt > KeptFor || _loadedSignedIn != access.SignedIn)
            (_loading, _loadedAt, _loadedSignedIn) = (access.SignedIn ? LoadAsync() : ForEveryoneAsync(), now, access.SignedIn);
        return _loading;
    }

    private async Task<Diary> LoadAsync()
    {
        List<CalendarInfo> calendars = (await editing.DeskAsync()).Calendars.Select(calendar => calendar.Settings.Calendar).ToList();
        DateOnly today = ChurchTime.Today(time);
        Agenda agenda = await editing.AgendaAsync(calendars.Select(calendar => calendar.Id), today.AddDays(-EventsFeeds.DaysBack), today.AddMonths(EventsFeeds.MonthsAhead));
        return new Diary(calendars, agenda.Occurrences.OrderBy(occurrence => occurrence.StartsAt).ToList(), new Dictionary<string, List<EventContact>>());
    }

    private async Task<Diary> ForEveryoneAsync()
    {
        PublicEvents events = await _events.ForEveryoneAsync(new Done());
        return new Diary(events.Calendars, events.Occurrences, new Dictionary<string, List<EventContact>>());
    }
}
