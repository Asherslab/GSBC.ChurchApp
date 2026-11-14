using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common.Printing;

namespace GSBC.ChurchApp.Features.Outreach.Client.Planning;

public sealed record SheetDay(string Title, string When, string MeetingPlace, string Campaign, string HandedOut, string Note, string Attribution);

public sealed record SheetRoute(PlannedSection Section, string Code, string Link);

public static class RouteSheets
{
    private readonly record struct Box(double X0, double Y0, double X1, double Y1)
    {
        public bool Overlaps(Box other) => X0 < other.X1 && other.X0 < X1 && Y0 < other.Y1 && other.Y0 < Y1;

        public bool Holds(Box other) => other.X0 >= X0 && other.X1 <= X1 && other.Y0 >= Y0 && other.Y1 <= Y1;
    }

    private const double Margin = 34;
    private const double MapTop = 150;
    private const double MapHeight = 430;
    private const double ScanSize = 96;
    private const double LabelSize = 7.5;
    private const double ArrowEveryMetres = 140;

    private static readonly PdfColour Ink = PdfColour.Black;
    private static readonly PdfColour Ink2 = PdfColour.Grey(0.3);
    private static readonly PdfColour Street = PdfColour.Grey(0.78);
    private static readonly PdfColour MainRoad = PdfColour.Grey(0.62);
    private static readonly PdfColour Rule = PdfColour.Grey(0.8);

    public static PdfDocument Make(DayPlanner planner, SheetDay day, IReadOnlyList<SheetRoute> routes)
    {
        PdfDocument document = new() { Title = $"{day.Title}: route maps" };
        foreach (SheetRoute route in routes)
            Page(document.AddPage(), planner, day, route);
        return document;
    }

    private static void Page(PdfPage page, DayPlanner planner, SheetDay day, SheetRoute route)
    {
        PlannedSection section = route.Section;
        RouteSummary summary = RouteSummary.Of(section.Summary.Plan, planner.Open);
        double width = page.Width - Margin * 2;

        page.Text(Margin, Margin + 26, $"Route {section.Number}", PdfFont.Bold, 26, Ink);
        double nameWidth = width - ScanSize - 16;
        double y = Margin + 46;
        foreach (string line in page.Wrap(section.Name, PdfFont.Bold, 14, nameWidth).Take(2))
        {
            page.Text(Margin, y, line, PdfFont.Bold, 14, Ink);
            y += 17;
        }
        page.Text(Margin, y + 2, $"{day.Title} · {day.When}{(day.MeetingPlace.Length > 0 ? $" · meet at {day.MeetingPlace}" : "")}", PdfFont.Regular, 9, Ink2);
        page.Text(Margin, y + 15, Facts(section, summary, day), PdfFont.Bold, 10, Ink);

        double scanX = page.Width - Margin - ScanSize;
        page.Qr(QrCode.Encode(route.Link), scanX, Margin - 6, ScanSize);
        page.Text(scanX + ScanSize / 2, Margin + ScanSize, "Scan to open it in the app", PdfFont.Regular, 7, Ink2, TextAlign.Centre);
        page.Text(scanX + ScanSize / 2, Margin + ScanSize + 11, $"Code {route.Code}", PdfFont.Bold, 9, Ink, TextAlign.Centre);

        DrawMap(page, planner, section, summary, Margin, MapTop, width, MapHeight);
        DrawStreets(page, summary, day, Margin, MapTop + MapHeight + 42, width);
    }

    private static string Facts(PlannedSection section, RouteSummary summary, SheetDay day)
    {
        string handed = day.HandedOut.Length > 0 ? day.HandedOut.ToLowerInvariant() : "bundles";
        string bundles = summary.BundlesToTake == 1 ? $"1 bundle of {handed}" : $"{summary.BundlesToTake} bundles of {handed}";
        return $"{section.Letterboxes} letterboxes · take {bundles} · about {OutreachWords.Minutes(section.Minutes)} · {summary.Metres / 1000:0.0} km";
    }

