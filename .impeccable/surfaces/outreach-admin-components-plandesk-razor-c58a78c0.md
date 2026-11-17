---
version: 1
slug: "outreach-admin-components-plandesk-razor-c58a78c0"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/PlanDesk.razor"
related_targets: ["src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/PlanPane.razor","src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Planning/PlanDrawing.cs"]
---

# Plan a day: several areas, each one route or split (admin panel, Outreach → Map → Plan a day)

**Mode:** Operate. **Audience:** the outreach leaders planning a Saturday at a laptop (1280–1440 wide). **Job:** put
several areas on one day; each is either one route exactly as outlined, or split into routes by the app; see where
areas overlap and who keeps the streets; publish. Feedback #124 (owner, 2026-11-17). Interview (owner, 2026-11-17): a
hand-drawn route is outlined by clicking its corners; a hand-drawn route keeps its streets where it overlaps an area the
app splits (between two of the same kind, the one drawn first keeps them); most days are mixed.

**States:** no areas yet; one split area (every day before this change); split + hand-drawn; only hand-drawn; an
overlap; an area whose streets all went to other routes; drawing (route or area); chip menu open; published.
Range: 1–8 areas, 1–30 routes.

**Chosen:** card *Areas live on the map, each with its own chip* (`.impeccable/mocks/route-areas/map-chips.html`).

## Direction contract

THESIS: An area is a thing on the map, not a setting in a form: each outline carries its own chip, and that chip is
where you say what it is (one route, or split) and where you change or remove it. Refuses a separate "areas" list or a
day-wide mode switch.

OWN-WORLD: The admin bulletin and the planner as built: navy sidebar, white drawer, brand base map, the nine route
colours with their capsule labels, the navy tool rail. Split areas are a dashed navy outline; a hand-drawn route is a
solid outline in its route colour. Overlaps are hatched in the brand's warm grey-brown, labelled with who keeps them.

STORY: The leader draws a route (outline, Finish, it's a route), draws the next, drops a saved area to split beside
them, sees at a glance which streets each one owns, switches an area between one route and split from its chip, and
publishes the lot.

FIRST VIEWPORT: The Map tab's Plan a day as today: dock, the day's drawer (title, lead, Plan / On the day, a caption,
filter chips All · each split area · Drawn by hand, the routes table with a pen mark on hand-drawn rows, Change how big
routes are, Save this area), its foot (published line, Rebalance, Regenerate, Publish). On the map: the rail with two
outline tools (Draw a route, Draw an area to split), each area's chip on its top edge, the hatched overlap with its
label, and a key bottom left.

FORM: structure 6 of 7 on my ordered list (areas live on the map), dealt as the lead. seed:37e52cf59b80e4aa

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Decisions for the build

- Rebalance and Regenerate rework only routes from split areas; hand-drawn routes, routes made with the lasso, and
  locked routes stay as they are.
- Taking an area off the day removes its routes; its streets in other areas are left out until Regenerate.
- Existing days read as one area split by the app.

# Map feel: walked-only stretches, and labels that give way (feedback #125–#127, owner, 2026-11-17)

**States added:** the full map zoomed out to the suburb (routes a few hundred pixels across); a route that walks
stretches with nothing to deliver; a route really in pieces; a chip's menu open; pinch and scroll over anything on the
map. Interview (owner, 2026-11-17): zoomed out, a route shows only its number and colour; the chip is mostly a control.

**Chosen:** card *Numbers on the map, chips in a strip* (`.impeccable/mocks/route-map-feel/strip.html`).

## Direction contract (map feel)

THESIS: The map carries one mark per route, a numbered disc in its colour, and the controls leave the streets: every
area's chip lines up in one strip along the top of the map. Refuses a chip and a label per route on the streets, and
labels that pile up when zoomed out.

OWN-WORLD: The planner as built: the nine route colours, the brand base map, the white (night: surface) map chips on
the map's float shadow, the navy tool rail. Walked-only stretches are round dots in the route's colour, the same on
printed maps and walkers' phones.

STORY: The leader zooms out to see the whole day, reads five coloured numbers, zooms in and reads each route's figures
beside its number, sees the dotted stretches joining a route's parts, and opens a route's menu from the strip.

FIRST VIEWPORT: Full map, panel on the left; rail top left; the chip strip top, from the rail's right to the Leave
full map button; the hover card below the button; each route's disc with figures beside it from zoom 15, the disc
alone below; the key bottom left with "Walked, nothing to deliver".

FORM: structure 7 of 7 on my ordered list, dealt as the lead. seed:03ae8a0465d9fc03

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
