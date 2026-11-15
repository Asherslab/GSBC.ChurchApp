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
- **Attribution under every map:** the region's sources (© OpenStreetMap contributors, and the address sources it
  used), passed as `attribution`.

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
`mapStyle` and the overlays below. Pass the map file as `MapSource.KeptFile(id, attribution)` on the phone (the file kept
on the phone, read by byte ranges through the native layer, so it works offline) or `MapSource.Url(url, attribution)` in
the admin panel. It follows light and dark by itself.

- **Lines** (`MapLine`, by `MapLineStyle`): `Walk` (deep aqua, white edge, arrows along it from zoom 15), `WalkDone` (a walk's
  part already covered, ink-3), `Walked` (streets walked recently, ink-3 at 45%), `Connector` (dashed), `LongWalk` (dotted, with
  its label, such as "290 m, no letterboxes"), `Route` (a planned route in its own `Colour`, with a label).
- **Areas** (`MapArea`): a polygon in its colour at 12% with an edge. `Outline` drops the fill and keeps the edge (a map
  area's squares with no homes or still to add, and the hovered square in ink).
- **Markers** (`MapMarker`): `Start`, `Pin`, `Meeting` (with a label), and from zoom 15 `NoJunkMail`, `DontLetterbox`, `Skip`,
  `Check` (small dots; the first two labelled from zoom 17).
- **You** (`MapYou`): the orange dot with its accuracy ring.
- **Where it looks:** `Frame` fits the map to a box, and again whenever the box changes, unless someone has dragged the
  map. With `Follow`, a screen gives a `Frame` around you and what's next to you (the walk: you, the street you're on and
  the next one, at street level), so the map moves with you; with no `Frame`, it centres on you at zoom 17. The first drag
  stops following (`OnStoppedFollowing`) and setting `Follow` again brings it back. `Centre` and `Zoom` place a map with
  neither. `Interactive="false"`
  makes a still map (*Around you*).
- `OnTap` gives the point tapped (dropping a pin); `Overlay` holds a control over the map (*Follow me*). Its
  attribution sits under it.

The libraries are vendored in `wwwroot/map/vendor` by `tools/outreach/map/vendor.mjs` (`npm install`, then
`node vendor.mjs`), so the app never fetches them.

## Drawing on the map

`MapSketch` (Platform.Ui `Maps/`, with `wwwroot/map/map-tools.js`) sits beside a `MapView` (`Map="@_map"`) and adds what a
planner draws, on its own layers over the overlays:

- **`Tool`** (`MapSketchTool`): `Polygon` (click the corners; click the first corner, double-click or press Enter to close;
  Backspace takes the last corner back; Escape starts again), `Lasso` (drag a loop), `Line` (drag a line, for cutting),
  `Brush` (drag along streets; `OnDrawing` reports the stroke as it goes, drawn wide in `BrushColour`), `Pin` (one click).
  `None` gives the map back its panning and reports the pointer (`OnHover`, `OnHoverEnded`). A drag tool holds the map
  still while drawing; scroll and the zoom buttons still zoom.
- **`OnDrawn`** gives the finished stroke's points (`MapSketchStroke`); what they select is the screen's business.
- **`Labels`** (`MapLabel`): a capsule at a point, such as a route's "4 · 137 · 74 min": `Ring` (the page colour, ringed in
  the label's colour, ink text) or `Filled` (the colour with white text, for the chosen one). Below zoom 15 a label shows
  its `Short` text, from 15 its full text; every label draws, highest `Order` on top. `Quiet` labels draw on a layer of
  their own beneath, and one that would overlap another is hidden rather than drawn over it (a map area's square names,
  place names), so a screen can name many things where they fit. Every colour a label uses holds 4.5:1 against white.
- **`Preview`**: lines drawn over everything while a gesture is under way (a cut's pieces, a brush's stretch).

## Coverage and dashed areas (reports station, 2026-11-15)

- `MapLineStyle.Covered` with `MapLine.Age` (0 today, 1 the rest period over) draws a street side in the walk colour
  fading to `--map-land`, the base map's land colour in each scheme, so a side at the end of its rest period disappears
  into the map.
- `MapArea.Dashed` draws the edge dashed: areas outreach skips (in danger) and a drawn selection (in primary).
- `Colour` on a line or area may be a theme token name (`--danger`, `--primary`); the map reads it when it draws.