    private static void DrawMap(PdfPage page, DayPlanner planner, PlannedSection section, RouteSummary summary, double left, double top, double width, double height)
    {
        StreetGraph graph = planner.Network.Graph;
        List<Flat> routeFlats = summary.Line.Select(graph.Projection.ToFlat).ToList();
        Flat meeting = graph.Projection.ToFlat(planner.Meeting);
        Flat low = new(routeFlats.Min(f => f.X), routeFlats.Min(f => f.Y)), high = new(routeFlats.Max(f => f.X), routeFlats.Max(f => f.Y));
        double spanX = Math.Max(high.X - low.X, 150), spanY = Math.Max(high.Y - low.Y, 150);
        double scale = Math.Min(width / (spanX * 1.2), height / (spanY * 1.2));
        Flat centre = new((low.X + high.X) / 2, (low.Y + high.Y) / 2);
        PdfPoint At(Flat flat) => new(left + width / 2 + (flat.X - centre.X) * scale, top + height / 2 - (flat.Y - centre.Y) * scale);
        Flat viewLow = new(centre.X - width / 2 / scale, centre.Y - height / 2 / scale), viewHigh = new(centre.X + width / 2 / scale, centre.Y + height / 2 / scale);
        Box frame = new(left + 4, top + 4, left + width - 4, top + height - 4);
        PdfPoint meet = At(meeting);
        bool showMeeting = frame.Holds(new Box(meet.X - 8, meet.Y - 8, meet.X + 8, meet.Y + 8));
        PdfPoint start = At(routeFlats[0]);
        List<Box> taken = [new(start.X - 8, start.Y - 8, start.X + 40, start.Y + 8), new(left + width - 30, top, left + width, top + 46), new(left, top + height - 26, left + 150, top + height)];
        if (showMeeting)
            taken.Add(new Box(meet.X - 8, meet.Y - 8, meet.X + 64, meet.Y + 8));

        page.Save();
        page.ClipTo(left, top, width, height);
        foreach (int street in graph.Index.StreetsIn(viewLow, viewHigh).OrderBy(s => graph.Street(s).Kind.IsMainRoad()))
        {
            StreetInfo info = graph.Street(street);
            if (info.Kind.IsPath())
                continue;
            page.Line(graph.ShapeBetween(street).Select(At).ToList(), info.Kind.IsMainRoad() ? MainRoad : Street, info.Kind.IsMainRoad() ? 2.4 : 1);
        }
        foreach (RouteStep step in summary.Steps)
        {
            List<PdfPoint> line = step.Line.Select(p => At(graph.Projection.ToFlat(p))).ToList();
            if (step.Delivers)
                page.Line(line, Ink, 3.4);
            else
                page.Line(line, Ink, 1.3, [3, 2.5]);
        }
        Arrows(page, routeFlats, At);
        StreetNames(page, graph, summary, At, viewLow, viewHigh, frame, taken);
        foreach (RouteMarker marker in summary.Steps.SelectMany(step => step.Markers).Where(m => m.Kind is MarkerKind.NoJunkMail or MarkerKind.DontLetterbox))
        {
            PdfPoint at = At(graph.Projection.ToFlat(marker.Point));
            page.Circle(at.X, at.Y, 2.6, PdfColour.White, Ink, 0.9);
            page.Line([new(at.X - 1.8, at.Y - 1.8), new(at.X + 1.8, at.Y + 1.8)], Ink, 0.8, round: false);
        }
        page.Circle(start.X, start.Y, 6.5, Ink, PdfColour.White, 1.6);
        page.Text(start.X + 9, start.Y + 3.5, "Start", PdfFont.Bold, 9, Ink, halo: PdfColour.White);
        if (showMeeting)
        {
            page.Rectangle(meet.X - 6, meet.Y - 6, 12, 12, PdfColour.White, Ink, 1.6);
            page.Text(meet.X, meet.Y + 3.2, "M", PdfFont.Bold, 8, Ink, TextAlign.Centre);
            bool room = meet.X + 64 < left + width;
            page.Text(room ? meet.X + 10 : meet.X - 10, meet.Y + 3.5, "Meet here", PdfFont.Bold, 9, Ink, room ? TextAlign.Left : TextAlign.Right, halo: PdfColour.White);
        }
        page.Restore();
        page.Rectangle(left, top, width, height, null, Ink, 0.8);
        ScaleBar(page, scale, left + 10, top + height - 12);
        North(page, left + width - 16, top + 22);
        Key(page, left, top + height + 10);
    }

