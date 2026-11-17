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
