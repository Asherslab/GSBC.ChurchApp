# Places off site: an address, a pin, a note and a map

Status: being built, 2026-11-17 (branch `feature/places-on-a-map`). Asked for in the owner's feedback #121 (dev,
2026-11-17, admin panel → Outreach → Days, *Where people meet*):

> let them actually select a real location on the map for this. this actually can/should be a generic feature across
> the app, anything that isn't on site (site will have a site map feature added later). allows people to click on the
> location and view in app or Apple Maps/Google Maps/etc. Display as a street address, possibly with notes (e.g. "park
> on XYZ street"), etc. as said, this should be a standard component across the whole app.

"Place" already means two things in this codebase (a place in the app, `Destinations`; a place on the church site,
[site-map.md](site-map.md)), so in code this is a **location** (`Location`, `LocationField`, `LocationCard`), and on
screen "where it is" or "a place off site".

## Decided

### The owner's answers (2026-11-17, in the station's chat)

1. **Addresses come from our own geocoder, not an outside service.** Asked whether to search Queensland's address
   layer live, the owner asked for "proper geocoding" because "some of our stuff *could* come up in NSW", hosted by
   us ("no pricing. i'm asking about hosting our own"). Chosen: **G-NAF in our own Postgres**: Australia's open
   national address file (every state, about 15 million addresses, released quarterly), plus named places from
   OpenStreetMap (suburbs and towns, parks, camps, halls), behind a gRPC search with a reverse lookup for a dropped
   pin. Rejected: Photon and Nominatim (OSM's street numbers in Australia are patchy, and each is a new service to run)
   and Pelias (several services and Elasticsearch). Searching sends nothing outside our servers.
2. **Our own maps everywhere.** A place outside the built outreach regions gets a small street-level map file of
   about 2 km around it, cut from the same Protomaps build as the regions by the same code, stored by its content in
   the media host and read by range like a region's. Apple Maps and Google Maps are only for *Open in Maps*. The owner
   asked how this differs from the region files: it is the same thing, smaller (100 to 300 KB), with no streets or
   letterbox file, and owned by the place rather than Outreach. Approved as its own row in AGENTS.md's non-gRPC paths
   (2026-11-17), with a condition: **"i don't want duplicates taking up storage on the phone for offline. i.e.
   duplicates between a region and a place."** A phone that keeps a region's map shows a place inside it from that
   region, and never keeps the place's own file as well.
3. **Choosing a place on the phone is an approved direct call** (2026-11-17): the address search, the nearest address
   to a dropped pin and preparing the place's map, online only. Offline, a leader can still type the address and note,
   and it saves through the outbox as usual. Showing a place never needs one: the address, note, pin and map file come
   with the synced record.
4. **Everything with a location switches over in this change:** the outreach day's meeting point, an event's place,
   and a ministry's or Life Group's *meets*. "the whole feature here needs the option (when it's actually built) to
   switch between off site and on site locations. because most ministries will be on site." So a location is either
   **at church** (a quick choice with no map; a room on the site map later) or **off site** (address, pin, note, map).
   Old text locations stay readable as they are.
5. **The design** (decision page `.impeccable/mocks/places/`, the owner's pick 2026-11-17): **A, a row and a picker of
   its own.** A place shows as one row (pin icon, name, address, note, a small square of map at its end); choosing one
   opens its own picker (search and results beside the map, then name and note; a page of its own on the phone).
   B (search first, the map confirms) and C (the map strip) stay on the page.
6. **A place has an optional name** above its address ("Name, address, note").
7. **A home** (owner, 2026-11-17): a place can be marked as a home. The people in that ministry, or going to that
   event, see the street address, pin and map; everyone else sees only the suburb ("A home in Albany Creek"). Access
   decides who's in.
8. **Tapping a place on the phone** opens a full-screen map in the app (from the kept map, so it works offline) with
   the address, the note, and *Open in Maps* and *Directions*, which hand over to Apple Maps, Google Maps and the like.

## Built (2026-11-17)

### The data

- **`Location`** (Platform.Contracts): `Kind` (at church or off site), `Name`, `Address`, `Suburb`, `Note`, the pin
  (`Latitude`, `Longitude`), `Home`, its `Map` (a `LocationMap`: the file's URL and key, its bounds and zoom), and
  `Text` for a location typed before places had a map, shown as it is. `SeenFromOutside()` is what someone outside a
  home's group gets: the suburb and nothing else (no name, address, note, pin or map).
- **Saving** goes through `LocationRules.Cleaned` (Platform.Api): trimmed and clipped, a pin only when it's on Earth.
  `LocationRules.Text` is the one line for places that only take words (the printed route maps, iCal, the website).
- **Where it's stored:** an outreach day's `Meeting` and a ministry's `Where` are jsonb columns; an event's place keeps
  its `Name` and `Address` (the website, iCal, the digest and change notes read those) and adds its `Location`.
  `EventPlaces.LocationOf` reads an old event place as a place without a pin.

### Finding a place: our own geocoder

In the Maps module (`Outreach.Maps.Api`, *Claude's decision*: it already holds the address search's logic, the map
sources, the tile cutter and the media bucket; features still never reference each other, because the service is the
app-wide gRPC contract `ILocationsService` in Platform.Contracts, and the Maps module is what serves it).

- **G-NAF** (`GnafImport`): data.gov.au's package lists each release; the newest GDA2020 zip (about 1.7 GB) is
  downloaded to a temporary file and each state's tables are copied into temporary Postgres tables (`COPY`), joined in
  SQL into `outreach_maps.national_addresses` (principal, unretired addresses with their default geocode; about 15.9
  million) and `national_localities` (suburbs and towns with their points). Each import is a numbered release: searches
  use the newest finished one, and older ones are deleted only once a new one has finished, so a search never sees a
  half-imported release. An interrupted import is forgotten and starts again. The runner (`NationalAddressRunner`,
  its own advisory lock) checks for a new release once a day and imports when the name changes (quarterly). Settings:
  `Outreach:Maps:NationalAddresses`, `ImportNationalAddresses`. The key column is built in SQL to match
  `AddressSearchText.Key`, so the outreach address search's parsing, street-type spellings and ranking are shared.
- **Searching** (`NationalAddresses.FindAsync`): an address with a number; a street without one (one result per
  street); suburbs; buildings by their G-NAF building name. Each result says how far and which way it is from the
  church. **Nearest** gives a dropped pin the closest address within 200 m, then 1 km, then 5 km.
- **Not yet:** parks, camps and halls without a G-NAF building name aren't found by name; the pin and a name cover
  them. OpenStreetMap's named places (the owner's choice included them) need a read of the whole Australian extract
  for the centres of parks drawn as areas; left for a follow-up.

### A place's map

`PlaceMaps` cuts a map file for the 2 × 2 block of zoom-15 tiles nearest the place (about 2.2 km a side; the place is
at least 550 m from its edge), with every lower zoom, from the newest Protomaps build, by the same `PmtilesExtract` as the
regions. Places in the same block share one file (`outreach_maps.place_maps`, one row a block), stored by content in the
public bucket (`immutable`). Australia only. Each feature says which map files it uses (`ILocationUses`: outreach days,
events, ministries), and once a day a map nothing has used for two days is removed, its file too unless a region uses it.

### On the phone

- **Showing:** `LocationRow` (a `ListRow`: the pin icon, the name, the address, the note, and a 64 px map square) opens
  `LocationSheet`: the map, the note, *Directions* and *Open in Maps* (Apple Maps or Google Maps; on Android one
  *Open in Maps* through `geo:`, which offers the phone's own choice). The pin is the shared `MapPin`, as a navy pin.
- **Kept for offline** (`LocationMaps`, a startup task): each feature lists the places on the phone
  (`ILocationSource`); a place inside a region map the phone keeps whole is shown from that region
  (`IKeptMapCover`, Outreach's `OutreachLocations`), and only the others keep their own file (`places/<key>`) in the
  kept files. `PlaceMapPlan` works out what to keep and remove, so a place file and a region are never both kept (the
  owner's condition), and a place no longer on the phone loses its file.
- **Choosing** (`LocationField` opening `LocationPicker`, a wide sheet): At church (with an optional note) or Somewhere
  else: the search and its results (or places used before) beside the map, then Address, Name, Note and, where the
  record allows it, *This is someone's home*. Tapping the map moves the pin and fills in the nearest address; a pin
  near the map's edge gets the next block's map. Offline, the search says so and the address and note are typed.

### Who sees a home

A ministry's directory row (synced to everyone) carries its place as seen from outside. When it's a home, the whole
place goes in `ministries.places`, one row per audience (attendees, parents and team) with the ministry's own
audience, so Access decides who gets it; the phone lays it over the directory. Online, the server gives the whole place
only to someone who may see one of those audiences (`MinistryViews.SeesHome`). *Claude's default:* events have no
"home" choice, because an event is only shown to its calendar's audience; a private calendar already keeps an event
and its place to its people.

### What changed where

- Outreach: the day's *Where people meet* is a place; the day page on the phone shows it as a row; the printed route
  maps print `LocationRules.Text`; the planner's meeting-point pin tool is gone (the pin is the place), and a day at
  church plans from the church's point.
- Events: the place picker is the place field, with places used before; the event page shows the row.
- Ministries: *When it meets* (text) and *Where it meets* (a place, may be a home) are separate; the About tab and Life
  Groups show both. Old "when and where" text stays as it was, labelled *When and where it meets* until a place is set.