    private static void Arrows(PdfPage page, List<Flat> line, Func<Flat, PdfPoint> at)
    {
        double walked = ArrowEveryMetres / 2;
        for (int i = 1; i < line.Count; i++)
        {
            double length = Flat.Distance(line[i - 1], line[i]);
            while (walked <= length)
            {
                Flat along = line[i - 1] + (line[i] - line[i - 1]) * (walked / length);
                PdfPoint tip = at(along), from = at(line[i - 1]), to = at(line[i]);
                double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
                PdfPoint Back(double turn) => new(tip.X - Math.Cos(angle + turn) * 4.2, tip.Y - Math.Sin(angle + turn) * 4.2);
                page.Polygon([tip, Back(0.55), Back(-0.55)], PdfColour.White, Ink, 0.5);
                walked += ArrowEveryMetres;
            }
            walked -= length;
        }
    }

    private static void StreetNames(PdfPage page, StreetGraph graph, RouteSummary summary, Func<Flat, PdfPoint> at, Flat low, Flat high, Box frame, List<Box> taken)
    {
        HashSet<string> named = [];
        foreach (IGrouping<string, RouteStep> street in summary.Steps.Where(s => s.Delivers && s.Street != WalkLeg.Unnamed).GroupBy(s => s.Street))
        {
            List<PdfPoint> line = street.MaxBy(s => s.Metres)!.Line.Select(p => at(graph.Projection.ToFlat(p))).ToList();
            if (Label(page, line, street.Key, PdfFont.Bold, LabelSize, Ink, frame, taken))
                named.Add(street.Key);
        }
        foreach (IGrouping<string, int> street in graph.Index.StreetsIn(low, high)
                     .Where(s => graph.Street(s).Name.Length > 0 && !graph.Street(s).Kind.IsPath())
                     .GroupBy(s => graph.Street(s).Name)
                     .Where(g => !named.Contains(g.Key))
                     .OrderByDescending(g => g.Any(s => graph.Street(s).Kind.IsMainRoad()))
                     .ThenByDescending(g => g.Sum(s => graph.Street(s).Length)))
        {
            int longest = street.MaxBy(s => graph.Street(s).Length);
            Label(page, graph.ShapeBetween(longest).Select(at).ToList(), street.Key, PdfFont.Regular, LabelSize - 0.5, Ink2, frame, taken);
        }
    }

    private static bool Label(PdfPage page, List<PdfPoint> line, string name, PdfFont font, double size, PdfColour colour, Box frame, List<Box> taken)
    {
        if (line.Count < 2)
            return false;
        (PdfPoint a, PdfPoint b) = Longest(line);
        double angle = Math.Atan2(b.Y - a.Y, b.X - a.X);
        if (angle > Math.PI / 2 || angle < -Math.PI / 2)
        {
            (a, b) = (b, a);
            angle = Math.Atan2(b.Y - a.Y, b.X - a.X);
        }
        double length = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        double width = PdfPage.Measure(name, font, size);
        if (width > length + 30)
            return false;
        double x = (a.X + b.X) / 2 + Math.Sin(angle) * 6, y = (a.Y + b.Y) / 2 - Math.Cos(angle) * 6;
        double halfX = Math.Abs(Math.Cos(angle)) * width / 2 + Math.Abs(Math.Sin(angle)) * size / 2 + 2;
        double halfY = Math.Abs(Math.Sin(angle)) * width / 2 + Math.Abs(Math.Cos(angle)) * size / 2 + 2;
        Box box = new(x - halfX, y - halfY, x + halfX, y + halfY);
        if (!frame.Holds(box) || taken.Any(box.Overlaps))
            return false;
        taken.Add(box);
        page.Text(x, y + size / 3, name, font, size, colour, TextAlign.Centre, angle, PdfColour.White);
        return true;
    }

    private static (PdfPoint, PdfPoint) Longest(List<PdfPoint> line)
    {
        int best = 0;
        double bestLength = -1;
        for (int i = 1; i < line.Count; i++)
        {
            double length = Math.Pow(line[i].X - line[i - 1].X, 2) + Math.Pow(line[i].Y - line[i - 1].Y, 2);
            if (length > bestLength)
                (best, bestLength) = (i, length);
        }
        return (line[best - 1], line[best]);
    }

    private static void ScaleBar(PdfPage page, double scale, double x, double y)
    {
        double metres = new[] { 50, 100, 200, 250, 500, 1000 }.LastOrDefault(m => m * scale <= 110, 50);
        double length = metres * scale;
        page.Rectangle(x - 4, y - 12, length + 46, 18, PdfColour.White);
        page.Line([new(x, y - 4), new(x, y), new(x + length, y), new(x + length, y - 4)], Ink, 1, round: false);
        page.Text(x + length + 5, y + 1, metres >= 1000 ? $"{metres / 1000:0} km" : $"{metres:0} m", PdfFont.Regular, 7.5, Ink);
    }

