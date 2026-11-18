# Maps

Detail behind [DESIGN.md](../../DESIGN.md) for the outreach base map, on the phone and in the admin panel. Read
DESIGN.md first; its rules win. Background: [outreach.md](../features/outreach.md), *The map's look*.

## The base map

`mapStyle({ tiles, glyphs, scheme, look, attribution })` in Platform.Ui `wwwroot/map/map-style.js` builds the
MapLibre style for one region's map file (PMTiles, OpenStreetMap in the Protomaps schema). `scheme` is `light` or
`dark` and follows the person's light or dark choice. It ships with the app and the admin panel, so the map works
offline. Every map uses it; a screen never writes its own style.

**What Bounds a Walk.** The map shows why a walk stops. Parks, places that aren't homes (schools, shops, hospitals,
industry, sports grounds) and main roads read at a glance. The streets where people live stay white and quiet. Grey
category base maps can't explain a short walk, so we don't use one.

## Palette (`pockets`, the default)

| Part | Light | Dark |
|---|---|---|
| Land | `#f7f4ee` warm bulletin ground | `#141d31` night |
| Water | `#cde2e3` | `#123247` |
| Parks, reserves, sports fields, cemeteries | `#d9e6cf` sage | `#1c3528` |
| Not homes (schools, shops, industry, hospitals) | `#ebe2d2` beige | `#2e2c2f` |
| Buildings, from zoom 14 (fading in to 15) | `#ece6dc`, edge `#e0d8cb` | `#1a2337`, edge `#1f293f` |
| Homes' streets | `#ffffff`, sand edge `#ddd5c7` | `#26324b`, edge `#1f293f` |
| Main roads | `#fbf6ee`, firm brown edge `#a99c88` | `#283450`, edge `#3b4763` |
| Paths (dashed) | `#c8bfae` | `#2f3a52` |
| Street names | `#545e70` (ink-2) | `#b2b8c5` (night ink-2) |
| Suburb names | `#626a79` (ink-3) | `#9ca2b1` (night ink-3) |
| Label halo | the land colour | the land colour |

The map's colours live in `map-style.js`, not in the theme: staff styling doesn't change the map.

## What is drawn, and what is left out

- **Drawn:** land, water and waterways, parks, places that aren't homes, buildings, streets, main roads, paths, street
  names and suburb names.
- **Left out on purpose:** POI icons, sprites and road shields. Nothing on the map competes with a walk.
- **Hidden:** sidewalks and crossings. They double every street and add nothing to a walk.
- **Quiet:** service roads and car-park aisles draw in the street-edge colour only, from zoom 14, so they don't read as
  streets with homes.
- **From zoom 15:** paths, dashed and thin.
- Main roads draw over homes' streets, edge first, so a main road always reads as a boundary.

## Labels

- **Street names:** Montserrat 400, along the line, from zoom 14: 11px at 14, 13px at 16, 15px at 18. A 1.6px halo in
  the land colour.
- **Suburb names:** Montserrat 600 in capitals, letter-spacing 0.12em, 11px at zoom 10 to 14px at 14, gone from
  zoom 15, when the streets take over.
- No other fonts. The map's glyphs are the brand's own Montserrat, never a font server's.

## What screens draw on top

The base map leaves the colour to the overlays. Each screen draws them the same way:

- **The walk:** deep aqua (`#00776d`) by day, bright aqua (`#2ec4b6`) at night, with a white edge.
- **Streets walked recently:** ink-3 (`#626a79`) at 45%.
- **You:** the orange marker (`#e7ae74`).
- **Walked through, no letterboxes:** a stretch a walk or route only passes through, to get between its parts, is a
  row of round dots in the walk's or route's colour, under its solid lines, so a whole route never reads as broken
  (owner's pick, 2026-11-17, #125, `.impeccable/mocks/route-map-feel/`). Printed maps draw the same dots in black; the
  planner's key and the printed key call them "Walked through, no letterboxes".
- **Attribution under every map:** the region's sources (© OpenStreetMap contributors, and the address sources it
  used), passed as `attribution`, on one line, cut short with an ellipsis, with an ⓘ that shows them all
  (2026-11-17).

