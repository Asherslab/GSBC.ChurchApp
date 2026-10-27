namespace Gsbc.AppPoc.Data;

public sealed class SiteMapSvg(HttpClient http)
{
    private Task<string>? loading;

    public Task<string> GetAsync() => loading ??= http.GetStringAsync(SitePlan.SvgPath);
}
