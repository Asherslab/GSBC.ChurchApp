---
version: 1
slug: "outreach-admin-map-overview"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/OutreachAtlas.cs"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/map-view.js", "src/Platform/GSBC.ChurchApp.Platform.Ui/Maps/MapView.razor", "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/OutreachMap.razor", "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/MapAreasJob.razor"]
---

# Outreach → Map: looking past the edge of our maps (admin)

**Mode:** Operate. **Audience:** staff who look after maps and leaders planning outreach, at a laptop or monitor.
**Job:** see all of Australia under the outreach map so adding an area, and adding regions on an edge, is easy to
place; know at a glance where walkers have maps. Part of the Map workspace
(`reach-admin-components-mapworkspace-razor-e436dcaf.md`).

**Settled with the owner (2026-11-15):** a static low-zoom Australia overview (z0–10) for the admin panel only, never the
app; past the edge on every job of the Map tab; outside Queensland is shown but areas can't be added there; placing needs
town and suburb names, roads and built-up land.

**States:** inside built regions (as before); outside them (overview only); a region waiting, building or switched off;
outside Queensland; the overview not built yet (the map behaves as before, held to our regions).

**Chosen:** card *The ground says where walkers have maps* (`.impeccable/mocks/outreach-overview/ground.html`, the
roll's assigned lead, picked 2026-11-16 with no steer).

## Direction contract

THESIS: The ground itself says where walkers have maps: built land is drawn clear, everything else sits under a grey
veil, and over the Queensland border the veil deepens and hatches. Refuses the default of a locked map with a hatched
"nothing here" void.

OWN-WORLD: The brand base map underneath everywhere, the overview's own roads and names in the same style; a flat
slate veil (ink-3 at about 30%) outside built regions; outside Queensland a deeper veil with the existing reach hatch;
waiting regions dashed aqua, switched-off ones hatched and dashed grey; a white key card on the Pop shadow, "Where
walkers have maps", with one swatch per state.

STORY: Staff open Map areas → Add a map area, zoom out past the church's edge, see Gympie's streets and names under the
veil, click it, see the squares and their letterboxes, add it. In Coverage they pan out, the zoom stops at suburb
level, and a status line says coverage is drawn where walkers have maps.

FIRST VIEWPORT: unchanged workspace; the key bottom left of the map while any veil is in view (the coverage legend takes
that corner when it shows, and the key sits above it); the outside status top centre, below Layers when the map is narrow.

FORM: structure 4 of 7 on my ordered list (status as ground colour with a key), the roll's assigned lead. seed:7baacb882dd94571
Signature move: one map from the whole of Australia down to a letterbox, with the veil's edge as the edge of our maps.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
