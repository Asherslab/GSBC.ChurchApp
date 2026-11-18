using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public static class MapAreaWords
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string State(MapArea area) => area.State switch
    {
        MapAreaStates.Waiting => "Waiting to build",
        MapAreaStates.Building => area.Stage is { Length: > 0 } stage ? $"Building: {stage}" : "Building",
        MapAreaStates.Failed => $"Couldn't build it{(area.Problem is { Length: > 0 } problem ? $": {problem}" : "")}.",
        _ => area.BuiltAt is { } built ? $"Built {OutreachWords.Date(built)}" : "Built",
    };

    private static string BuiltDetail(MapArea area) =>
        area.BuiltAt is { } built ? $"Streets, addresses and maps from {OutreachWords.Date(built)}" : "Streets, addresses and maps are ready";

    public static string StateDetail(MapArea area) => area.State switch
    {
        MapAreaStates.Building => area.Stage is { Length: > 0 } stage ? stage : "Starting",
        MapAreaStates.Waiting => "Builds as soon as the one before it finishes",
        MapAreaStates.Built => BuiltDetail(area),
        _ => State(area),
    };

    public static double Progress(MapArea area) => area.Steps == 0 ? 0 : Math.Clamp((double)area.StepsDone / area.Steps, 0, 1);

    public static string Regions(MapArea area)
    {
        long bytes = area.Regions.Sum(region => region.Streets.Bytes + region.Map.Bytes);
        int letterboxes = area.Regions.Sum(region => region.Letterboxes);
        int off = area.Cells.Count(cell => !cell.On);
        string offWords = off == 0 ? "" : $", {off.ToString(Invariant)} off";
        return $"{OutreachWords.Plural(area.Regions.Count, "region", "regions")}{offWords} · {OutreachWords.Plural(letterboxes, "letterbox", "letterboxes")} · "
               + $"{Megabytes(bytes)} to download in all";
    }

    public static string Megabytes(long bytes) => (bytes / 1_000_000.0).ToString(bytes < 10_000_000 ? "0.0" : "N0", Invariant) + "\u00a0MB";

    private const int LongestShortName = 13;

    public static string Label(AreaCell cell) => cell.Look switch
    {
        CellLook.Edge => "+",
        CellLook.Adding => cell.Name.Length > 0 ? $"Adding {cell.Name}" : "Adding",
        CellLook.SwitchingOff => $"Switching off {cell.Name}".TrimEnd(),
        CellLook.Off when cell.OtherAreas.Count > 0 => cell.Name.Length > 0 ? $"{cell.Name} · off here" : "Off here",
        CellLook.Off => cell.Name.Length > 0 ? $"{cell.Name} · off" : "Off",
        CellLook.Empty => "No homes",
        CellLook.Waiting => cell.Name.Length > 0 ? $"{cell.Name} · waiting to build" : "Waiting to build",
        _ => cell.Name,
    };

    public static string ShortLabel(AreaCell cell) => cell.Look switch
    {
        CellLook.Edge => "+",
        CellLook.Adding => "Adding",
        CellLook.SwitchingOff => "Switching off",
        CellLook.Off => "Off",
        CellLook.Empty => "No homes",
        CellLook.Waiting when cell.Name.Length == 0 => "Waiting",
        _ => FirstSuburb(cell.Name),
    };

    public static string Title(AreaCell cell) => cell.Name.Length > 0 ? cell.Name : cell.Look switch
    {
        CellLook.Edge => "A square on the edge",
        CellLook.Empty => "No homes here",
        CellLook.Adding => "A new region",
        _ => "A region not built yet",
    };

    public static string Facts(AreaCell cell, RegionChangeCheck? check)
    {
        IReadOnlyList<string> others = cell.OtherAreas.Count > 0 ? cell.OtherAreas : EstimateOf(cell, check)?.AlsoIn ?? [];
        string alsoIn = others.Count > 0 ? $" · also in {Names(others)}" : "";
        return cell.Look switch
        {
            CellLook.Built when cell.Region is { } region =>
                $"{OutreachWords.Plural(region.Letterboxes, "letterbox", "letterboxes")} · {Megabytes(cell.Bytes)} · version {region.Version.ToString(Invariant)}{alsoIn}",
            CellLook.Empty => "Bush or water: no homes, so phones get nothing here",
            CellLook.Waiting => $"Builds next{alsoIn}",
            CellLook.Off when others.Count > 0 => $"Switched off here: it's on in {Names(others)}, so phones still get it.",
            CellLook.Off => "Switched off: phones don't get it. Switch it on to build it again.",
            CellLook.Adding when others.Count > 0 => $"Adding · already in {Names(others)}, so nothing new builds or downloads",
            CellLook.Adding => Estimate(cell, check) is { } estimate ? $"Adding · {estimate}" : "Adding · builds when you save",
            CellLook.SwitchingOff when others.Count > 0 => $"Switching off here: it stays on in {Names(others)}, so phones keep it",
            CellLook.SwitchingOff when cell.Bytes == 0 => "Switching off · it isn't built, so phones have nothing to drop",
            CellLook.SwitchingOff => $"Switching off · phones drop {Megabytes(cell.Bytes)}",
            _ => "Click to add it to the area",
        };
    }

    public static string Names(IReadOnlyList<string> names) =>
        names.Count < 2 ? string.Concat(names) : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";

    private static CellEstimate? EstimateOf(AreaCell cell, RegionChangeCheck? check) =>
        check?.Added.FirstOrDefault(added => added.Row == cell.Cell.Row && added.Column == cell.Cell.Column);

    public static string? Estimate(AreaCell cell, RegionChangeCheck? check) => EstimateOf(cell, check) is { } found ? Estimate(found) : null;

    private static string Estimate(CellEstimate estimate) => estimate.HasHomes
        ? $"about {OutreachWords.Plural(Rounded(estimate.Addresses), "letterbox", "letterboxes")}, about {Megabytes(estimate.Bytes)}"
        : estimate.OutsideQueensland ? "outside Queensland, so it can't be added"
        : "no homes, so it isn't made";

    public static string Letterboxes(CellEstimate estimate) => estimate.AlsoIn.Count > 0 ? $"Already in {Names(estimate.AlsoIn)}"
        : estimate.HasHomes ? OutreachWords.Plural(Rounded(estimate.Addresses), "letterbox", "letterboxes")
        : estimate.OutsideQueensland ? "Outside Queensland"
        : "No homes";

    public static string? Count(CellEstimate estimate) => estimate.HasHomes && estimate.AlsoIn.Count == 0 ? Rounded(estimate.Addresses).ToString("N0", Invariant) : null;

    public static string Unmade(IReadOnlyCollection<CellEstimate> estimates)
    {
        int outside = estimates.Count(estimate => estimate.OutsideQueensland);
        int empty = estimates.Count(estimate => estimate is { HasHomes: false, OutsideQueensland: false });
        List<string> parts = [];
        if (empty > 0)
            parts.Add($"{OutreachWords.Plural(empty, "square has", "squares have")} no homes and {(empty == 1 ? "isn't" : "aren't")} made");
        if (outside > 0)
            parts.Add($"{OutreachWords.Plural(outside, "square is", "squares are")} outside Queensland and can't be added: there's no address data there");
        return string.Join("; ", parts);
    }

    public static string ChangeLabel(int off, int on) => (off, on) switch
    {
        (0, _) => $"Add {OutreachWords.Plural(on, "region", "regions")}",
        (_, 0) => $"Switch off {OutreachWords.Plural(off, "region", "regions")}",
        _ => $"Switch off {off.ToString(Invariant)} and add {on.ToString(Invariant)}",
    };

    public static string ChangeSummary(IReadOnlyCollection<AreaCell> off, int on, RegionChangeCheck? check)
    {
        List<string> parts = [];
        List<AreaCell> going = [.. off.Where(cell => cell.OtherAreas.Count == 0)];
        List<AreaCell> staying = [.. off.Where(cell => cell.OtherAreas.Count > 0)];
        if (going.Count > 0)
            parts.Add($"{OutreachWords.Plural(going.Count, "region goes", "regions go")} from phones{(check is { FreedBytes: > 0 } ? $", {Megabytes(check.FreedBytes)}" : "")}");
        if (staying.Count > 0)
            parts.Add($"{OutreachWords.Plural(staying.Count, "region stays", "regions stay")} on phones: {(staying.Count == 1 ? "it's" : "they're")} also in {Names([.. staying.SelectMany(cell => cell.OtherAreas).Distinct()])}");
        if (on > 0)
        {
            List<CellEstimate> added = check?.Added ?? [];
            List<CellEstimate> shared = [.. added.Where(estimate => estimate.AlsoIn.Count > 0)];
            List<CellEstimate> fresh = [.. added.Where(estimate => estimate.AlsoIn.Count == 0)];
            int building = added.Count == 0 ? on : fresh.Count(estimate => estimate.HasHomes);
            string unmade = Unmade(fresh);
            if (building > 0 || unmade.Length > 0)
            {
                string size = fresh.Count == 0 ? "" : $", about {Megabytes(fresh.Where(estimate => estimate.HasHomes).Sum(estimate => estimate.Bytes))}";
                string none = unmade.Length == 0 ? "" : $" ({unmade})";
                parts.Add(building == 0 ? $"{char.ToUpperInvariant(unmade[0])}{unmade[1..]}" : $"{OutreachWords.Plural(building, "region builds", "regions build")}{size}{none}");
            }
            if (shared.Count > 0)
                parts.Add($"{OutreachWords.Plural(shared.Count, "region is", "regions are")} already in {Names([.. shared.SelectMany(estimate => estimate.AlsoIn).Distinct()])}, so {(shared.Count == 1 ? "it doesn't" : "they don't")} build again");
        }
        return string.Join(". ", parts) + ".";
    }

    public static bool AllShared(IReadOnlyCollection<CellEstimate> cells) =>
        cells.Any(cell => cell.HasHomes) && cells.Where(cell => cell.HasHomes).All(cell => cell.AlsoIn.Count > 0);

    public static string AddEstimate(IReadOnlyCollection<CellEstimate> cells)
    {
        List<CellEstimate> homes = [.. cells.Where(cell => cell.HasHomes)];
        string unmade = Unmade(cells);
        string none = unmade.Length == 0 ? "" : $" {char.ToUpperInvariant(unmade[0])}{unmade[1..]}.";
        if (homes.Count == 0 && none.Length > 0)
            return none.TrimStart();
        string found = $"{OutreachWords.Plural(homes.Count, "region", "regions")} with homes, about {OutreachWords.Plural(homes.Sum(cell => cell.Addresses), "letterbox", "letterboxes")}";
        List<CellEstimate> shared = [.. homes.Where(cell => cell.AlsoIn.Count > 0)];
        List<CellEstimate> fresh = [.. homes.Where(cell => cell.AlsoIn.Count == 0)];
        string others = Names([.. shared.SelectMany(cell => cell.AlsoIn).Distinct()]);
        string size = Megabytes(fresh.Sum(cell => cell.Bytes));
        return (shared.Count, fresh.Count) switch
        {
            (0, _) => $"{found}, about {size} for phones in all.{none}",
            (_, 0) => $"{found}. {(shared.Count == 1 ? "It's" : "All of them are")} already in {others}, so nothing new builds or downloads.{none}",
            _ => $"{found}. {OutreachWords.Plural(shared.Count, "is", "are")} already in {others}; {(fresh.Count == 1 ? "the other one builds" : $"the other {fresh.Count.ToString(Invariant)} build")}, about {size} for phones.{none}",
        };
    }

    public static string DeleteLead(MapArea area)
    {
        List<MapCell> on = [.. area.Cells.Where(cell => cell.On)];
        HashSet<string> shared = [.. on.Where(cell => cell.AlsoIn.Count > 0).Select(cell => cell.RegionId)];
        List<OutreachRegion> going = [.. area.Regions.Where(region => !shared.Contains(region.Id))];
        string others = Names([.. on.SelectMany(cell => cell.AlsoIn).Distinct()]);
        string stays = shared.Count == 0 ? ""
            : $"{OutreachWords.Plural(shared.Count, "region stays", "regions stay")} on phones: {(shared.Count == 1 ? "it's" : "they're")} also in {others}. ";
        string bytes = Megabytes(going.Sum(region => region.Streets.Bytes + region.Map.Bytes));
        string goes = going.Count == 0 ? ""
            : shared.Count == 0 ? $"Its {OutreachWords.Plural(going.Count, "region goes", "regions go")} from phones and {(going.Count == 1 ? "its" : "their")} maps and streets are deleted ({bytes}). "
            : going.Count == 1 ? $"The other region goes from phones and its maps and streets are deleted ({bytes}). "
            : $"The other {going.Count.ToString(Invariant)} regions go from phones and their maps and streets are deleted ({bytes}). ";
        return stays + goes + "What walkers did there, coverage and history, stays. You can add the place again later; it builds as a new area.";
    }

    public static string Use(RegionUse use) => use.Kind switch
    {
        RegionUseKinds.Day when use.On is { } on => $"{use.Name}'s outreach day on {OutreachWords.Date(on)}",
        RegionUseKinds.Day => $"An outreach day of {use.Name}",
        _ => $"The campaign {use.Name}",
    };

    public static string Place(MapPlace place)
    {
        string kind = place.Kind switch
        {
            MapPlaceKinds.City => "City",
            MapPlaceKinds.Town => "Town",
            MapPlaceKinds.Suburb => "Suburb",
            MapPlaceKinds.Village => "Village",
            MapPlaceKinds.Hamlet => "Hamlet",
            MapPlaceKinds.Neighbourhood => "Neighbourhood",
            _ => "Locality",
        };
        return place.FromChurchKm < 1 || place.Direction.Length == 0
            ? $"{kind} · at the church"
            : $"{kind} · {place.FromChurchKm.ToString("N0", Invariant)} km {place.Direction} of the church";
    }

    public static double? Radius(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, Invariant, out double km) && km is >= 1 and <= 30 ? km : null;

    private static string FirstSuburb(string name)
    {
        string first = name.Split(" and ")[0];
        return first.Length <= LongestShortName ? first : first[..(LongestShortName - 1)].TrimEnd() + "…";
    }

    private static int Rounded(int count) => count < 1_000 ? count : (int)(Math.Round(count / 100.0) * 100);
}