    private static void North(PdfPage page, double x, double y)
    {
        page.Circle(x, y, 10, PdfColour.White, Ink, 0.7);
        page.Polygon([new(x, y - 7), new(x + 4, y + 4), new(x, y + 2), new(x - 4, y + 4)], Ink);
        page.Text(x, y + 19, "N", PdfFont.Bold, 7, Ink, TextAlign.Centre);
    }

    private static void Key(PdfPage page, double x, double y)
    {
        page.Line([new(x, y), new(x + 22, y)], Ink, 3.4);
        page.Text(x + 28, y + 2.6, "Letterbox both sides", PdfFont.Regular, 7.5, Ink2);
        page.Line([new(x + 118, y), new(x + 140, y)], Ink, 1.3, [3, 2.5]);
        page.Text(x + 146, y + 2.6, "Walk through, no letterboxes", PdfFont.Regular, 7.5, Ink2);
        page.Circle(x + 266, y, 2.6, PdfColour.White, Ink, 0.9);
        page.Line([new(x + 264.2, y - 1.8), new(x + 267.8, y + 1.8)], Ink, 0.8, round: false);
        page.Text(x + 273, y + 2.6, "Skip this letterbox", PdfFont.Regular, 7.5, Ink2);
    }

    private static void DrawStreets(PdfPage page, RouteSummary summary, SheetDay day, double left, double top, double width)
    {
        page.Text(left, top, "The streets, in order", PdfFont.Bold, 10, Ink);
        List<string> lines = [];
        foreach (StreetLine street in Ordered(summary))
            lines.Add($"{lines.Count + 1}. {street.Title}, {(street.Sides == 2 ? "both sides" : "one side")} ({street.Letterboxes})");
        double columnWidth = (width - 18) / 2;
        int perColumn = (int)Math.Ceiling(lines.Count / 2.0);
        double bottom = page.Height - Margin - 46;
        int fits = (int)((bottom - top - 8) / 11.5);
        perColumn = Math.Min(perColumn, fits);
        for (int i = 0; i < lines.Count && i < perColumn * 2; i++)
        {
            double x = left + (i < perColumn ? 0 : columnWidth + 18);
            double y = top + 15 + i % perColumn * 11.5;
            string text = i == perColumn * 2 - 1 && lines.Count > perColumn * 2 ? $"and {lines.Count - i} more streets" : lines[i];
            page.Text(x, y, Clip(text, columnWidth), PdfFont.Regular, 8.5, Ink);
        }
        int skips = summary.SkippedNonHomes;
        int noJunk = summary.Steps.SelectMany(s => s.Markers).Count(m => m.Kind is MarkerKind.NoJunkMail or MarkerKind.DontLetterbox);
        List<string> notes = [];
        if (skips > 0)
            notes.Add($"Skips {skips} {(skips == 1 ? "place that isn't a home" : "places that aren't homes")} (shops, schools, parks).");
        if (noJunk > 0)
            notes.Add($"{noJunk} {(noJunk == 1 ? "letterbox is" : "letterboxes are")} marked not to letterbox on the map.");
        notes.Add("Keep the houses on your left and you'll cover both sides. Please walk in pairs.");
        if (day.Note.Length > 0)
            notes.Add(day.Note);
        double noteY = bottom + 4;
        page.Line([new(left, noteY - 10), new(left + width, noteY - 10)], Rule, 0.6, round: false);
        foreach (string line in page.Wrap(string.Join(" ", notes), PdfFont.Regular, 8, width).Take(3))
        {
            page.Text(left, noteY, line, PdfFont.Regular, 8, Ink2);
            noteY += 10;
        }
        page.Text(left, page.Height - Margin + 6, Clip(day.Attribution, width), PdfFont.Regular, 6.5, Ink2);
    }

    private static IEnumerable<StreetLine> Ordered(RouteSummary summary)
    {
        List<string> order = summary.Steps.Where(s => s.Delivers).Select(s => s.Title).Distinct().ToList();
        return summary.Streets.OrderBy(street => order.IndexOf(street.Title));
    }

    private static string Clip(string text, double width)
    {
        if (PdfPage.Measure(text, PdfFont.Regular, 8.5) <= width)
            return text;
        while (text.Length > 1 && PdfPage.Measure(text + "…", PdfFont.Regular, 8.5) > width)
            text = text[..^1];
        return text + "…";
    }
}
