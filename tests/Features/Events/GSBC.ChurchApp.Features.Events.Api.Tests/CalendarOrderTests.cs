using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("Putting calendars in order rewrites their order in Postgres and in the calendars feed the phones read")]
public sealed class CalendarOrderTests : EventsHost
{
    [Fact]
    public async Task TheOrderStaffSetIsTheOrderPhonesGetAndCalendarsLeftOutKeepTheirPlaceAfterIt()
    {
        CalendarSettings services = await CalendarAsync("Services", Audience.Everyone);
        CalendarSettings kids = await CalendarAsync("Impact Kids", Audience.Everyone);
        CalendarSettings youth = await CalendarAsync("Impact Youth", Audience.Everyone);

        await WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>()
            .OrderCalendarsAsync(new CalendarOrder { CalendarIds = [youth.Calendar.Id, services.Calendar.Id] }, As(Person())));

        List<CalendarInfo> shown = (await LiveRowsAsync(EventsFeeds.Calendars))
            .Select(row => EventsPayloads.Decode<CalendarInfo>(row.Payload)).OrderBy(calendar => calendar.Order).ToList();
        Assert.Equal(["Impact Youth", "Services", "Impact Kids"], shown.Select(calendar => calendar.Name));
    }
}
