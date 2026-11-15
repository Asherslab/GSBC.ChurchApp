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

    public static string StateDetail(MapArea area) => area.State switch
    {
        MapAreaStates.Building => area.Stage is { Length: > 0 } stage ? stage : "Starting",
        MapAreaStates.Waiting => "Builds as soon as the one before it finishes",
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

    public static string Label(AreaCell cell) => cell.Look switch
    {
        CellLook.Edge => "+",
        CellLook.Adding => cell.Name.Length > 0 ? $"Adding {cell.Name}" : "Adding",
        CellLook.SwitchingOff => $"Switching off {cell.Name}".TrimEnd(),
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
        CellLook.Waiting => "Waiting",
        _ => FirstSuburb(cell.Name),
    };

    public static string Title(AreaCell cell) => cell.Name.Length > 0 ? cell.Name : cell.Look switch
    {
        CellLook.Edge => "A square on the edge",
        CellLook.Empty => "No homes here",
        CellLook.Adding => "A new region",
        _ => "A region not built yet",
    };

    public static string Facts(AreaCell cell, RegionChangeCheck? check) => cell.Look switch
    {
        CellLook.Built when cell.Region is { } region =>
            $"{OutreachWords.Plural(region.Letterboxes, "letterbox", "letterboxes")} · {Megabytes(cell.Bytes)} · version {region.Version.ToString(Invariant)}",
        CellLook.Empty => "Bush or water: no homes, so phones get nothing here",
        CellLook.Waiting => "Builds next",
        CellLook.Off => "Switched off: phones don't get it. Switch it on to build it again.",
        CellLook.Adding => Estimate(cell, check) is { } estimate ? $"Adding · {estimate}" : "Adding · builds when you save",
        CellLook.SwitchingOff when cell.Bytes == 0 => "Switching off · it isn't built, so phones have nothing to drop",
        CellLook.SwitchingOff => $"Switching off · phones drop {Megabytes(cell.Bytes)}",
        _ => "Click to add it to the area",
    };

    public static string? Estimate(AreaCell cell, RegionChangeCheck? check) =>
        check?.Added.FirstOrDefault(added => added.Row == cell.Cell.Row && added.Column == cell.Cell.Column) is { } found ? Estimate(found) : null;

    public static string Estimate(CellEstimate estimate) => estimate.HasHomes
        ? $"about {OutreachWords.Plural(Rounded(estimate.Addresses), "letterbox", "letterboxes")}, about {Megabytes(estimate.Bytes)}"
        : "no homes, so it isn't made";

    public static string ChangeLabel(int off, int on) => (off, on) switch
    {
        (0, _) => $"Add {OutreachWords.Plural(on, "region", "regions")}",
        (_, 0) => $"Switch off {OutreachWords.Plural(off, "region", "regions")}",
        _ => $"Switch off {off.ToString(Invariant)} and add {on.ToString(Invariant)}",
    };

    public static string ChangeSummary(int off, int on, RegionChangeCheck? check)
    {
        List<string> parts = [];
        if (off > 0)
            parts.Add($"{OutreachWords.Plural(off, "region goes", "regions go")} from phones{(check is { FreedBytes: > 0 } ? $", {Megabytes(check.FreedBytes)}" : "")}");
        if (on > 0)
        {
            List<CellEstimate> added = check?.Added ?? [];
            long bytes = added.Sum(estimate => estimate.Bytes);
            int empty = added.Count(estimate => !estimate.HasHomes);
            string size = added.Count == 0 ? "" : $", about {Megabytes(bytes)}";
            string none = empty == 0 ? "" : $" ({OutreachWords.Plural(empty, "square has", "squares have")} no homes and isn't made)";
            parts.Add($"{OutreachWords.Plural(on, "region builds", "regions build")}{size}{none}");
        }
        return string.Join(". ", parts) + ".";
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

    private static string FirstSuburb(string name) => name.Split(" and ")[0];

    private static int Rounded(int count) => count < 1_000 ? count : (int)(Math.Round(count / 100.0) * 100);
}
