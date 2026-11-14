---
version: 1
slug: "happ-platform-ui-wwwroot-map-map-style-js-7455e02c"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/map-style.js"
related_targets: []
---

# Outreach map: the base map

Mode: Operate. The base map under every outreach screen (walking on the phone, planning and the live board in the admin
panel, light and dark) and the printed outreach-day maps. Decision page: `.impeccable/mocks/outreach-map/`
(payload-1.json); the owner was asleep, so the station built the lead look and kept the other two on the page.

- **Job:** a walker glances at it mid-street, one-handed, in daylight or at dusk; a planner reads a whole suburb at a
  desk. Both need the route and coverage drawn on top to be the loudest thing, and the map to say why a walk stops
  (a park, a shopping centre, a main road).
- **Content:** OpenStreetMap vector tiles (Protomaps schema) per region, street names in Montserrat, suburb names in
  small capitals. No POI icons, no sprites, no road shields.
- **On top (not this style's job, but it must leave room):** the walk in deep aqua (bright aqua in dark) with a white
  (night) edge, walked streets in ink-3 at 45%, your position as the orange marker.

## Direction contract

THESIS: The map shows what bounds a walk: parks, non-homes and main roads read at a glance, homes' streets stay white
and quiet. It refuses the grey category basemap that can't explain a short walk.
OWN-WORLD: Warm bulletin ground (#f7f4ee; night #141d31), sage parks, beige non-homes, white streets with sand edges,
main roads with a firm brown edge, Montserrat 400 street names and 600 small-capital suburbs, no icons.
STORY: The walker sees their walk first, then why it stops; the planner sees a suburb's pockets before drawing routes.
FIRST VIEWPORT: At walking zoom (16), the route and the orange position dominate; streets are white channels,
labels ink-2 on a land-coloured halo; at planning zoom (14) suburbs and main roads structure the view.
FORM: "What bounds a walk", first of three on the decision page; seed:fb8d700ae59edbba.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
