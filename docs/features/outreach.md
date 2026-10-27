# Outreach

Status: draft, 2026-10-27. Extends [SPEC.md](../../SPEC.md). A POC is in the PWA spike
(`spikes/app-poc`, More → Outreach and the Hub tile).

## What it is

A church member asked for a way to make outreach seamless: put in an address and how far you want to
walk, get a route that covers both sides of every street, see how many tracts to take, and keep it
central so the same houses aren't done twice in a week.

Outreach here means **letterbox drops** of tracts, in bundles of 50. Doorknocking may come later; the
design leaves room for it (a slower pace, fewer letterboxes per hour) but doesn't build it.

## What people can do

Three ways in, each a different thing (decided 2026-10-27):

1. **Walk now (casual, the default).** "How long have you got?" 30 min, 1 hr, 2 hr or a distance. The
   app makes a loop there and then, from where you are, from church, or from a point you tap. It
   shows the distance, letterboxes, bundles to take and roughly how long, then Start.
2. **Outreach days (ready-made).** The church holds them at a set day and time (for example, Saturday
   8:30–11:30). Staff prepare the routes beforehand in the admin panel. Walkers **claim** a route:
   several people can claim the same one, together or separately, and others see it's taken
   ("Sam + 2"). An outreach day is a **time window, not a place**: someone who walks from home
   during it counts toward the day.
3. **Areas (more effort).** The church's historical outreach areas, about a bundle each, freshest
   first, with when each was last done. You can take one (a claim with an end date).

All three end on the same **walking screen**: the map with your position, one side of one street at
a time ("Kingfisher St, this side · 14 letterboxes") with Done, and a **No junk mail** button. Finishing
records the streets as covered.

The entry point is a Hub tile (the Hub is the favoured home, 2026-10-27): "Got an hour? Walk from your
door" normally; "Outreach is on till 11:30 · 5 of 9 routes claimed" during an outreach day.

## How a route is made

The POC runs this on real Albany Creek data. Validated 2026-10-27.

- **Streets** come from OpenStreetMap. **Letterboxes** come from address points: in Queensland, the
  state's open Addresses layer (every address with its street, unit number and location, CC BY 4.0);
  nationally, G-NAF. Each address is placed on the nearest segment of its own street and on a side
  of it, so every street side has a count. Units count as letterboxes too (faster to fill: about 3 s
  each against 8 s for a house). In the POC area, 95% of 10,784 addresses place cleanly.
- **Which streets:** grow outward from the start along residential streets until the time or distance
  is used up, skipping streets walked in the last 7 days. **Arterials** (Old Northern Rd, Albany
  Creek Rd) are never delivery streets: they can be walked along to get somewhere, but nobody is sent
  to letterbox a main road.
- **Both sides of the road is what makes the maths easy.** Walking every chosen street once per side
  always forms a closed loop (each street becomes two one-way kerbs, so every corner has as many ways
  in as out). The walk prefers the sharpest left turn at each corner, which traces each block's kerb
  without crossing; cul-de-sacs become up one side and back the other. Loops are spliced where they
  meet, which is where you cross.
- **No empty walking** (2026-10-27): a street with no letterboxes (all businesses, a park edge, a main
  road) is never a delivery street. It's walked only as a short connector between parts of a route,
  drawn dashed, counted in time but not letterboxes. Long empty walks are avoided with hard limits
  (POC values, to tune): Walk now never adds a street that needs more than 150 m of empty walking in
  one stretch; the splitter joins streets only through connectors up to 125 m, so a cut-off group
  becomes its own small route rather than a long empty walk; a route with more than 250 m of
  connectors in total, or one leg over 150 m, is flagged for review. The walk from the meeting
  point to a route's start is exempt.
- **Time:** walking at 4 km/h plus the per-letterbox time above. Around Albany Creek that is about 38
  letterboxes per km walked, so an hour is roughly 3 km and 100 letterboxes.
- **Bundles:** letterboxes ÷ 50, rounded up, plus a spare when the remainder is close.

### Homes only: skipping businesses

