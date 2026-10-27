---
version: 1
slug: "spikes-app-poc-outreach"
primary_target: "spikes/app-poc/App/Pages/Outreach"
related_targets: ["spikes/app-poc/App/Layouts/Hub/HubHome.razor"]
---

# GSBC app: Outreach (letterbox drops)

Scope: the Outreach area, reached from a Hub tile: an outreach-day block, Walk now, Areas, and the shared walking
screen. Mode: operate. The walker is outdoors, one hand holding tracts, glancing at the phone between letterboxes.

Decided by the owner, 2026-10-27: people mostly use it casually and in the moment (Walk now is the default); routes
for outreach days are ready-made by staff in the admin web view and claimed by walkers, several people per route;
an outreach day is a time window, not a place, so a walk from home during it counts toward the day; Areas are the
church's historical outreach areas (how they are made is to be reviewed). The church records streets walked and
"No junk mail" letterboxes, nothing about residents. No sign-in: a first name, asked once, for claims. Letterbox
drop first; doorknocking later.

Three rounds of dealt structures were shown (seeds 12d69949847adcb5, 06e6292cc5f84b5f, a7e0bc018c60383f); the owner
re-rolled twice and then asked for one recommended design.

## Direction contract

THESIS: Three ways in that read as three different things (a day the church planned, a walk made for you now, an
area the church has worked before) behind one calm page, and one walking screen they all share. It refuses the
map-first planner and the feed of identical cards.

OWN-WORLD: The app's bulletin look: navy masthead with the ring echo, white page, sand callouts, orange for
progress and "here", aqua-deep for actions, Montserrat. The map is quiet grey (Positron) so the navy route,
drawn as a two-sided ribbon, is the only strong line on it.

STORY: I have an hour: open Outreach, tap "1 hr", see the loop from where I am, Start. On an outreach day I see the
ready-made routes and who has grabbed them, claim one or walk from home and still count.

FIRST VIEWPORT: Masthead "Outreach" with this month's coverage line; outreach-day card only when one is within a
week (top, live dot while on); "Walk now" with four large time choices; "Areas" as a short list of the freshest
nearby areas. Walking: map top, one street side per card below, Done and No junk mail.

FORM: own synthesis after two re-rolls, combining dealt structures (moment-aware stack, go-now quick start, the
vertical-feed challenger's one-thing-per-screen for walking); seed:a7e0bc018c60383f.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
