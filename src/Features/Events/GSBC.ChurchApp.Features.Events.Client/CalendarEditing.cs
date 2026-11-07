using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class CalendarEditing(ChannelBase channel)
{
    private readonly IEventEditingService _editing = channel.CreateGrpcService<IEventEditingService>();
    private readonly ICalendarSetupService _setup = channel.CreateGrpcService<ICalendarSetupService>();

    public IEventEditingService Editing => _editing;

    public ICalendarSetupService Setup => _setup;

    public ValueTask<CalendarDesk> DeskAsync() => _editing.DeskAsync(new Done());

    public ValueTask<Agenda> AgendaAsync(IEnumerable<string> calendarIds, DateOnly from, DateOnly to) =>
        _editing.AgendaAsync(new AgendaQuery { CalendarIds = calendarIds.ToList(), From = EventDates.Format(from), To = EventDates.Format(to) });

    public ValueTask<BreakList> BreaksAsync() => _editing.BreaksAsync(new Done());

    public ValueTask<PlacesUsed> PlacesUsedAsync() => _editing.PlacesUsedAsync(new Done());

    public static string Problem(Exception exception) => exception switch
    {
        RpcException { StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded } => "That needs a connection. Try again when you're online.",
        RpcException { StatusCode: StatusCode.PermissionDenied } rpc when rpc.Status.Detail.Length == 0 => "You can't change that calendar.",
        RpcException rpc when rpc.Status.Detail.Length > 0 => rpc.Status.Detail,
        _ => "Something went wrong. Try again in a moment.",
    };
}
