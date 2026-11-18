---
version: 1
slug: "each-gsbc-churchapp-features-outreach-app-196a1598"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.App"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/Maps"]
---

# Outreach on the phone

Scope: the walker's side of Outreach in the app (Operate). Outreach page (`/outreach`), plan a walk, the walk, the end of
a walk, doorknocking, outreach days and a route opened from a printed QR code, Offline maps, My outreach; the shared
map they all draw on. Audience: anyone with the app, signed in or not, often walking with the phone in a pocket,
outdoors in daylight, one hand busy with flyers. Job: get a route that avoids what the church walked recently, walk
it without ticking houses, mark the few things worth marking, and leave an honest record. Decisions: docs/features/outreach.md.

## Direction contract

THESIS: A walk is read at a glance. The street you're on and its side lead in the masthead's navy at display size, what
comes next under it; the map supports, never leads. Refuses the map-app default (a full-screen map with a small card).

OWN-WORLD: The bulletin: navy masthead, white page, sand boxes, hairline rows, Montserrat 700/600/400; deep aqua for the
walk and actions, orange only for "you" and progress; the base map from mapStyle. Marks are two wide quiet buttons in
the thumb zone.

STORY: Got an hour? Pick a length, see the plan and why it stops where it does, start, put the phone away; glance to
see where you are; mark No junk mail or a chat; finish and fix what the GPS got wrong as a line of stops.

FIRST VIEWPORT: Walking: navy panel (status line, street name at display size, side and letterboxes, next turn), the
map filling the middle with the walk, you and recent coverage, then progress, No junk mail and Spoke to someone, and
Pause · ⋯ · Finish along the foot. Outreach page: masthead, the "Got an hour?" box with length and Plan my walk.

FORM: Words first, first of three dealt (structure 3 on the ordered list); raised from the running order (end of a walk
and day routes as a line of stops). seed:8513cbb11e29b7c1

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Decided 2026-11-17 (owner, #110–#115)

- No *Around you* map on the Outreach page: the first map you see is Your walk's, which knows where you are.
- What you carry is chosen on Your walk only (its *Change the walk* row opens the sheet; a doorknock picks one campaign
  in `Segmented Rows`), and the plan redraws at once.
- Your dot carries a soft orange fan for the way you face (`.impeccable/mocks/outreach-heading/`, A): from the course
  while you walk, otherwise the compass; it widens and fades as the compass gets less sure and goes when it has no idea.
  It sits under the drawn walk, the dot on top.
