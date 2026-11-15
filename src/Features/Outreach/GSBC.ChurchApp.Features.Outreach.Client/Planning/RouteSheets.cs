using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Common.Printing;

namespace GSBC.ChurchApp.Features.Outreach.Client.Planning;

public sealed record SheetDay(string Title, string When, string MeetingPlace, string Campaign, string HandedOut, string Note, string Attribution);

public sealed record SheetRoute(PlannedSection Section, string Code, string Link);

public static class RouteSheets
{
    private readonly record struct Box(double X0, double Y0, double X1, double Y1)
    {
        public bool Holds(Box other) => other.X0 >= X0 && other.X1 <= X1 && other.Y0 >= Y0 && other.Y1 <= Y1;

        public bool Contains(PdfPoint point) => point.X >= X0 && point.X <= X1 && point.Y >= Y0 && point.Y <= Y1;
    }

    private readonly record struct Spot(PdfPoint Centre, double Angle, double HalfWidth, double HalfHeight)
    {
        public static Spot Of(Box box) => new(new PdfPoint((box.X0 + box.X1) / 2, (box.Y0 + box.Y1) / 2), 0, (box.X1 - box.X0) / 2, (box.Y1 - box.Y0) / 2);

        public IEnumerable<PdfPoint> Corners()
        {
            (double cos, double sin) = (Math.Cos(Angle), Math.Sin(Angle));
            foreach ((double along, double across) in new[] { (-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0) })
                yield return new PdfPoint(Centre.X + along * HalfWidth * cos - across * HalfHeight * sin, Centre.Y + along * HalfWidth * sin + across * HalfHeight * cos);
        }

        public bool Holds(PdfPoint point, double margin)
        {
            double dx = point.X - Centre.X, dy = point.Y - Centre.Y;
            double along = dx * Math.Cos(Angle) + dy * Math.Sin(Angle), across = -dx * Math.Sin(Angle) + dy * Math.Cos(Angle);
            return Math.Abs(along) <= HalfWidth + margin && Math.Abs(across) <= HalfHeight + margin;
        }

        public bool Overlaps(Spot other) => !Apart(this, other) && !Apart(other, this);

        private static bool Apart(Spot a, Spot b)
        {
            foreach (double axis in new[] { a.Angle, a.Angle + Math.PI / 2 })
            {
                (double cos, double sin) = (Math.Cos(axis), Math.Sin(axis));
                double Project(PdfPoint point) => point.X * cos + point.Y * sin;
                List<double> mine = a.Corners().Select(Project).ToList(), theirs = b.Corners().Select(Project).ToList();
                if (mine.Max() < theirs.Min() || theirs.Max() < mine.Min())
                    return true;
            }
            return false;
        }
    }

    private const double Margin = 34;
    private const double MapTop = 150;
    private const double MapHeight = 430;
    private const double ScanSize = 96;
    private const double LabelSize = 7.5;
    private const double RouteLabelSize = 8;
    private const double LabelPadding = 1.5;
    private const double RouteHalfWidth = 1.7;
    private const double MostBend = 0.3;
    private const double PlaceStep = 12;
    private const int MostPlacesARun = 40;
    private const double SampleEvery = 4;
    private static readonly double[] WalkThroughDash = [3, 2.5];
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

