using GSBC.ChurchApp.Features.People.Admin.Components;
using GSBC.ChurchApp.Features.People.Client;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.Admin;

public static class PeopleAdminExtensions
{
    public const string FamilyTab = "family";
    public const string SafeguardingTab = "safeguarding";
    private const int FamilyOrder = 2;
    private const int SafeguardingOrder = 3;

    public static IServiceCollection AddPeopleAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("People", typeof(PeopleAdminExtensions).Assembly,
            [new NavEntry("Safeguarding", "safeguarding", AppIcons.Safeguarding, PeoplePermissions.SafeguardingView, Area: NavAreas.Safety,
                Summary: "Blue cards and training that have run out or are due, and young people without a parent"),
             new NavEntry("Safe Ministry Check", "safe-ministry-check", AppIcons.SafeMinistryCheck, PeoplePermissions.SafeguardingView, Area: NavAreas.Safety,
                Summary: "Blue cards and training brought across from Safe Ministry Check, and records still to match")],
            [PeopleFeeds.Family, PeopleFeeds.Safeguarding], [PeopleFeeds.Mine]));
        services.AddSingleton(new PersonTab(FamilyTab, "Family", FamilyOrder, Family, PeoplePermissions.FamilyView));
        services.AddSingleton(new PersonTab(SafeguardingTab, "Safeguarding", SafeguardingOrder, Safeguarding, PeoplePermissions.SafeguardingView));
        services.AddSingleton(new PersonFact("born", "Birth date", 0, BirthDate, PeoplePermissions.FamilyView));
        return services.AddPeopleClient().AddSingleton<SafeMinistryCheckClient>().AddSingleton<PeopleOfficeClient>().AddSingleton<IOfficeWeekSource, SafeguardingOfficeWeek>();
    }

    private static readonly RenderFragment<PersonOnPage> Family = person => builder =>
    {
        builder.OpenComponent<PersonFamilyTab>(0);
        builder.AddComponentParameter(1, nameof(PersonFamilyTab.Person), person);
        builder.CloseComponent();
    };

    private static readonly RenderFragment<PersonOnPage> BirthDate = person => builder =>
    {
        builder.OpenComponent<PersonBirthDate>(0);
        builder.AddComponentParameter(1, nameof(PersonBirthDate.Person), person);
        builder.CloseComponent();
    };

    private static readonly RenderFragment<PersonOnPage> Safeguarding = person => builder =>
    {
        builder.OpenComponent<PersonSafeguardingTab>(0);
        builder.AddComponentParameter(1, nameof(PersonSafeguardingTab.Person), person);
        builder.CloseComponent();
    };
}
