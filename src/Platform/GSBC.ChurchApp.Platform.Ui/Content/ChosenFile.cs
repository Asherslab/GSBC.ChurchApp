using Microsoft.AspNetCore.Components.Forms;

namespace GSBC.ChurchApp.Platform.Ui.Content;

public sealed record ChosenFile(IBrowserFile File, string ContentType);
