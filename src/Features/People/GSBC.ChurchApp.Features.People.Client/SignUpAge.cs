using GSBC.ChurchApp.Features.People.Client.Components;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Features.People.Client;

public sealed class SignUpAge(PeopleClient people, TimeProvider clock) : ISignUpQuestion
{
    public int Order => 10;

    public bool Adult { get; set; }

    public string Born { get; set; } = "";

    public string ParentEmail { get; set; } = "";

    public DateOnly Today => ChurchTime.Today(clock);

    private DateOnly? BirthDate => PeopleWords.ParseDay(Born) is { } day && day <= Today ? day : null;

    public bool Minor => !Adult && Ages.IsMinor(BirthDate, Today);

    public bool Answered => Adult || (BirthDate is not null && (!Minor || ParentEmail.Trim().Length > 0));

    public RenderFragment Ask(EventCallback changed) => builder =>
    {
        builder.OpenComponent<SignUpAgeAsk>(0);
        builder.AddComponentParameter(1, nameof(SignUpAgeAsk.Answers), this);
        builder.AddComponentParameter(2, nameof(SignUpAgeAsk.Changed), changed);
        builder.CloseComponent();
    };

    public async Task SaveAsync()
    {
        if (!Answered)
            return;
        await people.SetMyBirthDateAsync(Adult ? null : BirthDate, Adult);
        if (Minor)
            await people.AskToLinkAsync(ParentEmail);
        (Adult, Born, ParentEmail) = (false, "", "");
    }
}
