---
version: 1
slug: "s-outreach-admin-pages-outreachdesk-razor-9fdeefce"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Pages/OutreachDesk.razor"
related_targets: []
---

# Admin panel: Outreach desk

Scope: the admin panel's Outreach page (`/outreach`, `/outreach/{tab}`), stage 1: Campaigns, Settings, Follow-ups and
Reports. Mode: operate. The planner, coverage map, outreach days and live board are later tabs (stage 2). Audience: the
Outreach ministry's leaders and the Outreach Manager at a desk; follow-ups and reports also reach them by notification.
Interview substituted by the owner's brief and docs/features/outreach.md (owner asleep, 2026-11-14). Decision page:
`.impeccable/mocks/outreach-desk/` (three dealt structures; the station picked C).

## Direction contract

THESIS: Campaigns are records staff create and delete, so they get the Workbench (list, then the chosen campaign in the
record pane); settings are settings, so they get the grouped cards every settings page has. It refuses one long form
that mixes the two.

OWN-WORLD: The admin bulletin: navy sidebar, white work area opening on the title band, the orange tab underline, sand
fields, grouped settings cards with a sand foot, hairline-divided table rows, tabular figures for counts.

STORY: A leader opens Outreach, sees each campaign with its dates and this month's letterboxes, opens Carols 2026 to set
its dates and leaderboard, and moves to Settings to change the rest period. A follow-up notification opens the
Follow-ups tab with the person's first name and contact.

FIRST VIEWPORT: Title band "Outreach" with New campaign at the right; tabs Campaigns, Follow-ups (count), Reports
(count), Settings; the campaigns table (name, when, what's handed out, letterboxes this month) with the chosen campaign
in the record pane: PaneHead with ⋯ (Delete), its facts (letterboxes, km, days), the fields and Cancel / Save.

SIGNATURE: the campaign's own numbers (letterboxes, km walked, outreach days) at the head of its record, so "each
campaign has its own coverage" is visible, not explained.

FORM: seed:a95f774a928224cc, dealt structure 3 of 3 (campaign records), chosen by the station.

## Adaptations (2026-11-15, after the finish review)

- The campaigns table has two columns, the campaign (what's handed out and its dates as a caption under the name) and
  a bare, tabular *Letterboxes this month*: with the record pane open at 1024px a separate When column pushed the count
  out of the list.
- Settings save per group, each card with its own sand foot, as the pattern map's *A group of settings* asks.
- The campaign record's save bar docks at the pane's foot (`editor-foot`), as the event editor's does.

## Stage 2: coverage, numbers and places (2026-11-15)

Scope: three new tabs on the same page, Coverage, Numbers and Places, plus Regions as a group on Settings. Mode: operate.
Audience as above. Interview substituted by the owner's brief and docs/features/outreach.md (owner asleep). Decision
page: `.impeccable/mocks/outreach-coverage/` (A the map is the desk, B numbers lead, C a coverage map with a working
rail); the station picked C and built it; A and B stay on the page.

### Direction contract (stage 2)

THESIS: Coverage is a job of its own: a map with a ruled-off rail at the right, where the record pane sits on every
workbench, holding the campaign, the age legend, what to show and the selection with Mark as not done. It refuses the
map as a junk drawer of every pin, and refuses numbers hiding the map.

OWN-WORLD: The admin bulletin unchanged; the map is the outreach base map (`mapStyle`), coverage drawn as one
sequential deep-aqua ramp that fades to the base map's white exactly when the rest period ends; exclusions in danger,
dashed; No junk mail as small navy dots; the selection navy and dashed.

STORY: A leader opens Coverage, sees General outreach's last three months fade across the suburb, draws round an area
the council asked to redo, reads "86 street sides, 1,140 letterboxes" and marks it not done, with an undo. In Numbers
they read the quarter's letterboxes and who walked; in Places they draw a zone or pick an address on the same map.

FIRST VIEWPORT: Title band; tabs Campaigns, Coverage, Numbers, Places, Follow-ups, Reports, Settings; the map filling
the work area to the left, the 360px rail ruled off on the right: Campaign menu, the ramp "This week → rest period:
open again", two figures (streets done, share of letterboxes), Show on the map ticks with counts, then Chosen with Mark
as not done.

SIGNATURE: the age ramp: the same colour scale in the legend and on the streets, ending in the map's own white at the
rest period, so "open again" is visible, not explained.

FORM: seed:5d8bb9f46b4c42a0, dealt structure 3 of 3 (coverage rail), chosen by the station.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