Businesses, schools, parks and vacant land are skipped; only homes count as letterboxes. The church
is near the Moreton Bay–Brisbane boundary and works both (2026-10-27).

- **Moreton Bay:** the council's open planning-scheme layer gives every lot its **actual use** (the
  rates land-use category: Single Unit Dwelling, Residential CTS, Shops, Professional Offices…), keyed
  by lot and plan, which the address points carry. Around the church 86% of addresses join exactly
  by lot; the rest (mostly units in a scheme) join by the lot polygon they sit in.
- **Brisbane:** City Plan 2014 zoning only (no actual use, no lot key), joined by the zone polygon the
  address sits in: 99% of 2,930 Bridgeman Downs and Aspley addresses land in a zone. Residential zones
  are delivered; centres, industry, community facilities, open space and sport are skipped. A shop in
  a residential zone isn't caught, and homes in a centre or mixed-use zone are skipped; mixed use is
  shown as "check" rather than skipped outright.
- **Delivered:** single dwellings, unit and townhouse schemes, flats, rural residential, and
  **retirement villages** (most are fine with it, 2026-10-27).
- **Skipped:** shops, offices, centres, service stations, hotels and clubs, industry, schools and
  childcare, churches, hospitals, parks, vacant land, utilities. The walking screen says so ("Skip:
  shop") and routes show "skips 6 businesses".
- **Elsewhere:** other councils publish differently, so each is its own import; the national fallback
  is ABS mesh block categories (residential, commercial, industrial…), coarse but enough to skip a
  shopping centre.

### Data sources and licences

Checked against each publisher's own metadata, 2026-10-27:

| Data | Publisher | Licence |
|---|---|---|
| Address points | Queensland Department of Resources | CC BY 4.0 |
| Lot land use and zones | Moreton Bay City Council ("MBRC 2020") | CC BY 4.0 |
| City Plan 2014 zoning | Brisbane City Council | CC BY 4.0, no access or use limitations |
| Streets | OpenStreetMap contributors | ODbL (attribution; the derived street graph is shared under ODbL) |
| Map tiles | OpenFreeMap (OpenMapTiles schema, OSM data) | Free public instance, no keys or limits; attribution required |

The app shows the attributions under the map and in More → About.

## Outreach days

- **Made in the admin panel** (decided 2026-10-27): a day (date, time window, meeting point, note) and
  its routes. Staff plan these carefully; the app only offers them. Routes can be drawn street by
  street, or drafted by splitting an area (below), and are always reviewed by hand before they're
  published.

### Splitting an area into routes (admin)

Asked for 2026-10-27: draw a polygon and get it split into sections for staff to review.

1. **Draw the area** on the map; set the meeting point (church by default) and a target per route in
   letterboxes, bundles or minutes.
2. **Streets in it:** every street side whose midpoint is inside the polygon, minus main roads. Sides
   walked recently are shown greyed and can be left out.
3. **Split:** the number of routes is the total over the target, rounded. Seeds are spread across the
   area (weighted by letterboxes), then all sections grow at once: the lightest section always takes
   its next adjacent street, so each stays one connected piece and sizes come out even. A last pass
   moves boundary streets between neighbours to even them further and keep shapes compact.
4. **Loops:** each section gets its both-sides loop (above), starting from its point nearest the
   meeting point; the walk there and back is added to its time.
5. **Review, on the map:** each section is coloured, with a map label giving its number, letterboxes
   and time; hovering shows its stats and any "Needs review" beside it, and a selected section is
   drawn thicker with a halo while the rest fade. Staff edit directly on the map with a tool rail:
   select (with Merge, Rename, Lock and Unassign floating beside the selection, and renaming inline on
   the label), scissors (draw a line across a section to cut it in two; streets go to the side holding
   most of their length, with both halves and their letterboxes previewed while drawing), brush (paint
   streets into a section), lasso (loop streets into a section or a new one), eraser (leave streets
   out that day) and a draggable start pin. Undo and redo cover every edit; Rebalance evens out the
   unlocked sections; locked sections are left as they are by Regenerate, Rebalance and the tools.
   Streets with no letterboxes stay connectors; an edit that breaks a section apart keeps it as one
   and flags it "Needs review: in pieces". A side list summarises the sections and offers the same
   actions as a fallback. Then publish them as the day's routes. Nothing is published without review.

The split runs in the admin panel (Blazor, not AOT) using the same shared route engine; a polygon and
its draft sections are admin data, never sent to phones until published.
- **Claims:** a first name, asked once on the phone and kept there; the claim is an App write (§6).
  Others see first names on a route ("Sam, Ruth + 1"). Claims are deleted when the day ends.
- **During the window** the Hub tile and the Outreach page lead with the day. A casual walk started in
  the window is tagged to the day and counts toward its total.

## Areas

- Meant to be the church's **historical outreach areas** (2026-10-27): how the church already divides
  the suburb. How they're made is to be reviewed with the people who run outreach.
- The POC cuts sample areas automatically (connected streets, about 55 letterboxes each, main roads
  excluded) and gives them sample "last done" dates. That is placeholder data, not a proposal.
- Taking an area is a claim with an end date (default the coming Sunday).

## What the church records

Decided 2026-10-27: streets walked and "No junk mail" letterboxes; nothing about the people who live
there.

- **Coverage:** per street side, the date it was walked and whether it was a day route, an area or a
  casual walk. No names. Coverage older than a year is aggregated away.
- **No junk mail:** a walker taps the button at a letterbox with the sign. The phone sends the
  address point it's nearest to on that side; the app then skips it and lowers that side's count.
  It's address-linked data, so: staff can see and clear flags in admin, a flag expires after
  **two years** unless re-confirmed (to decide), and flags are never shown with who made them.
- **Admin exclusion zones** (2026-10-27): staff draw an area no route may deliver to, with a reason
  (non-residential, a complaint, wrong data, unsafe, private estate, other). Streets inside are walked through, never
  delivered; existing day routes that touch it are flagged for review.
- **Admin exclusion addresses** (2026-10-27): for a phone call asking not to be letterboxed. Staff
  pick the address; it's skipped and its side's count drops. Optionally shown to walkers as an
  explicit "Don't letterbox" marker on the walking screen and map; the marker never shows why. The
  reason and the caller's details stay in admin only, and are kept as long as the request stands.
- **Reports from walkers** (2026-10-27): most walkers aren't staff, so anyone can report an area, a
  street or a letterbox: not homes, unsafe, a complaint, wrong on the map, or other, with a
  **mandatory note** and an optional photo. A report changes nothing until staff review it; then it
  becomes an exclusion (zone or address), a data fix, or is dismissed. The reporter sees "Sent for
  review". Photos are for staff only, should show the place not people (the app says so), and are
  deleted once the report is resolved.
- **No junk mail letterboxes** (decided 2026-10-27, above) stay in as the walker's quick, unreviewed
  flag; reports are the considered, reviewed route.
- **Respect the signs.** The letterbox distribution industry's code of practice asks distributors to
  honour "No junk mail" and "No advertising material" signs. Tracts arguably aren't advertising, but
  the church should honour the signs anyway, and the app assumes it does.

## Data and feeds

A feature module `Features/Outreach` (§8). Feeds (§5, §10):

| Feed | Records | Window | Notes |
|---|---|---|---|
| `outreach.streets` | The street graph with letterbox counts per side | Whole area, versioned | About 450 KB for Albany Creek; changes only when rebuilt from OSM and addresses |
| `outreach.days` | Outreach days and their routes | Next 4 weeks and the current day | Routes are lists of street sides |
| `outreach.claims` | Claims on day routes and areas | Live | First names only; deleted after the day or the claim's end date |
| `outreach.coverage` | Last walked date per street side | Rolling 8 weeks | Drives "avoid streets done this week" and areas' freshness |
| `outreach.areas` | Areas | All | Name, street sides, last done |
| `outreach.flags` | No junk mail points | All active | Points only; no reporter |
| `outreach.exclusions` | Exclusion zones and addresses | All active | Polygons and points, and whether to show "Don't letterbox"; never the reason |

Services, all gRPC (the AGENTS.md hard rule; nothing here needs an exception):

- `IOutreachService` (App write): `Claim`, `Unclaim`, `RecordWalk` (street sides, date, optional day),
  `FlagLetterbox` and `SubmitReport` (place, reason, note, and an optional photo sent as a
  client-streamed gRPC upload, so no non-gRPC path is needed). Rate-limited per install.
- `IOutreachAdminService` (Admin): days, routes, areas, flags (review and clear), exclusion zones and
  addresses, the report queue (review, turn into an exclusion or a fix, dismiss), drafting routes
  from a polygon (`DraftRoutes(polygon, target, meeting point)` returns sections to review), and
  rebuilding `outreach.streets`.
- **Building the street graph** is a server job, not a request path: fetch OSM and the address data,
  place letterboxes, publish a new version of `outreach.streets`. The POC's
  `tools/outreach/build_data.py` is that job in miniature.

## Where routes are made: on the phone

Recommended: the route engine is shared C# (AOT-safe) and runs **on the phone** from the synced street
graph. Routes appear instantly, work without signal, and need no server call. The server runs the same
engine in admin for day routes. The phone sends only what was walked.

## Maps

- **MapLibre in the web view** (the POC uses it), with vector tiles. It is the same in the app and the
  admin panel, and styles can match the brand. Native map SDKs (MapKit, Google Maps through a
  Capacitor plugin) would draw under the web view and split the UI; not worth it here.
- **Tiles:** the POC uses OpenFreeMap (free, no key; the phone fetches them directly, which is an
  outbound call to a third party, not one of our API paths). For the real app: OpenFreeMap or a paid
  provider with an SLA; or self-hosted vector tiles (one PMTiles file for the region). **Self-hosting
  means the gateway serves map data by HTTP range requests, which is a non-gRPC path: it needs the
  owner's explicit approval first** (AGENTS.md).
- **Offline:** before a walk, cache the tiles around the route (a few hundred KB) so the map works with
  no signal.

## Native, and what the PWA can't do

| Need | Real app | PWA preview |
|---|---|---|
| Your position while walking | Foreground location; background only if we add "street done" alerts, which App Store review scrutinises | Foreground only, while the screen is on |
| Screen stays on | Native keep-awake | Wake Lock API, where the iOS version supports it in Home Screen apps |
| Shared claims and coverage | The feeds above | Not possible without a server: kept on the phone, with sample claims and coverage |
| Offline map | Cached tiles | Online only |
| A nudge at each street end | Haptics | None on iOS |

## Safety

- Suggest going in pairs; outreach days should pair people.
- Daylight hours by default (the time choices stop offering long walks near dusk).
- Main roads are excluded from deliveries.
- The walking screen keeps the phone usable one-handed and glanceable; no typing while walking.

## Decided

- **Three ways in:** casual walk from your door (default), ready-made outreach-day routes, historical
  areas (2026-10-27).
- **Claims:** several people may claim the same route; others see it's taken (2026-10-27).
- **An outreach day is a time window:** walking from home during it counts (2026-10-27).
- **No sign-in for walkers;** staff make days, routes and areas in the admin web view (2026-10-27).
- **Recorded:** streets walked and No junk mail letterboxes, nothing about residents (2026-10-27).
- **Letterbox drop first;** doorknocking later (2026-10-27).

## Open questions

1. **Areas:** how the church's historical areas are defined today, and whether to import them or redraw
   them in admin.
2. **Licences:** settled for Queensland (table above). G-NAF isn't needed while both councils are in
   Queensland; if it's ever used, its licence restricts compiling addresses "for the sending of mail",
   which needs a look first.
3. **No junk mail flags:** retention (two years?) and whether walkers can see a flag before they reach
   the letterbox.
4. **Rest period:** 7 days between walks of the same street, or longer?
5. **Doorknocking:** if it comes, it needs its own pace, safety guidance and possibly a record of
   "not home" that must not become a record about people.
6. **Area beyond Albany Creek:** the street graph's boundary (suburbs around the church), and its size
   in the app.
