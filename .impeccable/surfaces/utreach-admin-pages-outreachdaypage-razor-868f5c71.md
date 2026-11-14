---
version: 1
slug: "utreach-admin-pages-outreachdaypage-razor-868f5c71"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Pages/OutreachDayPage.razor"
related_targets: ["src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/DaysDesk.razor"]
---

# Outreach days: planner, printed maps, live board (admin, and the board on the phone)

**Mode:** Operate. **Audience:** leaders of the ministry running outreach, at a desk before the monthly outreach
Saturday (planning), and standing at the meeting point on the day (the board, often on a phone). **Job:** set up a day,
split an area into even walkable routes and check each by hand, print one A4 map per route with its QR, then on the day
see which routes are claimed and walked and send late arrivals to the gaps. Replaces Google Maps screenshots in
Illustrator. Settled by the owner (docs/features/outreach.md): the splitter and its tools, homes only, routes
reviewed before publishing, people pick routes themselves.

**States:** no days yet; a day with no routes; drawing an area; a draft with sections needing review; published;
during the window (live); after (what got done). Range: 1 to 20 routes, 50 to 400 letterboxes each.

**Chosen:** card C, the map and the routes table (decision page `.impeccable/mocks/outreach-planner/`, A and B kept).

## Direction contract

THESIS: A day is one frame: the map and its routes table side by side, the same before and on the day. Refuses the
category default of a form for the day, a separate map editor, and a separate dashboard that each lose the others.

OWN-WORLD: The admin bulletin: navy sidebar, white work area on a title band, sand for shading. The map is the brand
base map; routes are a fixed set of nine distinguishable route colours, each with a numbered capsule label (number,
letterboxes, minutes) on white with the route colour as its ring. Selection fades the rest; the meeting point is the
orange marker.

STORY: The leader sees the whole day at once (how many routes, how big, what still needs review), fixes the odd ones
with the tool rail, publishes, prints, and on the day reads progress off the same table and map.

FIRST VIEWPORT: Title band with the day ("Outreach Saturday, 6 December", window, meeting place, campaign, totals) and
Print maps and a ⋯ at the right. Below, full bleed: the map (left, all remaining width) with a floating tool rail at
its top left (select, scissors, brush, lasso, eraser, start pin, then undo and redo), the selection's actions floating
at its foot; a 440px pane at the right with Segmented Plan / On the day, a caption of where the routes came from, the
routes table, and its foot: Rebalance and Regenerate at the left, Publish at the right.

FORM: structure 3 of 7 on my ordered list (map and table), dealt card C. seed:9406ec8e2afd524e
Signature move: on the day the routes table becomes a departures board: each row's state (Open, Claimed, Walking,
Done) and a walked bar, ranked by what needs a person; open routes rise to the top for late arrivals.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Open
- The owner may switch to A (board) or B (printed sheets) on the decision page in the morning.
