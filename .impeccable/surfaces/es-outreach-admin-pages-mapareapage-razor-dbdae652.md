---
version: 1
slug: "es-outreach-admin-pages-mapareapage-razor-dbdae652"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Pages/MapAreaPage.razor"
related_targets: ["src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Pages/NewMapAreaPage.razor","src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/MapAreasGroup.razor"]
---

# Admin panel: Outreach map areas

Scope: the admin panel's map areas (Outreach → Settings → Map areas, then `/outreach/maps/{area}` and
`/outreach/maps/new`): finding a place to add an area by name or by pointing at the map, seeing its squares
(regions) and the download before it builds, switching an area's regions off and on, growing it on its edge, and the
build's progress. Mode: operate. Audience: staff holding `outreach.maps.manage` (the Outreach Manager, the Owner) at a
desk, a few times a year. Interview substituted by the owner's brief (owner away, 2026-11-15: "is there way(s) to quickly
and easily add additional areas and regions via the admin panel? ... let admins add or remove it"). Decision page:
`.impeccable/mocks/outreach-areas/` (A a wide sheet, B tiles in the settings card, C a map desk per area); the station
picked C; A and B stay on the page.

## Direction contract

THESIS: An area is a set of squares on the map, so it is worked on the map: each area opens on its own MapDesk page,
the squares drawn over the region's map, clicked to switch off, on, or add on the edge, the change held in the pane's
foot until saved. It refuses a list of coordinates and a form that only describes a place.

OWN-WORLD: The admin bulletin and the outreach base map unchanged: navy title band, the map edge to edge under it, the
460px pane ruled off at the right with a sand foot. Squares that are on in live aqua at 12%, switched off dashed in
ink 3, being added in orange ink, empty (no homes) quiet; labels as the planner's capsules.

STORY: The Outreach Manager opens Around the church, clicks the three south-side squares, reads that Carols 2026 uses
one and what phones lose, saves; later searches "Gympie", sets 15 km, sees five squares with homes and about 9 MB, and
adds it; the build's progress shows on its page and in Settings.

FIRST VIEWPORT: Title band (area name, its facts, ⋯ with Rebuild, Rename, Delete); the map with its squares and their
names, the hint top centre, the hovered square's facts top right; the pane: state and progress, a warning when a region
going off is used, regions grouped Changes / On / Off with switches; foot: what the change costs and frees, Cancel left,
Save right.

SIGNATURE: the squares themselves as the controls: what's on, off, empty and being added reads on the map in one glance,
and a click is the edit.

FORM: dealt structure 1 of 7 (map desk per area), chosen by the station; seed:6669aea0cb33630b.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