        DrawMap(page, planner, summary, Margin, MapTop, width, MapHeight);
        DrawStreets(page, summary, day, Margin, MapTop + MapHeight + 42, width);
    }

    private static string Facts(PlannedSection section, RouteSummary summary, SheetDay day)
    {
        string bundles = summary.BundlesToTake == 1 ? "1 bundle" : $"{summary.BundlesToTake} bundles";
        if (day.HandedOut.Length > 0)
            bundles += $" of {day.HandedOut.ToLowerInvariant()}";
        return $"{section.Letterboxes} letterboxes · take {bundles} · about {OutreachWords.Minutes(section.Minutes)} · {summary.Metres / 1000:0.0} km";
    }

    private static void DrawMap(PdfPage page, DayPlanner planner, RouteSummary summary, double left, double top, double width, double height)
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
        List<Spot> taken = [Spot.Of(new Box(start.X - 8, start.Y - 8, start.X + 40, start.Y + 8)), Spot.Of(new Box(left + width - 30, top, left + width, top + 46)),
            Spot.Of(new Box(left, top + height - 26, left + 150, top + height))];
        if (showMeeting)
            taken.Add(Spot.Of(new Box(meet.X - 8, meet.Y - 8, meet.X + 64, meet.Y + 8)));

        page.Save();
        page.ClipTo(left, top, width, height);
        foreach (int street in graph.Index.StreetsIn(viewLow, viewHigh).OrderBy(s => graph.Street(s).Kind.IsMainRoad()))
        {
            StreetInfo info = graph.Street(street);
            if (info.Kind.IsPath())
                continue;
            page.Line(graph.ShapeBetween(street).Select(At).ToList(), info.Kind.IsMainRoad() ? MainRoad : Street, info.Kind.IsMainRoad() ? 2.4 : 1);
        }
        List<List<PdfPoint>> delivered = summary.Steps.Where(step => step.Delivers).Select(step => step.Line.Select(p => At(graph.Projection.ToFlat(p))).ToList()).ToList();
        foreach (List<PdfPoint> run in WalkedThrough(summary.Steps.Where(step => !step.Delivers).Select(step => step.Line.Select(p => At(graph.Projection.ToFlat(p))).ToList()), delivered))
            page.Line(run, Ink, 1.3, WalkThroughDash, round: false);
        foreach (List<PdfPoint> line in delivered)
            page.Line(line, Ink, 3.4);
        Arrows(page, routeFlats, At);
        List<PdfPoint> walked = Sampled(summary.Steps.Select(step => step.Line.Select(p => At(graph.Projection.ToFlat(p))).ToList()));
        StreetNames(page, graph, summary, At, viewLow, viewHigh, frame, taken, walked);
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
                page.Polygon([tip, Back(0.55), Back(-0.55)], PdfColour.White, Ink);
                walked += ArrowEveryMetres;
            }
            walked -= length;
        }
    }

    private static void StreetNames(PdfPage page, StreetGraph graph, RouteSummary summary, Func<Flat, PdfPoint> at, Flat low, Flat high, Box frame, List<Spot> taken,
        List<PdfPoint> walked)
    {
        HashSet<string> named = [];
        foreach (IGrouping<string, RouteStep> street in summary.Steps.Where(s => s.Delivers && s.Street != WalkLeg.Unnamed).GroupBy(s => s.Street)
                     .OrderByDescending(street => street.Sum(s => s.Metres)))
        {
            List<List<PdfPoint>> lines = street.Select(s => s.Line.Select(p => at(graph.Projection.ToFlat(p))).ToList()).ToList();
            if (Label(page, lines, street.Key, PdfFont.Bold, RouteLabelSize, Ink, frame, taken, walked, onTheRoute: true))
                named.Add(street.Key);
        }
        foreach (IGrouping<string, int> street in graph.Index.StreetsIn(low, high)
                     .Where(s => graph.Street(s).Name.Length > 0 && !graph.Street(s).Kind.IsPath())
                     .GroupBy(s => graph.Street(s).Name)
                     .Where(g => !named.Contains(g.Key))
                     .OrderByDescending(g => g.Any(s => graph.Street(s).Kind.IsMainRoad()))
                     .ThenByDescending(g => g.Sum(s => graph.Street(s).Length)))
        {
            List<List<PdfPoint>> lines = street.Select(s => graph.ShapeBetween(s).Select(at).ToList()).ToList();
            Label(page, lines, street.Key, PdfFont.Regular, LabelSize, Ink2, frame, taken, walked, onTheRoute: false);
        }
    }

    private static bool Label(PdfPage page, List<List<PdfPoint>> lines, string name, PdfFont font, double size, PdfColour colour, Box frame, List<Spot> taken,
        List<PdfPoint> walked, bool onTheRoute)
    {
        double halfWidth = PdfPage.Measure(name, font, size) / 2 + LabelPadding, halfHeight = size / 2 + LabelPadding;
        double beside = halfHeight + RouteHalfWidth + 0.5;
        double[] offsets = onTheRoute ? [-beside, beside] : [0, -beside, beside];
        IEnumerable<Spot> along = Places(lines, halfWidth * 2).SelectMany(place => offsets.Select(offset =>
            new Spot(new PdfPoint(place.Centre.X + Math.Sin(place.Angle) * offset, place.Centre.Y - Math.Cos(place.Angle) * offset), place.Angle, halfWidth, halfHeight)));
        IEnumerable<Spot> level = onTheRoute ? Level(lines, halfWidth, halfHeight) : [];
        foreach (Spot spot in along.Concat(level))
        {
            if (!spot.Corners().All(frame.Contains) || taken.Any(spot.Overlaps) || walked.Any(point => spot.Holds(point, RouteHalfWidth)))
                continue;
            taken.Add(spot);
            page.Text(spot.Centre.X - Math.Sin(spot.Angle) * size * 0.35, spot.Centre.Y + Math.Cos(spot.Angle) * size * 0.35, name, font, size, colour, TextAlign.Centre, spot.Angle,
                PdfColour.White);
            return true;
        }
        return false;
    }

    private static IEnumerable<Spot> Level(List<List<PdfPoint>> lines, double halfWidth, double halfHeight)
    {
        List<PdfPoint> line = lines.MaxBy(Length)!;
        PdfPoint middle = Halfway(line);
        double gap = RouteHalfWidth + 2;
        foreach ((double dx, double dy) in new[] { (halfWidth + gap, 0.0), (-halfWidth - gap, 0.0), (0.0, -halfHeight - gap), (0.0, halfHeight + gap),
                     (halfWidth + gap, -halfHeight - gap), (-halfWidth - gap, -halfHeight - gap), (halfWidth + gap, halfHeight + gap), (-halfWidth - gap, halfHeight + gap) })
            yield return new Spot(new PdfPoint(middle.X + dx, middle.Y + dy), 0, halfWidth, halfHeight);
    }

    private static double Length(List<PdfPoint> line) => line.Zip(line.Skip(1), Distance).Sum();

    private static PdfPoint Halfway(List<PdfPoint> line)
    {
        double left = Length(line) / 2;
        for (int i = 1; i < line.Count; i++)
        {
            double piece = Distance(line[i - 1], line[i]);
            if (left <= piece && piece > 0)
                return new PdfPoint(line[i - 1].X + (line[i].X - line[i - 1].X) * left / piece, line[i - 1].Y + (line[i].Y - line[i - 1].Y) * left / piece);
            left -= piece;
        }
        return line[^1];
    }

    private static IEnumerable<(PdfPoint Centre, double Angle)> Places(List<List<PdfPoint>> lines, double length)
    {
        List<(PdfPoint From, PdfPoint To, double Length)> runs = lines.SelectMany(Runs).Where(run => run.Length >= length).OrderByDescending(run => run.Length).ToList();
        foreach ((PdfPoint from, PdfPoint to, double runLength) in runs)
        {
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            if (angle > Math.PI / 2 || angle < -Math.PI / 2)
                angle = Math.Atan2(from.Y - to.Y, from.X - to.X);
            double room = (runLength - length) / 2;
            int steps = Math.Min(MostPlacesARun, (int)(room / PlaceStep));
            foreach (double shift in Enumerable.Range(0, steps * 2 + 1).Select(Shift))
            {
                double t = 0.5 + shift / runLength;
                yield return (new PdfPoint(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t), angle);
            }
        }
    }

    private static IEnumerable<List<PdfPoint>> WalkedThrough(IEnumerable<List<PdfPoint>> walkThroughs, List<List<PdfPoint>> delivered)
    {
        HashSet<(long, long, long, long)> drawn = [.. delivered.SelectMany(Segments)];
        foreach (List<PdfPoint> line in walkThroughs)
        {
            List<PdfPoint> run = [];
            for (int i = 1; i < line.Count; i++)
            {
                if (drawn.Add(Segment(line[i - 1], line[i])))
                {
                    if (run.Count == 0)
                        run.Add(line[i - 1]);
                    run.Add(line[i]);
                    continue;
                }
                if (run.Count >= 2)
                    yield return run;
                run = [];
            }
            if (run.Count >= 2)
                yield return run;
        }
    }

    private static IEnumerable<(long, long, long, long)> Segments(List<PdfPoint> line) => line.Zip(line.Skip(1), Segment);

    private static (long, long, long, long) Segment(PdfPoint a, PdfPoint b)
    {
        (long, long) Rounded(PdfPoint point) => ((long)Math.Round(point.X * 10), (long)Math.Round(point.Y * 10));
        ((long, long) first, (long, long) second) = (Rounded(a), Rounded(b));
        return first.CompareTo(second) <= 0 ? (first.Item1, first.Item2, second.Item1, second.Item2) : (second.Item1, second.Item2, first.Item1, first.Item2);
    }

    private static double Shift(int place)
    {
        int step = (place + 1) / 2;
        return step * PlaceStep * (place % 2 == 0 ? 1 : -1);
    }

    private static IEnumerable<(PdfPoint From, PdfPoint To, double Length)> Runs(List<PdfPoint> line)
    {
        for (int first = 0; first < line.Count - 1; first++)
        {
            double heading = Math.Atan2(line[first + 1].Y - line[first].Y, line[first + 1].X - line[first].X);
            int last = first + 1;
            while (last + 1 < line.Count && Bend(heading, Math.Atan2(line[last + 1].Y - line[last].Y, line[last + 1].X - line[last].X)) <= MostBend)
                last++;
            yield return (line[first], line[last], Distance(line[first], line[last]));
        }
    }

    private static double Bend(double a, double b) => Math.Abs(Math.IEEERemainder(a - b, Math.PI * 2));

    private static double Distance(PdfPoint a, PdfPoint b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    private static List<PdfPoint> Sampled(IEnumerable<List<PdfPoint>> lines)
    {
        List<PdfPoint> points = [];
        foreach (List<PdfPoint> line in lines)
            for (int i = 1; i < line.Count; i++)
            {
                int pieces = Math.Max(1, (int)Math.Ceiling(Distance(line[i - 1], line[i]) / SampleEvery));
                for (int piece = 0; piece <= pieces; piece++)
                    points.Add(new PdfPoint(line[i - 1].X + (line[i].X - line[i - 1].X) * piece / pieces, line[i - 1].Y + (line[i].Y - line[i - 1].Y) * piece / pieces));
            }
        return points;
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
        page.Line([new(x + 118, y), new(x + 140, y)], Ink, 1.3, WalkThroughDash, round: false);
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
        double attributionY = page.Height - Margin + 2;
        foreach (string line in page.Wrap(day.Attribution, PdfFont.Regular, 6.5, width).Take(2))
        {
            page.Text(left, attributionY, line, PdfFont.Regular, 6.5, Ink2);
            attributionY += 8;
        }
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
