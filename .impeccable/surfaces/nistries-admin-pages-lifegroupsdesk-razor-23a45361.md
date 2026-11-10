---
version: 1
slug: "nistries-admin-pages-lifegroupsdesk-razor-23a45361"
primary_target: "src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Admin/Pages/LifeGroupsDesk.razor"
related_targets: ["src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Client/Components/LifeGroupsView.razor"]
---

# Life Groups desk (admin panel)

Mode: Operate. Desktop admin page, People → Life Groups.

## Job

Staff and the Life Groups planner come here mostly to plan and fill the next set of Sundays; checking the set now on is
secondary, and finished sets are rarely opened (owner, 2026-11-10). Each set has its own groups; a group almost never
runs in two sets (owner's steer: "most life groups will be unique to a specific round"). A few all-year groups follow
every set and can sit one out. The page must stay short as the year fills with sets, and keep finished sets and
archived groups findable without showing them by default.

## Direction contract

THESIS: Sets of Sundays move left to right through their life (being planned, shown in the app, now on, finished), so
the page reads as a board of where each set is, not a growing pile of cards in date order.

OWN-WORLD: the admin panel's own system: navy sidebar, white page, sand fields, 14px cards on hairlines, aqua for now
on, marker orange for the set being worked on and for anything missing, Montserrat. No new colours or components.

STORY: Staff see at once which set is being planned and what it still needs, add its groups in place, see which
all-year groups are off for it, and fold finished sets away (archiving their groups when they're done with them).

FIRST VIEWPORT: under the title band and calendar row, a sand strip of the all-year groups as chips (an off one in
marker) with Archived groups at its right; below, four equal columns with counts: Being planned (the next set's card,
marker-outlined, its groups listed with any missing room in marker ink, + Add a group; later sets' cards under it),
Shown in the app (cards, or a dashed empty card saying when the next one shows), Now on (aqua-outlined card, week x of
y), Finished in this year (one line per set with its group count, a fold for more, Archive their groups).

FORM: Sets by stage, position 5 on the second ordered list; seed:7a59c4de3fb39e5e.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Open

- Earlier years: a year chooser on the Finished column (default) rather than a separate page.
