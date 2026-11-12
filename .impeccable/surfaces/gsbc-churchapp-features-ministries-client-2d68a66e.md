---
version: 1
slug: "gsbc-churchapp-features-ministries-client-2d68a66e"
primary_target: "src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Client"
related_targets: ["src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.App","src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Admin"]
---

# Ministries: directory, a ministry, leading it, the admin panel, Life Groups

Scope: Stage 2 of Ministries. The phone's directory (`/ministries`) and a ministry (`/ministries/{id}` and its areas) as a visitor,
an attendee, a parent, a team member and a leader see it; joining (team or attending, for yourself or a child); a leader's People tab
and Settings on the phone; the same management in the admin panel; Life Groups. Mode: operate. Phone first; admin desktop first.
Sketches and the decision page: `.impeccable/mocks/ministries/decision.html` (chosen by the owner, 2026-11-10, on the impeccable page).

Audience and job: lay leaders without a desktop, answering asks and keeping their team and attendee lists (with each child's
parents) on the phone; parents and volunteers seeing what's for them; visitors finding where they fit; staff doing the same at a
computer. Constraints: who sees what only through Access; four audiences (everyone, attendees and parents, parents, team); Life
Groups never say "round" to anyone (owner); a Life Groups set is set up by staff in the admin panel only (owner).

## Direction contract

THESIS: A ministry reads like the Sunday bulletin's shaded boxes: in each area, what you may see is boxed by who it's for, team
first, so nobody wonders who else reads it. Refuses one mixed feed with tags, and refuses a separate admin world for leaders.

OWN-WORLD: The ministry's navy masthead (title, its one line as the note, no label above the title), its one action as the
white on-masthead button or a chip pill ("You lead it"), the area tabs (TabStrip link mode, orange underline), audience boxes
at the 14px radius (team navy 7%, families aqua 8%, parents orange 18%, everyone sand) with a pill-size uppercase "For …" head;
hairline rows with 34px tiles; a leader's People and Settings tabs in the same strip; the admin's tree beside the same areas.

STORY: A parent opens Impact Kids, sees "Mia attends", reads the families box. A leader opens People, answers Sam's ask, links
Noah's parent from a sheet. Staff pick a ministry in the tree and do the same in a table. A newcomer taps Choose a group and
pages through the next Life Groups.

FIRST VIEWPORT: Phone: hero with its action, tabs About · News · Calendar · Files (· People · ⚙ for leaders), then the first
audience box. Directory: Yours, serving teams, Life Groups. Admin: tree 300px, the ministry's tabs, a sand person panel 340px.

FORM: ministry page 5 of 7 ("bulletin board"), seed: none (unavailable); leading 4 of 7 ("People tab, two lists"),
seed: none (unavailable); admin 3 of 7 ("tree beside the ministry"), seed: none (unavailable); Life Groups 7 of 7 ("choose one,
page by page"), seed: none (unavailable).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Wave 2 (2026-11-12): the owner's picks on `.impeccable/mocks/ministries-wave2/`

Builds on the Pattern map; no new world. One decision round, two questions, both answered in the browser:

- **A Life Group's Calendar tab** (#56): *Its Sundays* goes. A change to one of its Sundays is a **News post for that
  Sunday** ("Tell the group in News", picked over a ⋯ menu on each Sunday and a strip of date tiles), optionally
  *No class this Sunday*; Coming up strikes the Sunday through and links the post under it. Linked calendars show their
  events while the group's set runs; each row's ⋯ menu holds Unlink, asking once (#50). Leaders link from the phone (#54).
- **A ministry's tab order** (#21): **drag with a live preview** (picked over arrows on each row and an order sheet):
  `ReorderList` with the tab strip previewed above, each row's ⋯ menu for Move up and Move down, the on/off switch in
  the same row; changes straight away.
- Settled by the Pattern map without a question: settings in groups (#12), the role menu in the person's pane or sheet
  with the add sheet staying open (#13, #14, #29, as chosen in the design-system round), Archive and Delete in the
  ministry's ⋯ menu, Workbench for the admin People tab.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
