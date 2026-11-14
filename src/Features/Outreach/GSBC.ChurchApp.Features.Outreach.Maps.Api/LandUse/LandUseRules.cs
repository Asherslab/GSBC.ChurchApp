namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;

internal enum Delivery
{
    Deliver,
    Check,
    Skip,
}

internal readonly record struct LandUseVerdict(Delivery Delivery, string? Label)
{
    public static readonly LandUseVerdict Home = new(Delivery.Deliver, null);

    public static LandUseVerdict CheckFor(string why) => new(Delivery.Check, $"check: {why}");
}

internal static class LandUseRules
{
    private static readonly HashSet<string> MoretonBayHomes =
        ["Single Unit Dwelling", "Multi Unit Dwelling (flats)", "Residential CTS Properties", "Retirement Village"];

    private static readonly HashSet<string> MoretonBayCheck = ["", "Outbuildings", "Cattle Breeding & Fattening", "Turf Farms"];

    private static readonly (string Use, string Label)[] MoretonBaySkip =
    [
        ("Shopping Centre", "shopping centre"), ("Shops", "shop"), ("Professional Offices", "office"), ("Restaurant", "restaurant"),
        ("Service Station", "service station"), ("Hotel", "hotel"), ("Club", "club"), ("Industr", "industry"), ("Extractive", "quarry"),
        ("Educational", "school"), ("Child Care", "childcare"), ("Religious", "church"), ("Hospitals", "hospital or care home"),
        ("Parks", "park"), ("Vacant", "vacant land"), ("Reservoir", "utility"), ("Telco", "utility"), ("Cemeteries", "cemetery"),
        ("Community Protection", "emergency services"), ("Nurseries", "nursery"),
    ];

    private static readonly HashSet<string> BrisbaneHomes = ["LDR", "LMR", "MDR", "HDR", "CR", "EC", "RU", "RR", "EM", "TA"];

    private const string BrisbaneMixedUse = "MU";
    private const string BrisbaneCommunityFacilities = "CF";

    private static readonly Dictionary<string, string> BrisbaneSkip = new()
    {
        ["NC"] = "shops", ["DC"] = "shops", ["MC"] = "shops", ["PC"] = "shops", ["CN"] = "shops", ["SC"] = "shops",
        ["OS"] = "park", ["SR"] = "sport", ["SP"] = "utility",
        ["IN"] = "industry", ["LII"] = "industry", ["GI"] = "industry", ["HI"] = "industry", ["IN1"] = "industry", ["IN2"] = "industry",
        ["SI"] = "industry", ["IPA"] = "industry",
    };

    private static readonly (string Precinct, string Label)[] BrisbaneCommunity =
        [("Cemetery", "cemetery"), ("Emergency", "emergency services"), ("Health", "health care"), ("Education", "school"), ("Worship", "church")];

    public static LandUseVerdict MoretonBay(string? landUse)
    {
        string use = (landUse ?? "").Trim();
        if (MoretonBayHomes.Contains(use))
            return LandUseVerdict.Home;
        if (MoretonBayCheck.Contains(use))
            return LandUseVerdict.CheckFor(use.Length == 0 ? "no land use record" : use.ToLowerInvariant());
        foreach ((string key, string label) in MoretonBaySkip)
            if (use.Contains(key, StringComparison.OrdinalIgnoreCase))
                return new LandUseVerdict(Delivery.Skip, label);
        return LandUseVerdict.CheckFor(use.ToLowerInvariant());
    }

    public static LandUseVerdict Brisbane(string? zoneCode, string? precinct)
    {
        string code = (zoneCode ?? "").Trim();
        string description = precinct ?? "";
        if (BrisbaneHomes.Contains(code))
            return LandUseVerdict.Home;
        if (code == BrisbaneMixedUse)
            return LandUseVerdict.CheckFor("mixed use");
        if (code == BrisbaneCommunityFacilities)
            return new LandUseVerdict(Delivery.Skip,
                BrisbaneCommunity.FirstOrDefault(community => description.Contains(community.Precinct, StringComparison.Ordinal)).Label ?? "community facility");
        if (BrisbaneSkip.TryGetValue(code, out string? label))
            return new LandUseVerdict(Delivery.Skip, label);
        string last = description.Split(" - ")[^1].ToLowerInvariant();
        return new LandUseVerdict(Delivery.Skip, last.Length == 0 ? "not homes" : last);
    }
}
