# Outreach

Status: interviewed 2026-11-14 (*The interview*, below: it supersedes the draft where they differ); being built from
2026-11-14 in stages. Draft 2026-10-27. Extends [SPEC.md](../../SPEC.md). A POC is in the PWA spike
(`spikes/app-poc`, More → Outreach and the Hub tile): its engine and planner tools are the starting point, its rough
edges are not.

## The interview (owner, 2026-11-14)

The owner: "The POC was decent, but it definitely had some rough edges and felt very POC." Two parts:

**A) Casual outreach.** "An app user has X amount of time, or wants to go for a walk for X amount of km. Opens the app,
goes to outreach, and wants to walk from where they are (most likely), maybe a select from location they're driving to
... The app gives them a route, and they start walking while the app has a continuous interaction throughout the walk.
needs to be able to handle the user not constantly interacting (shouldn't have to tick off every house, etc), but should
allow marking houses / etc as makes sense." The heart of it is **a shared record with the church**: what has been
outreached, how recently and how much, so casual walks quietly collaborate by avoiding recently done streets. "Sometimes
(especially if a user is frequently walking around their house area), the area around is already outreached, in which
case, the app will need to advise them about that and where to go to start."

**B) Planned outreach.** A monthly outreach Saturday with pre-planned routes, in pairs. Today a ministry member
screenshots Google Maps into Illustrator and draws each route by hand ("That's a lot of work"). People arrive, get a
paper map with a QR code to a PDF copy; **the QR code now opens the route in the app**, and walkers are asked to use it
so the church can see which parts of each route did or didn't get done.

Decided:

- **Tracking: GPS marks streets.** While a walk is on, the phone's position marks each street side as covered as you
  pass it, including with the phone locked in a pocket (iOS background location for the length of a walk only, started
  and stopped by the walker). Nobody ticks houses; you can correct the record at the end.
- **Marks along the way:** No junk mail; Spoke to someone (a count, nothing about the people); Not a home / wrong on
  the map (a report for staff review); Skipped this bit (stays open for someone else even though you walked past it).
