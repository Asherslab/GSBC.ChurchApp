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

OWN-WORLD: The ministry's navy hero (kind kicker in beige, title, one line, its one action in marker orange or a quiet chip), the
sliding orange section tabs, audience boxes at the 14px radius (team navy 7%, families aqua 8%, parents orange 18%, everyone sand)
with a pill-size uppercase "For …" head; hairline rows with 34px tiles; a leader's People and ⚙ tabs in the same strip.

STORY: A parent opens Impact Kids, sees "Mia attends", reads the families box. A leader opens People, answers Sam's ask, links
Noah's parent from a sheet. Staff pick a ministry in the tree and do the same in a table. A newcomer taps Choose a group and
pages through the next Life Groups.

FIRST VIEWPORT: Phone: hero with its action, tabs About · News · Calendar · Files (· People · ⚙ for leaders), then the first
audience box. Directory: Yours, serving teams, Life Groups. Admin: tree 300px, the ministry's tabs, a sand person panel 340px.

FORM: ministry page 5 of 7 ("bulletin board"), seed: none (unavailable); leading 4 of 7 ("People tab, two lists"),
seed: none (unavailable); admin 3 of 7 ("tree beside the ministry"), seed: none (unavailable); Life Groups 7 of 7 ("choose one,
page by page"), seed: none (unavailable).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