## Switching look

Three looks are built: `pockets` (the default, *What bounds a walk*), `sand` (*One calm sand page*: everything sand
with white streets, no parks or buildings) and `homes` (*The houses themselves*: buildings in beige on a near-white
ground). Pass `look` to `mapStyle`, or change `defaultLook` to move every map. The decision page in
`.impeccable/mocks/outreach-map/` still shows all three.

## Glyphs

`wwwroot/map/glyphs/` holds Montserrat 400 (`montserrat-regular`) and 600 (`montserrat-semibold`) as map glyphs, Latin
and punctuation only, as the brand fonts are subset. They are made from the fonts in `wwwroot/fonts` by
`tools/outreach/map/glyphs.mjs` (`npm install`, then `node glyphs.mjs` in `tools/outreach/map`). Run it again whenever
those fonts change; never edit the files by hand.

## The component

`MapView` (Platform.Ui `Maps/`) is the one map on both sides: MapLibre in the web view over a region's PMTiles file, with
`mapStyle` and the overlays below. Pass the map file as `MapSource.KeptFile(id, attribution, url)` on the phone (the file kept
on the phone, read by byte ranges through the native layer, so it works offline; until that copy is whole and of the
same `url`, the map reads the same file by range from the media host, so the streets in view show while the region
downloads, #112) or `MapSource.Url(url, attribution)` in
the admin panel; `MapSource.Urls(urls, attribution)` draws several map files as one map (each base layer once per file,
in order, so the 2 km overlaps meet without a seam). It follows light and dark by itself.

- **Lines** (`MapLine`, by `MapLineStyle`): `Walk` (deep aqua, white edge, arrows along it from zoom 15), `WalkDone` (a walk's
  part already covered, ink-3), `Walked` (streets walked recently, ink-3 at 45%), `Connector` (the walk's stretches with no letterboxes: round
  dots in the walk colour, 3.5px at zoom 13 to 10px at 18, two widths apart, under the walk), `LongWalk` (dotted, with
  its label, such as "290 m, no letterboxes"), `Route` (a planned route in its own `Colour`), `RouteWalkedThrough`
  (a route's stretches with no letterboxes, the same round dots in the route's `Colour`, under its `Route` lines).
  Thousands of lines that change a block at a time (the admin coverage map, one block per region) go in `LineSets`
  (`MapLineSet`, by key): the map sends a set only when its list changes and adds or removes just that set, so a
  redraw never sends every line again.
- **Areas** (`MapArea`): a polygon in its colour at 12% with an edge. `Outline` drops the fill and keeps the edge (a map
  area's squares with no homes or still to add, and the hovered square in ink).
- **Markers** (`MapMarker`): `Start`, `Pin`, `Meeting` (with a label), and from zoom 15 `NoJunkMail`, `DontLetterbox`, `Skip`,
  `Check` (small dots; the first two labelled from zoom 17), and `Match` (a numbered accent pin with a page ring, for
  search matches). `Start`, `Pin` and `Meeting` dots and every `MapPin` hold their room on the map, so the base map's
  labels give way to them.
- **You** (`MapYou`): the orange dot with its accuracy ring. With `ShowsHeading` (the walk and Your walk), a soft orange
  fan from the dot shows which way you face (owner's pick, 2026-11-17, #115, `.impeccable/mocks/outreach-heading/`): the
  direction you're moving once you've gone 6 m or further than the fix's accuracy (for 10 s after), otherwise the
  phone's compass, 50° wide and 104px long when it's within 20°, widening to 110° and shortening to 86px as it gets
  less sure, fading in orange to nothing, gone past 60°. It sits under the drawn lines, the dot on top. The compass is
  read natively on iOS and needs no permission; iOS's calibration prompt is turned off. Android has no compass yet, so
  it shows the fan only while you move.
- **Where it looks:** `Frame` fits the map to a box (`StartPadding` keeps room on the left for a tool rail), and again whenever the box changes, unless someone has dragged the
  map. Framing also leaves room on either side for what covers the map: the overlay's inset (a full map's floating
  panel) and any tall element marked `data-map-cover` (a tool rail), and the map follows when that room changes. With `Follow`, a screen gives a `Frame` around you and what's next to you (the walk: you, the street you're on and
  the next one, at street level), so the map moves with you; with no `Frame`, it centres on you at zoom 17. The first drag
  stops following (`OnStoppedFollowing`) and setting `Follow` again brings it back. `Centre` and `Zoom` place a map with
  neither. `Interactive="false"`
  makes a still map.
- **Reach** (`MapReach`): where the maps end. Outside its `Covered` squares the map is hatched (the land colour with
  ink-3 stripes at 40%) under a thin ink edge, or veiled when there's a `Backdrop` (below); `Others` and `Notes` mark
  squares not built yet; `Clear` lifts the veil off squares being looked at (a map area being added); `Limit` stops
  panning. `Outside` is shown in a card top centre while the view is mostly (three quarters) beyond the squares, and
  `OnMoved` gives the view after each move.
- **Notes** (`MapMarkerKind.Note`): text only, ink with a halo in the surface colour, 12px, wrapping at 9 ems (a
  square's letterbox estimate, "No homes", "Outside Queensland").
- **Backdrop** (`MapBackdrop`, admin panel only): a low-zoom map file under the regions, drawn in the same `mapStyle`
  without street names. See *Past the edge of our maps*.
- `OnTap` gives the point tapped (dropping a pin); `Overlay` holds a control over the map (*Follow me*). Its
  attribution sits under it.
- **Gestures** (2026-11-17, #126): pinch (Safari's `gesturestart`/`gesturechange`), ctrl+wheel and scroll over
  anything laid on a map (chips, discs, pins, cards, menus, the key, the full map button) move the map as over the map
  itself, so the page never zooms; something that scrolls itself (a card's body, a menu) still scrolls until it reaches
  its end. Every interactive `MapView` does it, over its map desk or workspace.
- **No text selection:** the map, its overlay and the full map button don't select as text when dragged (#127); a
  `MapCard`'s text, the outside note and fields stay selectable.
- **The top-right corner:** a `MapWorkspace` sets `--map-corner-top` and `--map-corner-right` so a card or hover facts
  sit clear of the full map button (beside it, below *Leave full map* once full), and `--map-top-end` where a strip
  along the top must stop.

The libraries are vendored in `wwwroot/map/vendor` by `tools/outreach/map/vendor.mjs` (`npm install`, then
`node vendor.mjs`), so the app never fetches them.

## Past the edge of our maps (admin, 2026-11-16)

The admin panel's outreach maps draw all of Australia under the built regions, so staff can see where to add a map area
(owner's pick on the decision page, `.impeccable/mocks/outreach-overview/`). The ground says where walkers have maps:

- **Where walkers have maps:** the regions as they are, clear.
- **Overview only:** everywhere else in Queensland sits under a flat veil (`--map-veil`), the overview's roads and town
  and suburb names showing through. Out here the map zooms in only a little past the overview's own detail (suburbs);
  inside the built squares it goes to the deepest zoom.
- **Outside Queensland:** the veil at 80% again on top, with the reach hatch's stripes (no ground; fainter at night).
  Areas can't be added there, and a square's note says "Outside Queensland".
- **Waiting to build** and **Switched off** keep their dashed looks over the veil.
- **The key** (`OutreachMapKey`): a card on the page colour with the map's float shadow, "Where walkers have maps", one
  swatch per state (22 × 14, square corners, a faint ink-3 edge), only the states in play. It shows while the view
  reaches past the built squares, bottom left in the map's corner, above the coverage legend when that shows.
- **The note** (`OutreachMapOutside`): top centre, below *Layers* when the window is under 1280px. With the overview it
  says "Past the edge of our maps", what the open job does where walkers have maps, and that out here the map zooms in
  only as far as suburbs, with *Add a map area* for those who look after maps. Map areas shows no note: the veil and
  the key say it. Without the overview the map is held to our regions, hatched beyond, and the note says "No map here
  yet".

## Drawing on the map

`MapSketch` (Platform.Ui `Maps/`, with `wwwroot/map/map-tools.js`) sits beside a `MapView` (`Map="@_map"`) and adds what a
planner draws, on its own layers over the overlays:

- **`Tool`** (`MapSketchTool`): `Polygon` (click the corners; click the first corner, double-click or press Enter to close;
  Backspace takes the last corner back; Escape starts again), `Lasso` (drag a loop), `Line` (drag a line, for cutting),
  `Brush` (drag along streets; `OnDrawing` reports the stroke as it goes, drawn wide in `BrushColour`), `Pin` (one click).
  `None` gives the map back its panning and reports the pointer (`OnHover`, `OnHoverEnded`). A drag tool holds the map
  still while drawing; scroll and the zoom buttons still zoom.
- **`OnDrawn`** gives the finished stroke's points (`MapSketchStroke`); what they select is the screen's business.
- **`Labels`** (`MapLabel`): a capsule at a point, such as "Route 8 keeps these": `Ring` (the page colour, ringed in
  the label's colour, ink text) or `Filled` (the colour with white text, for the chosen one). Below zoom 15 a label shows
  its `Short` text, from 15 its full text. A label with a `Badge` is a disc instead (2026-11-17, owner's pick on
  `.impeccable/mocks/route-map-feel/`): a 24px circle in the label's colour with a 2px surface edge and the badge (a
  route's number) in white Montserrat 600 at 12px, its text to the right on a quiet plate (the surface with a hairline;
  `Filled`, the colour with white text): its `Short` text below zoom 14, its full text from 14, and nothing when the text
  is empty. Every label draws and no two
  ever overlap: highest `Order` first, each takes the first of its `Places` (spots on its own route, nearest the
  middle first), then a spot nudged a little way off, that touches no other label, stays on the map and stays clear of
  anything marked `data-map-cover` (the planner's rail, chips and chosen-route bar) and the base map's place labels ("Meet here"). Labels are placed again as the map zooms.
  `Quiet` labels aren't moved: they stay on their point on a layer of their own beneath, and one that would overlap
  another is hidden rather than drawn over it (a map area's square names, place names), so a screen can name many
  things where they fit (2026-11-15). Every colour a label uses holds 4.5:1 against white.
- **`Preview`**: lines drawn over everything while a gesture is under way (a cut's pieces, a brush's stretch).

## Chips on the map

A chip is a control on the map: a `map-chip` `Menu` (a pill on the page colour, on the map's float shadow;
icon, name, chevron). One tied to a single point of the map rides on it in a `MapPin`. Chips for whole areas line up in
one `MapChipStrip` (Platform.Ui `Maps/`, a labelled toolbar) along the top of the map, from the tool rail
(`--map-pin-start`) to the top-right button (`--map-top-end`), wrapping onto a second line rather than scrolling (a
scrolling strip clips the chips' menus); an open menu lifts the strip above the map's other controls (owner's pick, 2026-11-17,
`.impeccable/mocks/route-map-feel/`). A chip for one route (`map-chip--route`, with `--route` and `--number`) is ringed
in the route's colour; on a map under 1100px wide it shows the route's numbered disc instead of its icon and name.
Mark each chip `data-map-cover` so labels keep off it.

## Coverage and dashed areas (reports station, 2026-11-15)

- `MapLineStyle.Covered` with `MapLine.Age` (0 today, 1 the rest period over) draws a street side in the walk colour
  fading to `--map-land`, the base map's land colour in each scheme, so a side at the end of its rest period disappears
  into the map.
- `MapArea.Dashed` draws the edge dashed: areas outreach skips (in danger) and a drawn selection (in ink).
- `MapLineStyle.Chosen` draws a chosen street as an ink outline (light ink at night) with the page colour inside, under
  the coverage, so the street stands out and its own coverage still shows (owner's decision page, 2026-11-15,
  `.impeccable/mocks/outreach-polish/`).
- `Colour` on a line or area may be a theme token name (`--danger`, `--primary`); the map reads it when it draws.