- **Start:** from here by default, or anywhere (search an address or drop a pin, e.g. where you'll park). When the
  streets around you are already done, the app says so and suggests the nearest good start and how far it is.
- **Rest period: configurable, default 3 months.** An admin **settings screen** holds this and the other tunables
  (pace, seconds per letterbox, bundle size, connector limits …), and an admin **coverage map** shows
  everything recently done and lets staff mark coverage expired by hand.
- **Outreach days:** printable route maps (one A4 PDF per route: the route drawn, start and meeting point, letterboxes
  and bundles, a street list and the route's QR code), replacing the Illustrator work; the **QR opens the route** in
  the app ready to start (a web page with the map without the app); a **live day board** for organisers (claimed or
  not, progress, what's done, so late arrivals go to gaps). People pick routes themselves; no pre-assigned pairs.
- **Planners:** Outreach is a **ministry**; its leaders (a role) plan days and routes in the admin panel; on the phone
  they see the day and the live board.
- **Areas: both** a coverage map (what's open and what's done, which casual routes aim at) and saved, named areas
  planners can reuse.
- **Sign-in optional.** Anyone with the app can walk, visitors on a day included. Signed-in walkers' totals are
  **counted by name, with an opt-out** (one switch; then counted anonymously), so the church can recognise people who
  do a lot (prizes, competitions); leaderboards are a per-campaign switch for staff. Which streets someone walked is
  never shown to others, only totals; each walker sees their own history.
- **Campaigns.** A campaign is what's handed out and where: *General outreach* (always on), *Carols 2026*, a mission
  like Gympie or the south side. Each has its own flyer, dates, area and **its own coverage**, so a carols drop ignores
  general coverage and the other way round. Outreach days and casual walks belong to a campaign; the app picks the
  active one near you and you can switch. **A walker may carry more than one campaign at once** (carols flyers and
  general tracts): the walk then records coverage for each. No junk mail and the doorknock "hostile" mark are shared
  across campaigns.
- **Data area:** about **20 km around church** by default, cut into **regions**; a campaign far away (Gympie, the south
  side) adds its own region. A phone downloads the region it's walking in automatically and the active campaign's
  region before an outreach day; *Offline maps* lets anyone keep or remove regions, with sizes.
- **Map tiles: self-hosted.** One static PMTiles file (vector map) per region, stored in R2 and read by HTTP range
  requests from the media host; the owner approved this path (AGENTS.md). Styled to the church brand; works offline.
- **Doorknocking: planned in now.** A doorknock walk has its own pace per door, pairs required, and a
  check-in at start and finish. At a door: *Knocked, no answer* (so a later doorknock can try again, never shown as
  anything about the household); *Spoke, follow-up wanted* (a first name and contact details only with their consent,
  sent to the outreach leaders' inbox); *Hostile*: "wants absolutely nothing to do with it", not merely dismissive;
  that address is then skipped for doorknocking and letterboxing.
- **Reporting:** the coverage map with expiry; totals over time (letterboxes, km, walks, conversations, per week, month,
  outreach day and campaign); each walker's own history.

## What it is

A church member asked for a way to make outreach seamless: put in an address and how far you want to
walk, get a route that covers both sides of every street, see how many tracts to take, and keep it
central so the same houses aren't done twice in a week.

Outreach here means **letterbox drops** of tracts, in bundles of 50. Doorknocking may come later; the
design leaves room for it (a slower pace, fewer letterboxes per hour) but doesn't build it.

## What people can do

Three ways in, each a different thing (decided 2026-10-27):

1. **Walk now (casual, the default).** "How long have you got?" 30 min, 1 hr, 2 hr or a distance. The
   app makes a loop there and then, from where you are (or a point you tap if location is off). It
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
  Creek Rd; in OpenStreetMap terms trunk, primary and secondary roads and their slip lanes) are never
  delivery streets: they can be walked along or crossed to get somewhere, but nobody is sent to
  letterbox a main road. They must still be in the street network: until 2026-10-28 the POC left out
  trunk roads (South Pine Rd, Old Northern Rd, Gympie Rd), which split the network into 30 pieces and
  made the streets across them unreachable; with them in, one piece holds 99% of letterboxes.
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

### When Walk now can't be as long as asked (2026-10-28)

Because of the no-empty-walking limit, homes fall into pockets: streets you can reach from each other
without more than 150 m of empty walking. Main roads, shops, parks, creeks and streets done this week
bound them. Around Albany Creek about a quarter of homes are in pockets of under 500 letterboxes, so a
1 to 2 hour walk often fills a whole pocket. Raising the length then changes nothing, and people
can't see why. So the plan reports its **reach**:

- If the walk falls more than 10% short of what was asked, or comes out more than 25% longer, the
  screen says so right under the figures, in plain words: "Streets near you run out at 4.8 km. You
  asked for 10 km." It isn't an error and doesn't pop up. Changing the length, when the walk doesn't
  change, briefly highlights the note and updates "You asked for", so a tap never looks ignored. The
  distance control shows "Up to 4.8 km nearby" once the limit is known, and a Walk now tile whose
  walk is capped says "about 290 letterboxes, the most nearby".
- **Why**, from the data, at most two lines: streets done this week nearby (named and drawn faded),
  the main roads that bound the pocket and the non-homes skipped around it, or the edge of the area
  the app covers. When a longer walk *would* reach further, it says the next streets don't fit, not
  that streets run out. When the walk is longer than asked, it says why: the nearest streets not done
  this week are some way off, or one street both ways already makes it that long.
- **Two ways to get more**, worked out only when the walk is capped:
  - **Walk further between streets:** the shortest stretch over 150 m, and up to 600 m, that reaches
    another pocket ("Add a 290 m walk to reach Flamingo Dr: about 150 more letterboxes, 10.0 km in
    all"). It's opt-in for that one walk and never automatic, because long empty walks are what the
    limit is there to stop. A walk that uses one draws it dotted on the map, labelled "290 m, no
    letterboxes", and can be undone ("Leave it out").
  - **Start where there's more:** the nearest start whose pocket can fill the request ("Start from the
    Bevlin Ct area, 310 m away: room for the full 10 km"). Choosing it switches to Pick on map at that
    point.
- **The real app** covers the whole church area, not the POC's Albany Creek box (so "edge of the
  area" rarely shows), and its "done this week" comes from everyone's walks on the server, not this
  phone. It should also add footpaths and cut-throughs to the street network (OpenStreetMap footway,
  path and cycleway, without the footpaths that run beside a road): in the POC area 68 of them would
  each save more than 150 m of walking, 17 within 1.5 km of church, and many would join pockets.

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
| Map tiles | Protomaps basemap daily planet build (OSM data; Protomaps keeps a week of builds) | An ODbL Produced Work: OpenStreetMap attribution required. Protomaps asks for a copy in your own storage rather than linking to its builds, which is what each region's map file is (checked 2026-11-15) |
| Map lettering | Montserrat (the brand face), turned into map glyphs by `tools/outreach/map/glyphs.mjs` | SIL Open Font License 1.1 |

Each region's record carries one attribution line for the map: "Map © OpenStreetMap contributors (ODbL) · Protomaps.
Addresses © State of Queensland (CC BY 4.0). Land use © City of Moreton Bay (CC BY 4.0). Zoning © Brisbane City Council
(CC BY 4.0)." The app shows it under every map (MapLibre's attribution control, compact) and in More → About; printed
outreach-day maps carry it in their foot. (Updated 2026-11-15: the map tiles are now self-hosted, *Regions and maps*.)

## The route engine (built 2026-11-14)

`GSBC.ChurchApp.Features.Outreach.Routing`: one AOT-safe C# library with no feature or host dependencies, used by the
phone (from the synced street graph), the admin panel (the splitter and its tools) and the API. Its tests run over the
POC's real Albany Creek data, converted by `tools/outreach/poc-fixture.cs`. This section records what the engine does,
and where it differs from the draft above; the draft's rules still hold unless a line here says otherwise.

### The street graph and its file

- **A region's streets** are nodes (with their OpenStreetMap ids), streets between them (name, road kind, shape,
  length) and letterboxes along each side. Each letterbox has its own kind (house, unit, check, skip), its position
  along the street (a fraction, stored to 1/10,000), a label for skips and checks ("shop", "check: mixed use") and an
  optional address key. Sides are left and right of the street's drawing direction, which is the OpenStreetMap way's.
- **Stable keys** (agreed with the data and foundations stations): a street is `way/from node/to node` in OpenStreetMap
  ids, so a rebuild with unchanged streets keeps every coverage record; a stretch is that key, a side, and from and to
  in thousandths; a letterbox's address key is a 32-bit hash of the Queensland address's own id, which No junk mail,
  hostile and excluded addresses key on (with street, side and position as the fallback). In-memory indices are never
  stored.
- **The file** (`.streets`, deflated, delta-coded): Albany Creek's 4,338 streets and 17,839 letterboxes are 152 KB,
  against the draft's 450 KB estimate. **Regions** are cut by the data job: a 10 km core and 2 km of overlap on each
  side. The phone uses the region whose core holds the start; it doesn't stitch regions together, and the overlap is
  enough for a two-hour walk. Shared OpenStreetMap keys make a street in two regions one coverage record.

### Walk now

- **Unchanged:** both sides of every street, the sharpest left at each corner, main roads walked but never delivered
  (and kept in the network: with trunk roads 99% of homes are in one piece, without them under 90%), no street added
  that needs more than 150 m of empty walking, 4 km/h, 8 s a house and 3 s a unit, bundles of 50 with a spare.
  Measured on the fixture: an hour from church is 3.0 km and 109 letterboxes.
- **Changed:** the loop grows street by street from a frontier ordered by distance from its first street, instead of
  the POC's repeated sweeps; same rules, a single pass. The walk to the first open street is limited to **500 m**
  (`MaxApproachMetres`, a setting). Past that, nearby is done: the walk is empty, the reach note says so ("all done
  nearby", naming the streets done recently) and offers the nearest start with room for the whole walk and how far
  away it is. The POC would walk any distance to the first street.
- **Coverage is per letterbox, per campaign.** Each carried campaign's coverage within the rest period (from
  settings, default 91 days) marks the letterboxes it covers; a letterbox is open while any carried campaign hasn't
  covered it, so carrying carols flyers and general tracts walks a street either still needs. A street half covered
  is still walked for its open half. A doorknock's "no answer" reopens that door for the campaign.
- **Marks:** No junk mail is skipped when letterboxing but still knocked on; hostile is skipped for both; excluded
  addresses are skipped, and shown as "Don't letterbox" only when staff chose to; a street whose middle is inside an
  exclusion zone is walked through, never delivered.
- **Doorknocking** uses the same planner with its own pace: 90 s a house and 60 s a unit (to tune in settings).
- **Reach** works as the draft says (short by 10%, long by 25%, the two ways to get more, at most two reasons), with
  the pockets worked out around the walk only, not over the whole region. "The edge of the area" is the region's edge.

### Outreach days: the splitter and its tools

The splitter is the POC's, with its rules unchanged (even sections grown from seeds, joined only through connectors up
to 125 m, scraps under a third merged into a neighbour, then balanced). A cut-off group stays its own route. **The
draft is a value:** every tool (merge, scissors, brush with ends snapping to a corner within 15 m, lasso, eraser,
lock, rename, start pin, rebalance, regenerate) returns a new draft and leaves the old one as it was, and undo and redo
step through the last 50. Sections are saved as stretches, so a draft survives a rebuild of the streets.

**Route summaries** give the walking screen and the printed map the same things: each step (one side of one street),
its letterboxes, time and line, its markers (Skip: shop, Check, No junk mail, Don't letterbox; never why), the street
list with sides, distance, bundles and time.

### Following a walk

The follower turns GPS fixes into the street sides actually walked, whatever the plan was. Fixes worse than 40 m are
ignored; fixes up to 50 m apart are joined straight, and gaps up to 600 m (a phone in a pocket) are bridged along the
streets when the detour and the speed are plausible. Each point is matched to a street by distance, heading and
staying on the street it was on. A side is decided by the fixes' average offset when it's clearly one kerb (4 m or
more), otherwise by the walking direction (the routes keep the houses on your left). Crossing a street doesn't mark it
(less than 14 m along it), turning back starts a new side, a stretch walked to within 15 m of a corner counts to the
corner, and "Skipped this bit" stays open. Measured on synthetic tracks over the fixture, an hour's route walked on
the kerb: 100% of its letterboxes marked with fixes every second or every five seconds, 97% with a fix every 45
seconds (10 m noise and drift), and no letterboxes marked that weren't walked. Someone on the other kerb is marked on
the side they were on in at least 8 of 10 test streets. **Open:** these are synthetic tracks; a few real recorded
walks (in hand and in a pocket) should become test fixtures once stage 2's walking screen can record them.

### Speed (measured 2026-11-15)

On an Apple M3 Pro, with the region-sized graph made by `tools/outreach/benchmark.cs` (Albany Creek six times over:
26,028 streets, 107,034 letterboxes, 789 KB), under WebAssembly AOT (the app's runtime, run in Node): loading the
region takes 16 ms (30 ms with its street index), working out what's open 3 ms, an hour's walk 0.5 ms, and an hour's
walk with its reach note from 50 places 0.6 ms on average and 4.7 ms at worst. On the JIT the numbers are about the
same. Even an older phone tens of times slower plans a walk well under a second. The whole 40 km church area (48 copies,
856,272 letterboxes, 6 MB) loads in 66 ms on the JIT, so a phone could hold it whole if regions ever needed to go.

## Regions and maps (built 2026-11-15)

`GSBC.ChurchApp.Features.Outreach.Maps.Api` and `.Maps.Contracts` (schema `outreach_maps`). The streets and the map
outreach works on come in **regions**, which the API builds itself, and which phones download as two files.

- **Areas and regions.** Staff add an **area** (a name, a centre and how far it reaches, 1 to 30 km); the first one is
  seeded: *Around the church*, 20 km each way from the church (Moreton Bay and Brisbane's northside). An area far away
  (Gympie, the south side) is just another area. Each area is cut on a grid into **regions**: cores of at most 10 km a side, an
  odd number of them each way so the area's centre (the church) sits in the middle of one, and 2 km of overlap on
  every side (*Around the church* is 5 × 5 regions with 8 km cores), so a two-hour walk started in a region's core stays inside it and the
  phone never stitches regions (agreed with the engine station). A region whose core holds no homes (bushland, the
  bay) isn't made. Region ids are stable slugs (`church-b2`); names come from the commonest suburbs among the homes in
  the core ("Albany Creek and Bridgeman Downs"), numbered when two would share one.
- **Why that size.** Measured 2026-11-15 by the job itself over *Around the church*: the whole 40 km square is 89 MB
  of maps and 11.4 MB of streets files (25 regions); a typical region downloads 2 to 5 MB in all (Albany Creek and
  Bridgeman Downs, the church's own: 4.5 MB of map and 0.65 MB of streets), the busiest, the inner city, 11.5 MB, and
  bush regions about 1.2 MB. One file for the whole area would be about 100 MB. A phone downloads the region it walks in automatically and the outreach day's region before the day,
  so a region is kept to what is reasonable on mobile data; *Offline maps* lists them with sizes.
- **The build is a job in the API** (`AreaBuildRunner`, one area at a time under a Postgres advisory lock), queued by
  adding an area or by *Rebuild* (the admin `Maps` service: list, add, rename, rebuild, delete;
  `outreach.maps.manage`, global: setting up maps is whole-church work). It resumes an interrupted build. Steps:
  1. **Streets** from OpenStreetMap: Geofabrik's Queensland extract (`queensland-latest.osm.pbf`, about 200 MB,
     refreshed daily), downloaded to a temporary file and read in one pass in .NET (`OsmPbf`): the coordinates of
     nodes inside the area, then the walkable ways, each cut to its longest run inside the area. Kept: roads (residential, living street, unclassified,
     tertiary, the main roads and their links) and footways, paths, cycleways, steps and pedestrian streets. Left out:
     private or no-foot ways, areas, and a footway tagged as a road's sidewalk or crossing. A path whose length runs
     mostly (60%) within 20 m of a road and alongside it (within 30°) is beside the road and left out; paths through
     parks and cut-throughs stay. Streets split where they meet and keep the OSM way's direction and node ids (the
     engine's stable keys). The first version asked a public Overpass server instead: around the POC box it took 20
     minutes on a busy night against 4 seconds from the extract, with the same streets (2026-11-15).
  2. **Addresses**: the Queensland address layer's primary addresses (`address_status = 'P'`; the POC also counted
     aliases, double-counting some homes). Each is placed on the nearest segment **of its own street** within 200 m,
     on the side it's on.
  3. **Homes only**: Moreton Bay lots by lot and plan, then by the lot it sits in; Brisbane City Plan zones by the
     zone it sits in, with the rules above (mixed use and unknowns are delivered as "check"). Any other council is
     delivered as "check: no land use data for this council" until its import is written.
  4. **Each region's `.streets` file**, written through the route engine: every segment that touches the region's
     bounds and every letterbox placed on them (skips with their label, units as units, the address id as its key).
  5. **Each region's map**: a vector map file (PMTiles, zoom 0 to 15) cut from the latest Protomaps planet build by
     range requests, in .NET (`PmtilesExtract`): only the region's tiles are read, about 10 to 20 s a region.
  6. **Stored and published**: both files go into the **public media bucket** under content-addressed keys
     (`<sha256>.streets`, `<sha256>.pmtiles`) with `Cache-Control: public, max-age=31536000, immutable`, and the region's
     record in the `outreach.regions` feed (public) carries both URLs and sizes, its core and bounds, the map's
     zoom range and the attribution. A rebuild whose streets and map are unchanged keeps its version, so phones don't
     download again; a changed one gets the next version. The previous version's files are kept (a phone may still be
     reading the old map), older ones are deleted. Deleting an area deletes its regions, their records and files.
- **The map file is a static file read by HTTP range requests** from the media host, the path the owner approved
  (AGENTS.md, 2026-11-14): no host gets an endpoint, nothing varies per person. Checked through Cloudflare on
  `dev-media.baptist.com.au` 2026-11-15: a range request answers 206 with the `immutable` header, the second is a
  Cloudflare `HIT`, and CORS allows the app (`capacitor://localhost`) and the admin panel with a `Range` header.
- **What UAT needs** (for the orchestrator to put to the owner; nothing here touches the cluster): the API pod reaches
  download.geofabrik.de, spatial-gis.information.qld.gov.au, services-ap1.arcgis.com,
  services2.arcgis.com, build-metadata.protomaps.dev and build.protomaps.com over HTTPS; a build of the church area
  peaks at about 1.7 GB of memory for a few minutes (measured in dev; most of it is the 640,000 addresses and
  the land-use polygons, freed once letterboxes are placed) and writes temporary files (the 200 MB Queensland extract and a
  region's map) to the pod's temp folder; the R2
  bucket `gsbc-uat-media`'s CORS keeps allowing `GET` with a `Range` header from the app and admin origins and exposes
  `ETag`; Cloudflare caches files of this size (well under the 512 MB limit). The first UAT start builds *Around the
  church* by itself (14 minutes in dev: addresses 5½, land use 4, streets from the extract under a minute, the 25 regions' files 3). Settings live under `Outreach:Maps` (sources, `MaxZoom`, the seeded centre and
  reach, `SeedChurchArea`).

- **Measured 2026-11-15 over *Around the church*:** 639,807 addresses, 96.6% placed on their own street (the POC's
  Albany Creek box: 94.0%; the POC counted alias addresses too and reported 95%); 533,227 homes, 29,405 delivered as
  "check", 85,051 skipped as not homes; 127,676 street segments; 10,486 footpaths beside roads left out.
- **The 20 km area, answered (owner, 2026-11-15):** asked whether *Around the church* should also take in the inner city
  and the south side, the owner answered "is there way(s) to quickly and easily add additional areas and regions via
  the admin panel? if not, build that" and "let admins add or remove it". So the seeded area stays 20 km each way, and
  staff switch parts of it off themselves (*Map areas you can shape*).

### Map areas you can shape (built 2026-11-15)

- **An area is any set of squares on one grid.** Adding an area still cuts a square round its centre into regions as
  above, but the area then keeps its grid (centre, a square's size, which square is the middle) and a row per square
  (`outreach_maps.cells`): on or off, waiting to build, or empty (no homes). Staff switch squares off (the inner city out
  of *Around the church*) and on again, and add squares that touch one that stays on, up to 64 an area. An area keeps at
  least one region (delete the area instead). Existing areas were given their grid from their bounds, keeping every
  region id.
- **A region's id is its square, and never names another place.** Ids stay `church-c3` inside the first grid and become
  `church-rm1c3` (a row above it) outside it; an area's id is never reused (a deleted area stays as a tombstone), so a
  new "Gympie" after a deleted one is `gympie-2`. Switching a square back on brings the same place back under the same
  id, and its version carries on from the last one published (phones holding the old files download again).
- **Switching a region off** deletes its region record (the `outreach.regions` feed marks it deleted, so phones drop it
  and *Offline maps* stops listing it) and its files from the media bucket, at once. Coverage, marks, follow-ups and
  history are keyed by streets and stay; switch it on and they show again. Refused while the area is building.
- **Adding builds only what was added.** The job builds the squares waiting to build, a touching group at a time (so
  squares added on opposite edges don't read the whole area between them), reading the sources for the box round each
  group only (the addresses and land use, the slow part, are queried for those squares; the Queensland extract is
  still downloaded whole and read for that box). *Rebuild* marks every square of the area to build again; *Try again*
  after a failure builds what was waiting. A square added while the area builds waits for the next run, which starts
  straight after. New regions' names avoid the area's existing names. The build reports its steps (four fetches, then a
  region at a time) so the admin panel shows a progress bar.
- **A slow source no longer fails the build (2026-11-15).** Dev's first build of *Around the church* stopped at region
  22 of 25: a map read of up to 16 MB from the Protomaps build stalled past the HTTP client's 5-minute limit, which .NET
  reports as a cancellation, so it was never retried. Now no request has a blanket limit: each range read gets 30 s
  plus a second per 100 KB and is retried five times (timeouts and dropped connections included), an address or
  land-use page gets 2 minutes and four tries, and the Queensland extract 30 minutes and three tries. A build that
  still fails keeps every region it finished; *Try again* builds only the squares still waiting.
- **What uses a region** (*Claude's default*): before a region goes, the admin panel lists the outreach days not yet
  over that plan routes there (from the day's draft or its routes) and the campaigns not yet ended that name it, as the
  "what goes" previews elsewhere do, and asks once. Nothing is changed or refused: the day keeps its routes and printed
  maps, but its walkers would have no map or streets there until the region is switched on again. Outreach answers
  through `IRegionUses` (Maps.Contracts), so Maps never reads Outreach's tables.
- **Finding a place by name** (`Maps.FindPlaces`, gRPC, `outreach.maps.manage`). *Claude's default, and why*: the names
  come from OpenStreetMap's place nodes in the Geofabrik Queensland extract the build already downloads (city, town,
  suburb, village, locality, hamlet, neighbourhood: about 4,400 in Queensland; Gympie, Carindale, Coorparoo and West End
  are all there), kept in `outreach_maps.places` and refreshed every 30 days by the build job (a failed refresh tries
  again in 6 hours). The address layer's localities were the other choice: they name every suburb but give no point to
  centre on without reading every address, and nothing for towns as a whole. A search matches names starting with the
  text first, then towns before suburbs before localities, and says how far and which way each is from the church.
- **Seeing an area before it builds** (`Maps.Preview`): the squares for a centre and reach, with each square's primary
  addresses counted by the address layer (counts only, kept 6 hours) and an estimated download (1.73 MB plus 44.5
  bytes for each address in the region with its overlap, fitted to *Around the church*'s 25 regions: half within 15%),
  and up to 12 places inside it, towns before suburbs before localities. A square with no
  addresses isn't made. `Maps.CheckChange` gives the same for squares being added, plus what uses the regions going and
  how much they free; `Maps.ChangeRegions` applies it.
- **No overview map file** (*Claude's default*): the admin panel draws the squares over the nearest built region's map
  (a region's map file carries its low zooms, which reach well past it near the church), with each square's count as
  its label; far away (Gympie) the ground is mostly blank until its own regions are built. A Queensland overview map
  would be a new static file on the media host beyond the approved "one map file per region", so it wasn't made.

### The map's look (2026-11-15)

`src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/`: `map-style.js` builds the MapLibre style for a region's map
file, light or dark, and `glyphs/` holds Montserrat 400 and 600 as map glyphs (made by `tools/outreach/map/glyphs.mjs`;
Latin and punctuation only, as the brand fonts are subset). It ships with the app and the admin panel, so it works
offline. There are no icons or sprites: the map names streets and suburbs and draws nothing that competes with a
walk. The look was put on the decision page (`.impeccable/mocks/outreach-map/`) while the owner was asleep; the station
built *What bounds a walk* (parks in sage, schools, shops and industry in beige, main roads with a firm edge, so a
short walk's reason shows) and kept *One calm sand page* and *The houses themselves* there to switch to: each is a
`look` in `map-style.js`. What's drawn on top is the screens' job: the walk in deep aqua (bright aqua at night) with a
white edge, streets walked recently in ink-3 at 45%, you in orange.

## Foundations: records, feeds and services (built 2026-11-14)

Built overnight by the foundations station while the owner slept. Everything marked *Claude's default* is the owner's to
change. The module is `Features/Outreach` (Contracts, Api, Client, App, Admin) beside the route engine (`Routing`) and
the maps station's regions (`Maps`); schema `outreach`.

### Outreach is a ministry

- **A ministry runs outreach by turning on the Outreach part** (Settings → *What it uses*; `MinistryParts.Outreach`, off
  for every ministry until a leader turns it on: a migration added it to every existing ministry's parts that are off).
  *Claude's default*: more than one ministry may run it (a missions ministry and an outreach team, say); they share
  the same campaigns and records.
- **Permissions** (Access, at the ministry; `OutreachPermissions`):

  | Node | Allows | Who holds it |
  |---|---|---|
  | (none) | Walk, mark, report, claim a route, doorknock | Anyone with the app, signed in or not (`[AppInstall]`) |
  | `outreach.reports.view` | See campaigns, coverage, totals and the day board; opens the admin page | Leader, Admin and Team of the Outreach ministry |
  | `outreach.plan` | Campaigns, outreach days, routes, saved areas | Leader, Admin |
  | `outreach.review` (sensitive) | Walkers' reports, follow-ups with contact details, clearing No junk mail and hostile marks | Leader, Admin |
  | `outreach.settings.change` | Settings, exclusion zones and addresses | Leader, Admin |
  | `outreach.coverage.expire` | Mark streets not done before the rest period ends | Leader, Admin |

  The ministry roles get these through `AddToSystemRole` (as Rosters does); every ministry's leaders hold them at their
  own ministry, but the server only accepts a ministry that has Outreach on. A global **Outreach Manager** role
  (`outreach.*`, *Outreach* in Access) is for staff who run outreach without leading the ministry; it also covers the
  maps station's `outreach.maps.manage`. The Owner has everything.
- Every admin request carries the ministry it acts at (`IScopedRequest`); an empty one is allowed only for global holders.
  The settings feed lists the ministries running outreach (`MinistryIds`), refreshed every 15 minutes and on any save.

### Campaigns

- A campaign: name, what's handed out, optional start and end dates, an optional area (a polygon) or regions, a
  leaderboard switch. **General outreach** is always there (seeded with a fixed id), can be renamed, has no dates, and
  can't be deleted. A walk carries one or more campaigns (none chosen means General) and records coverage for each.
- **Deleting a campaign** (*Claude's default*): refused while it has an outreach day still to come ("Delete or move them
  first"); otherwise its coverage, no-answer doors and past days (with their routes and claims) go with it, and its walks
  stay in the totals without it.

### Settings

One record (`OutreachSettings`, feed `outreach.settings`) with the engine's `RouteSettings` names 1:1 (rest days, pace,
house, unit, door and unit door seconds, bundle size, spare share, connector limits, the walk to the first open street,
reach thresholds) plus the screens' own: doorknocking in pairs, how long No junk mail marks (730 days)
and done follow-ups (90 days) are kept, and what counts as a walk (fastest 9 km/h, longest 30 km). Defaults are in
`OutreachDefaults`; the server refuses values outside sensible ranges (`SettingsRules`). The rest period is days: 91 for
"three months".

### Coverage

- One row per street side (or stretch, in thousandths) per campaign: date, source (casual, day route, area), the walk
  and the day, the stretch's middle point and length, and the region it came from. Streets are the engine's stable
  `StreetKey` ("way/from/to"), so coverage survives a rebuild of the map data.
- **The feed** `outreach.coverage` has one record per campaign and street: the newest walk of each piece of each side
  within the rest period (`CoverageRules.Merge`). A street leaves the feed when its last piece passes the rest period.
- **Expiring by hand**: staff choose a campaign and an area (by the stretch's middle point) or a list of stretches; those
  rows are marked expired and the streets reopen at once in that campaign only.
- Coverage older than a year is deleted; the walks' totals keep it in the reports.

### Walks are recorded as they go

*Claude's decision*: a walk is a phone-made id plus numbered pieces (`RecordWalk(WalkPiece)`), each sent through the
outbox as the walk goes: the stretches covered since the last piece, those skipped or taken back, metres walked, doors
and conversations, and `Ended` on the last one. The first piece creates the walk (with its campaigns, mode, day and
route). **Why:** a walk cut short (a flat battery, no signal for an hour) still counts everything up to its last piece,
retried pieces are recognised by (walk, number), and no message grows with the walk.

- **Plausible before it counts** (`WalkRules`): a piece whose times are in the future or more than a week old, that makes
  the walk faster than the fastest setting or longer than the longest, or that marks more street done than was walked,
  is kept but marks nothing done; the walk is marked doubtful with why and leaves the totals. The phone's figures are
  trusted otherwise (the server has no street graph; checking lengths against it is open).
- A casual walk started during an outreach day of one of its campaigns counts toward that day.

### What walkers mark

- **No junk mail** (`NoJunkMail`): the letterbox's address key (and street, side and position) goes into `outreach.skips`
  as "skip this letterbox" in every campaign, kept two years from the last time anyone marked it. The walker who marked it
  can take it back within a day; staff clear it (`outreach.review`). Never shown with who made it.
- **Spoke to someone** is a count on the walk; nothing about the people.
- **Skipped this bit** and corrections take the walk's own coverage back off those stretches.
- **Reports** (`Report`): a reason (not a home, unsafe, a complaint, wrong on the map, other), the place, a **mandatory
  note** and an optional photo (uploaded through the outbox into Media's *Outreach reports* area, readable only by
  `outreach.review`; deleted once the report is dealt with). Staff mark it *never deliver there* (it becomes an excluded
  address), *fixed* or *dismissed*, or delete it. *Not a home* from the walking screen is a report too, so it carries a
  note. Resolved reports are deleted after 90 days.

### Doorknocking

- *Knocked, no answer* keeps only the address and campaign, for the campaign's rest period, so a later doorknock tries
  again (not yet in a feed; the engine's `Reopened` reads it in stage 2).
- *Spoke* is a count. *Spoke, follow-up wanted* keeps a first name, contact and note **only when the walker ticks that
  they agreed** and gives a contact; otherwise nothing is kept (`DoorRules`). A kept follow-up notifies the outreach
  leaders (`outreach.follow-up`, waits until marked followed up) and syncs to their phones (`outreach.follow-ups`,
  open ones only); done ones are deleted after the settings' days, open ones after a year.
- *Hostile* adds the address to `outreach.skips` as "skip the letterbox and the door" in **every** campaign, until staff
  clear it.

### Outreach days, routes, claims and areas

- A day: campaign, start and end (a time window), meeting point and place, note, letterbox or doorknock, published. A
  route: a list of stretches with its start, letterboxes, metres and minutes, a number, and a **six-letter code** for its
  QR (`ABCDEFGHJKMNPQRSTUVWXYZ23456789`, unique). Published days within four weeks, and until a day after they end, are
  on phones (`outreach.days`, `outreach.routes`).
- **Claims**: a first name (required), several per route, the claimer can take theirs back; all deleted when the day
  ends (`outreach.claims`).
- **Saved areas**: a name, a polygon and a note, for planners (admin only).
- The day board (`OutreachAdmin.Board`): each route's claims and how much of it was walked since the day began
  (a stretch counts once 90% of it is covered).

### Walkers and history

- Walks are kept per install, and per person when signed in. **Counted by name** is one switch (`Counting`, default
  on): off, the person's walks count only in the anonymous total, on every leaderboard and report, past walks included.
  The person's own history (`outreach.mine`) shows every walk either way. Signed-out phones keep their own (stage 2).
- **Leaderboards** (`outreach.leaderboards`) for campaigns with the switch on: the top 20 named walkers as "Sam S." and
  everyone's total, refreshed every 15 minutes and published only when changed.
- **Deleting an account** (`OutreachPersonData`): their walks stay, without them; their history, counting choice and
  route claims go; their reports, follow-ups and marks stay without who made them.

### Limits (*Claude's defaults*)

Per phone per day: 20 walks, 150 No junk mail marks, 400 doors, 20 reports, 20 claims; past that the outbox waits until
tomorrow. Each walk: 2,000 pieces, 300 stretches a piece, 500 doors or conversations a piece.

### Feeds

| Feed | Audience | Records |
|---|---|---|
| `outreach.settings` | everyone | the settings and the ministries running outreach |
| `outreach.campaigns` | everyone | every campaign |
| `outreach.coverage` | everyone | per campaign and street, the newest walk of each piece within the rest period |
| `outreach.days`, `outreach.routes` | everyone | published days from four weeks ahead to a day after, their routes |
| `outreach.claims` | everyone | first names on routes until the day ends |
| `outreach.skips` | everyone | No junk mail (letterbox) and hostile (letterbox and door) addresses, never who or why |
| `outreach.exclusions` | everyone | exclusion zones and addresses, with "Don't letterbox" only where staff chose to show it; never the reason or caller |
| `outreach.leaderboards` | everyone | per campaign with a leaderboard |
| `outreach.mine` | own records | your walks and your counting choice |
| `outreach.follow-ups` | `outreach.review` at the ministry | open follow-ups |
| `outreach.desk` | `outreach.reports.view` at the ministry | markers for the admin panel |
| `outreach.regions` | everyone | the maps station's regions (`Maps`) |

### The admin page (2026-11-14)

Outreach in the admin panel (Ministries & groups → Outreach, `/outreach`): **Campaigns** (the workbench: the list with
what's handed out, dates and this month's letterboxes; the chosen campaign with its letterboxes, km and outreach days,
its fields, Cancel and Save, Delete in its ⋯), **Follow-ups**, **Reports** (lists with the chosen one beside them) and
**Settings** (grouped cards, one Save). Decision page: `.impeccable/mocks/outreach-desk/` (three structures; the station
built C, *Campaigns as the workbench*: campaigns are records, settings are settings; A and B stay on the page for the
owner). The planner, coverage map, day board and exclusions screens are stage 2. Notifications open its Follow-ups and
Reports tabs; the phone's follow-up page is stage 2.

### Coverage, numbers and places in the admin panel (built 2026-11-15)

Built overnight by the reports station while the owner slept; *Claude's default* marks what's the owner's to change.
Decision page: `.impeccable/mocks/outreach-coverage/` (A the map is the desk, B numbers lead, C a coverage map with a
working rail). The station built **C** (*Claude's default*): coverage is a job of its own with the map and a rail ruled
off where every workbench keeps its record, numbers get their own tab rather than hiding the map, and the places
outreach skips are a list and record like every other admin list. A and B stay on the page.

The Outreach page's tabs are now Campaigns, Coverage, Numbers, Places, Follow-ups, Reports and Settings.

- **Coverage:** a campaign's streets done within its rest period, on the region's map, each side drawn in one deep-aqua
  ramp that fades to the map's ground as the rest period ends (the legend is the same ramp: this week, half the rest
  period, "open again"). Two figures: streets done of the region's streets with homes, and the share of its
  letterboxes. No junk mail and hostile marks and the places outreach skips can be shown or hidden. **Mark as not
  done**: click a street (both sides) or draw round streets (a lasso, by each stretch's middle, as the server matches);
  the rail says how many sides and letterboxes and when they were done, and marking them applies at once with an
  **Undo** (`UndoExpiryAsync`: the expiry's time is returned and puts back exactly the rows it expired). *Claude's
  default*: undo rather than a confirm, since it is cheap to reverse and a confirm would be clicked through.
- **Numbers:** letterboxes, km, walks and conversations (doors when any) for the last 13 weeks, this year or the last
  12 months, all campaigns or one, each against the period before; letterboxes by week or month as a trend line with
  the period before dashed; a table per outreach day (`TotalsBy.OutreachDay`: walks during the day, the day's date and
  campaign); walkers counted by name ranked by letterboxes, then the walks of people counted without a name in one
  line. The leaderboard switch stays on the campaign; the walkers list says which campaigns show one in the app.
- **Places:** one workbench of areas and addresses outreach skips, No junk mail and hostile marks, filtered in the rail.
  An area is drawn on the map (name, why, note); an address is clicked on the map (the nearest letterbox on that side)
  with why, who asked (admin only) and whether walkers see a Don't letterbox marker. No junk mail and hostile marks are
  cleared from the ⋯. Who marked anything is never shown. Areas need `outreach.settings.change`; marks
  `outreach.review`.
- **Reports:** the place on a map and the walker's photo (opens in the picture viewer); *Never deliver there* skips the
  letterbox, *Skip an area round it* draws an area instead (`ReportResolve.Area`; the zone is named after the place and
  keeps the walker's note), and the reason maps to the exclusion's.
- **Settings:** every field says its usual value; *Spare tracts* (the engine's spare share, as a per cent) joins How
  long a walk takes. **Map areas** (for `outreach.maps.manage`): each area's state (with a progress bar while it
  builds), regions, letterboxes and download size, with Rebuild, Rename and Delete in its ⋯. An area opens on its own
  map page (`/outreach/map-areas/{id}`, built 2026-11-15, replacing the typed latitude and longitude): its squares on
  the map, clicked to switch off or on or (a + on the edge) to add, the regions listed on, off and changing with
  switches, and the change saved from the pane's foot, asking once before anything goes. *Add a map area*
  (`/outreach/map-areas/new`) finds a town or suburb by name or takes a click on the map, with the reach (1 to 30 km,
  10 to start) and a name, and shows the squares and the estimate before *Add and build it* (*Map areas you can shape*).
- **Seams:** `IOutreachMapsService.RegionsAsync` (signed in; the same as the public regions feed) gives staff without
  `outreach.maps.manage` the regions; `StreetGraphLoader` (Outreach.Client) fetches and caches a region's streets for
  the admin panel, shared with the planner. The shared map gained a `Covered` line style with an age, and colours given
  as theme tokens.

### Open (for stage 2 and the owner)

- Checking walked lengths against the street graph on the server (today the phone's figures are trusted within the
  speed and length limits).
- A new exclusion zone doesn't yet flag the day routes it touches.
- The coverage map shows one region at a time (a chooser when there are several); streets in two regions' overlap show
  in both.
- Taking an area as a claim (the 2026-10-27 draft's *Areas*) isn't built: areas are the planners' for now.

## Planning outreach days (built 2026-11-15)

Built overnight by the planner station while the owner slept. *Claude's default* marks a choice the owner hasn't made.

### Where it is

- **Admin panel → Outreach → Days:** the `Workbench` list of days (when, routes, a state: To plan, Ready, On now, Done)
  and the chosen day's details: campaign, date, start and end, where people meet, letterboxing or doorknocking, a note
  for walkers; its routes, letterboxes and claims; and the way into its routes. New outreach day suggests the next
  Saturday at the last day's times and meeting place.
- **The day's routes** (`/outreach/days/{id}/plan`): the map and the routes table side by side (`MapDesk`), the same page
  before and on the day (*Plan* and *On the day*). Decision page `.impeccable/mocks/outreach-planner/`: three
  structures; the station built C, *the map and the routes table*, because planning and the day itself both happen on
  the map. A (the day as a board of routes) and B (the day as its printed maps) stay on the page for the owner.
- **Outreach → Areas:** saved, named areas drawn on the map, with a note; renamed and deleted there, also saved from the
  planner ("Save this area for next time").
- **The phone:** *How the day is going* (`outreach/days/{id}/board`), linked from the day's page for anyone with
  `outreach.reports.view`. Read from the store (Sync), so it works at the meeting point with poor signal.

### Planning

- **Draw the area** (click the corners) or **start from a saved area**; it's split by the engine's splitter into routes of
  about a target size, in letterboxes (default 100, about an hour), minutes or bundles. The campaign's coverage within
  the rest period is left out (grey on the map) unless *Include streets done recently* is on. Coverage is judged on the
  day's own date, so streets done long enough before the day are open again.
- **Tools** (the rail on the map, as the POC had them): select, draw the area, scissors, brush (drag; a click takes the
  whole street), lasso (into the chosen route, or a new one), eraser (leave streets out of the day), start pin, meeting
  point, undo and redo. The chosen route floats a bar: lock or unlock, split in two, rename, merge with another route,
  leave its streets out. Rebalance and Regenerate leave locked routes alone. Hovering a route shows its letterboxes, km,
  time, bundles and why it needs review.
- **Needs review** (from the engine): in pieces; more than 150 m between streets in one go; more than 250 m in all.
- **The draft saves as you go** (to the day, `SaveDraft`), so a day can be left half planned. Sections are kept as
  stretches by the streets' stable keys, so a rebuild of the region doesn't lose it (`RouteDraft.Restore`).
- **Publish** sends the whole set of routes (`PublishRoutes`). Nothing reaches walkers before that, and publishing with
  routes still flagged asks first, listing them. A route kept from before keeps its six-letter code, so maps already
  printed still open it; routes taken away lose their claims. A published day with no draft opens its routes for
  editing as they are.
- **The meeting point** is set with its pin on the map (*Claude's default*: the day's form only names the place; the
  centre of the region until a pin is set).

### Printed route maps

*Claude's decision*: made in the admin panel as a real PDF (`PdfDocument` in Platform.Common, `RouteSheets` in
Outreach.Client), downloaded as a file; no new server path. One A4 page a route, in black for any printer:

- Route number and name, the day, its times and where to meet, letterboxes, bundles to take of what's handed out, time
  and km; the route's **QR code** (`OutreachLinks.RouteLink`, the app's shared link, `/open/outreach/r/<code>`) with the
  code printed under it for typing in.
- **The map is drawn from the street graph** in vector, not from the tiles: every street in view in grey (main roads
  heavier), the route's streets thick black, walk-through stretches dashed, arrows along the way, the start, the meeting
  point when it's in view, street names (the route's in bold, others for finding your way, never overlapping or cut by
  the frame), a scale and north. *Why:* tiles printed in grey wash out on black-and-white printers, and vector stays
  sharp at any size. The brand base map stays on screen.
- **The streets in order**, with "both sides" or "one side", "(part)" streets, and letterboxes each; skips and
  "Don't letterbox" counts; "Keep the houses on your left and you'll cover both sides. Please walk in pairs."; the day's
  note; the map data attribution.
- Printing waits for the routes to be published, and for changes to be published, so a map never shows a route walkers
  can't open. Fonts are the PDF's own Helvetica (*Claude's default*; embedding Montserrat is open).
- **Open:** a web page with the route's map for someone without the app is a new non-gRPC path and needs the owner's
  yes (the walking station asked); today the QR without the app lands on the /open page that says how to get it.

### On the day

- The board shows each route's claims (first names), how much is walked (a stretch counts once 90% of it is covered
  since the day began; `DayProgress`, one rule for the server and the phone), and a state: Open, Claimed (not started),
  Walking, Done (95%). **Open routes come first**, with "Someone just arrived? Routes 3, 6 and 8 have no one yet."
- Admin: the same map, with what's walked drawn over the routes, and how many people are walking now. Phone: the list,
  with no map (*Claude's default*: a leader at the meeting point needs the gaps, one-handed).

### Deleting

- **A day** (*Claude's default*): its routes and claims go, and printed QR codes stop opening them; its walks and the
  streets they covered stay, as ordinary walks (the streets were walked). The Sheet says so, and warns when it's on now.
- **A saved area:** days planned from it keep their routes.
- **A route:** by leaving its streets out, or merging it, then publishing.

### Colours

Nine route colours (`RouteColours`), each 4.5:1 with white label text, on the map and in the tables; the chosen route
stays full colour and the rest fade.
## The phone (built 2026-11-15)

Built overnight by the walking station while the owner slept; *Claude's default* marks what is the owner's to change.
Design: `.impeccable/mocks/outreach-walking/` (three structures; the station built A, *Words first*, borrowing B's line of
stops for the end of a walk; B and C stay on the page for the owner). Detail: [design/outreach.md](../design/outreach.md).

- **Where it lives.** Outreach (`/outreach`) is a page any tab or Home tile can open: the Home tile `hub.outreach` (it
  replaces the coming-soon tile with the same key, so placed tiles keep working, and goes live while an outreach day is on)
  and a nav entry staff can put in the tab bar. It leads with your walk if one is on, then the outreach day if one is on,
  then *Got an hour? Walk from your door* (30 min, 1 hr, 2 hr, or a distance), what you're carrying, a map of the streets
  around you with those walked lately in grey, the next outreach days, My outreach and Offline maps.
- **Carrying campaigns:** the active campaigns you choose are remembered on the phone; none chosen means General outreach.
  A doorknock counts for one campaign (*Claude's default*).
- **Planning** runs the engine on the phone from the region's street file kept on the phone: from here (location asked
  only when you plan), a street you find by name (searched in the region's own street names, offline, no geocoder), or a
  pin dropped on the map. The plan shows distance and time, letterboxes and bundles, when you'll be back, the reach note
  with its reasons and the two ways to get more, and the streets. Doorknocking can't
  start without *I'm walking with someone* when settings ask for pairs. A walk may start at any time of day (owner,
  2026-11-15, #103: "there's no requirement to only walk during the day").
- **The walk.** Location runs in the background for the walk's length only (When In Use permission and iOS's blue
  indicator; started and stopped by the walk). Every fix goes into a journal on the phone, so a locked phone, a suspended
  page or the app being closed loses nothing; the follower reads it whenever the app runs. The screen leads with the
  street and side at display size, what's left on it and the next turn; the map follows you (drag to look around,
  *Follow me* to come back); progress, then two big marks (No junk mail and Spoke to someone; At a door when
  doorknocking), then Pause, ⋯ (Not a home or wrong on the map, Skipped this bit) and Finish. A light haptic when you
  start a new street, while the app is open. No junk mail marks the nearest home on the side you're on, with Undo.
- **Recorded as it goes:** a piece goes through the outbox every 90 seconds or when you reach a new street, with the
  street sides newly covered (never one already sent), skips, conversations and doors. The walk's state is kept on the
  phone, so it carries on after the app is closed, numbering included.
- **The end** shows what you walked and the planned streets as a line of stops, each ticked where the app saw you walk;
  untick what it got wrong (taken back), tick what it missed (added), save. *Keep walking* goes back to the walk.
- **Doorknocking:** at a door, *Knocked, no answer*, *Spoke with them*, *Spoke, and they'd like a follow-up* (first name,
  phone or email and a note kept only when *They agreed to be contacted* is on; otherwise just a conversation), or *They
  want nothing to do with it* (explained, then confirmed). Check-in at start and finish is the walk's first and last piece
  (*Claude's default*: no separate check-in step).
- **Reports** (Not a home, Wrong on the map, Unsafe, A complaint, Other) need a note, can carry a photo of the place
  (queued with the report through the outbox into *Outreach reports*), and are for the nearest home to you.
- **Outreach days** list their routes with who's taking them (first names, "Sam, Ruth + 1"), Open and Yours. A route's page
  shows it on the map with the meeting point, its figures and streets, how much is done today, *I'll take this route*
  (a first name, asked once and remembered on the phone; taken back with *I'm not taking it after all*), and *Start this
  route*. Leaders who can see reports get *How the day is going* (the planner station's live board).
- **The QR on a printed map** is `https://<app host>/open/outreach/r/<code>` (`OutreachLinks.RouteLink`), the app's
  existing universal link, so it opens the route ready to start. **Without the app** it lands on the existing `/open` page,
  which says how to get the app. **No web page with the route's map for now** (owner, 2026-11-15: keep the get-the-app
  page), so no new path.
- **My outreach:** every walk saved on this phone, and your walks from wherever you're signed in, with totals; *Count me
  by name* for signed-in walkers (off: counted anonymously). Signed-out walks are kept on the phone (the last 200).
- **Offline maps:** each region with its size; the one you walk in is kept by itself, others by choice; Remove takes the
  files off the phone. The map reads the kept file by byte ranges through the native layer, so it works with no signal.

Open for the owner:

- **Android:** the background location and kept files are iOS only; Android's data layer is still a stub.
- **Design, left open after two review rounds** (impeccable's finish reviewer, 2026-11-15): day routes are rows, not the
  line of stops the end of a walk uses; the walking screen keeps the tab bar (the shell has no per-page way to hide it)
  and leaves a band above it; the "Your walk is on" card has no chevron; the plan map's skip markers are unlabelled dots;
  My outreach shows what's waiting in one line with Try again (Discard per change stays in More → Storage).
- **Tested on the Simulator only:** App Attest can't run there, so every outreach write was refused by the dev backend
  (as designed); the pieces, marks and claims queue correctly. The first real-phone walk is the end-to-end check.

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
   the label), scissors (draw across the streets where a section should split; only the streets the line
   crosses are severed, at the crossing, so each piece is what stays joined on either side; the pieces
   and their letterboxes are previewed while drawing, and a cut that leaves the section joined elsewhere
   says to keep cutting), brush (drag along a street to paint just that stretch into a section, drawn
   live as a brush stroke with its letterbox count; carrying on past a corner continues into the next
   street, ends snap to a corner within 15 m, and a click takes the whole street), lasso (loop streets
   into a section or a new one), eraser (the same gesture, leaving a stretch or a street out that day)
   and a draggable start pin. A section can own part of a street: painting over part of another
   section's street takes only that stretch, and the street is split into sub-streets at the stretch
   ends, which the splitter, the walk planner and every tool treat like streets. Neighbouring
   stretches with the same owner join back up. A part street is walked to the end of its stretch and
   back, and walkers see it as "Smith St (part)" with the stretch drawn; without house numbers it can't
   say "to no. 40". Undo and redo cover every edit; Rebalance evens out the
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

The 2026-10-27 draft; *Foundations* above is what was built and wins where they differ.

A feature module `Features/Outreach` (§8). Feeds (§5, §10):

| Feed | Records | Window | Notes |
|---|---|---|---|
| `outreach.regions` | Each region: name, core and bounds, version, and the URLs and sizes of its `.streets` file and map file | All | Public. The street graph is a file per region in the media store, not a feed record (*Regions and maps*); replaces the draft's `outreach.streets` |
| `outreach.days` | Outreach days and their routes | Next 4 weeks and the current day | Routes are lists of street sides, or stretches of them (street, from, to) |
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
  from a polygon (`DraftRoutes(polygon, target, meeting point)` returns sections to review).
- **Building the street graph and the map** is a server job, not a request path, in its own `Maps` service
  (*Regions and maps*); the POC's `build_data.py` was that job in miniature.

## Where routes are made: on the phone

Recommended: the route engine is shared C# (AOT-safe) and runs **on the phone** from the synced street
graph. Routes appear instantly, work without signal, and need no server call. The server runs the same
engine in admin for day routes. The phone sends only what was walked.

## Maps

- **MapLibre in the web view** (the POC uses it), with vector tiles. It is the same in the app and the
  admin panel, and styles can match the brand. Native map SDKs (MapKit, Google Maps through a
  Capacitor plugin) would draw under the web view and split the UI; not worth it here.
- **Tiles: self-hosted** (owner, 2026-11-14; built 2026-11-15): one PMTiles file per region in the media store,
  read by range requests (*Regions and maps*). The POC used OpenFreeMap.
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
