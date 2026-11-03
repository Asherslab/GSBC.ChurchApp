namespace GSBC.ChurchApp.Platform.Client.Modules;

public interface IOpeningSheet
{
    Task<bool> OfferAsync();
}

public sealed class OpeningSheets(IEnumerable<IOpeningSheet> sheets)
{
    public async Task PresentFirstAsync()
    {
        foreach (IOpeningSheet sheet in sheets)
        {
            if (await sheet.OfferAsync())
                return;
        }
    }
}
