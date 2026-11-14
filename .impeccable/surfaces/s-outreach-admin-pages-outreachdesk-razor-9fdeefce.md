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
