---
version: 1
slug: "reach-admin-components-mapworkspace-razor-e436dcaf"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/MapWorkspace.razor"
related_targets: ["src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Pages/OutreachDesk.razor"]
---

# Outreach → Map: one workspace for everything on the map (admin)

**Mode:** Operate. **Audience:** leaders of the ministry running outreach and the staff who look after maps, at a
laptop (1280–1440 first) or a monitor. **Job:** see where outreach has been and what it skips, and act on the map:
mark streets not done, add a skip area or address (typed or drawn), deal with walkers' reports where they happened,
keep saved areas, switch map regions off, on or add them, and plan an outreach day's routes. Replaces the Coverage,
Places, Areas and Reports tabs, the Map areas pages under Settings and the planner page (owner, 2026-11-15: "a single
large map admin panel … a map, with a good overlay with tools/etc, collapsable panes … all in one place").

**Settled with the owner (interview 2026-11-15):** a Map tab in Outreach (Campaigns, Map, Days, Numbers, Follow-ups,
Settings); the day's planner is a mode of the workspace; on arrival it shows whatever you had last on this computer
(first time: coverage); laptops first. Tickets #105 (type an address), #106 (drawing instructions lost in the
attribution), #107 (places as a map screen).

**States:** nothing chosen; a job's drawer open or folded; drawing (polygon, lasso) with Finish / Undo last point /
Cancel; a thing chosen (card); the view outside our maps; a region still building; read-only jobs for people without
the permission (the job is hidden when they can't see it); planning a day (Plan / On the day).

**Chosen:** card *A dock of jobs down the map's left edge* (decision page `.impeccable/mocks/outreach-workspace/`,
the roll's lead; the other two kept).

## Direction contract

THESIS: The map is the page and every outreach job is a drawer pulled out beside it, so the layers stay drawn
underneath whatever job you're in. Refuses the category default of one tab per job each with its own small map.

OWN-WORLD: The admin bulletin: navy sidebar, white work area under the title band and tabs, sand for shaded boxes. The
dock is a white 76px strip of icon-over-name buttons, the open job in Selected Fill with an orange marker bar at its
edge and orange counts; drawers are white panes ruled off by hairlines; floating things on the map are white cards on
the Pop shadow; the drawing bar is navy with white buttons. The brand base map, the coverage ramp in deep aqua, skip
areas in danger at 12% dashed, reports as orange flags, the edge of our maps hatched.

STORY: The leader opens Map, sees coverage as they left it, pulls out Places, types an address or draws an area with
an instruction bar they can't miss, clicks a report flag and resolves it in its card, folds everything away to look at
the whole map, or picks a day and plans its routes on the same map.

FIRST VIEWPORT: Title band "Outreach" with the job's actions at the right, tabs under it; below, full bleed to the
window's foot: dock (76px) at the left edge, the open job's drawer (320px, folds with a chevron in its head), then the
map. On the map: the drawing bar top centre while drawing (instruction, Undo last point, Cancel, Finish in white), the
chosen thing's card top right (330px), zoom at the right edge, the coverage legend and figures bottom left, the
attribution bottom right, never under a control.

FORM: structure 5 of 7 on my ordered list (icon dock with fold-out drawers), the dealt lead. seed:be1e16b7f838c0e7
Signature move: the drawers swap but the map never reloads: pulling out another job keeps the view and every layer
that's switched on, and a click on any layer's thing opens its card whichever drawer is out.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Open
- Whether the number badge on Reports counts new reports only (built so) or all open ones.
