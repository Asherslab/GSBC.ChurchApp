---
version: 1
slug: "rs-gsbc-churchapp-features-rosters-client-0f41f49a"
primary_target: "src/Features/Rosters/GSBC.ChurchApp.Features.Rosters.Client"
related_targets: ["src/Features/Rosters/GSBC.ChurchApp.Features.Rosters.Admin"]
---

# Rosters: a ministry's Roster tab, on the phone and in the admin panel

Scope: Ministries Stage 3. The Roster tab inside a ministry (`/ministries/{id}/roster`) for a team member, a parent of a young
person on the team, and a leader; its sheets (a date, putting someone on, swaps, days away, positions, how it works, adding a
date); the grid; the Hub's *Your roster* tile. Mode: operate. Phone first; the admin panel shows the same tab, wider.
Sketches and the decision page: `.impeccable/mocks/rosters/decision.html`. Picked by Claude on 2026-11-11 while the owner was
asleep; every pick is listed there for the owner to redirect.

Audience and job: volunteers saying whether they can do a date, asking for a swap, marking days away; parents doing it for a
child; lay leaders filling dates on the phone; staff and leaders filling a term at a computer. Constraints: unmet requirements
flag and never block; who sees how much is the ministry's setting, enforced on the server; the phone reads only its store and
writes through the outbox.

## Direction contract

THESIS: What a volunteer opens the tab for comes first: "when am I on, and can I do it", answered in one tap. The roster
itself follows as one hairline row per date, a sheet per date, or the paper roster's grid. Refuses a feed of date cards and
refuses a separate leader world.

OWN-WORLD: The ministry's masthead and tab strip; *You're on* rows led by a warm day tile with small primary and quiet
buttons; hairline date rows led by a sand day tile with positions on one line; a *By date | Grid* segmented control; the
grid as a sticky-first-column table with tinted cells (aqua answered, sand waiting, struck through declined) and dashed
"Put on" gaps; flags as marker-ink words, never a block.

STORY: A volunteer opens Production, sees "Cameras · Sun 15 Nov · 6pm", taps *I can do it*. A parent answers for Tom. A
leader opens a date, puts someone on past an "Away that day" flag. Staff fill a term in the grid.

FIRST VIEWPORT: Phone: masthead, tabs, *You're on* with its buttons, then *The roster*. Admin: the tab at desk width
opening on the grid, eight dates across.

FORM: phone tab 4 of 7 ("a row per date, a sheet per date"), seed: ab1474082d42122057ad7b5d06994755; admin 1 of 7 ("the
term as a grid"), seed: ab1474082d42122057ad7b5d06994755.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md
