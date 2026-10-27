# Site map

Status: draft, 2026-10-26. Extends [SPEC.md](../../SPEC.md). First used by
[Life Groups](life-groups.md); meant for the whole app.

## Why it's app-wide

The church shares its site with the school, so "where is it?" comes up everywhere: a Life Group's room,
an event in the Fellowship Area, the nursery for a visitor, the accessible toilet. Today each answer is
a printed map someone redraws. The app has one interactive map, and any feature can mark places on it.

## The map is drawn, not a picture

The site plan's Illustrator file (`LIFE Groups_Room location map_2026_Round 1.ai`) is PDF-compatible
and has named layers: `Map Content` (ground, paths, buildings), `Building Labels` (labels, numbers,
icons), `Layer 24` (the Life Groups legend), plus hidden ones: `Room Numbers`, `Trees` and the
`Proposed …` building layers.

- **The base map is an SVG** exported from `Map Content` alone, with no labels, numbers or legend. A
  first cut was made on 2026-10-26: 55 KB, all vector, with this round's eight coloured rooms set back
  to the grey of the other buildings.
- **Every place you can point at is a tagged shape:** `data-space="library-east"` on its roof and
  walls (`data-part="top"` / `"side"`). The app colours, outlines and labels a place by its tag, so a
  new round's rooms are data, not a redrawn image. The first cut tags only the eight Life Groups rooms,
  with placeholder names; every building still needs a tag.
- **Labels and icons are drawn by the app,** from place records, in the app's type and colours. They
  follow light and dark mode and the text-size setting, which the baked-in labels couldn't.
- **The map is data, not code** (§4): a record in a `site.map` feed (a version and the SVG), so a new
  building or a re-tag needs no release. It fits the 64 KB record limit (§5) with room to spare.
  Updating it: re-export from Illustrator with object names as IDs, or run the same layer-and-tag script
  that made the first cut; then upload it in the admin panel.

## Places

A **place** is anything worth finding: a building, a room, toilets, an entrance, parking. Each has a
name ("Library"), an optional short name and building code from the printed key (the `W`, `H`, `B`,
`S` letters), a kind, the `data-space` tag(s) it covers or a point for small things, and access notes
("lift on the east side"). Places are in a `site.places` feed, edited by staff.

## How features use it

Features never reference each other (§8), so the map lives in its own module, `Features/Site`, and
others reach it through two small interfaces in `Shared`:

- **`PlaceId`** goes in another feature's records: a group's room, an event's venue. A host without
  the Site module still shows the place's name, which the feature stores alongside.
- **An overlay** is what a feature asks the map to show: places with a colour, a number and a label.
  Life Groups passes this round's rooms in group colours, numbered as on the flyer. Events would pass
  one venue.

Shared components: **`SiteMap`** (the whole map with pan and pinch-zoom, an optional overlay, and tap a
place for a sheet saying what's there now; or, given a place, a small static crop around it for a
detail page) and **`PlaceChip`** (name, number and colour; tap to open the map). The map is inline SVG
in Blazor, with a few lines of JS for pan and zoom, and no map library. That is AOT-safe and needs no
network once synced.

## In the design spike

`spikes/app-poc` has the map working: `wwwroot/data/site-map.svg` is the first cut,
`Components/SiteMap.razor` draws it with an overlay (inline SVG, room colours as CSS on the tagged
shapes, badges that stay the same size as you zoom), and `gsbc.map` in `native.js` does pan, pinch,
double-tap zoom and tap. Places and their coordinates are in `Data/SitePlan.cs`, read from the .ai
file's label and room positions. Dark mode recolours the map's greys and greens with CSS on their fill
values, so the SVG itself stays as exported.

## Not now

- **"You are here"** from GPS: the site is small and phone GPS is about ±5 m, so it may mislead between
  adjacent rooms. Worth a try on site later.
- **Indoor detail:** room-level floor plans inside large buildings. The plan's hidden `Room Numbers`
  layers suggest the data exists if wanted.
- **Proposed buildings:** the plan has layers for them; switching the map over when they open is just
  a new map record.
