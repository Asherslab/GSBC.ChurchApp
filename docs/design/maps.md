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
