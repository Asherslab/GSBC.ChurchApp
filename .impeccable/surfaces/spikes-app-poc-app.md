---
version: 1
slug: "spikes-app-poc-app"
primary_target: "spikes/app-poc/App"
related_targets: []
---

# GSBC app: Sunday, Week, Listen

Scope: the store app's shell and its five tabs (Sunday, Week, Listen, Prayer, More) plus their detail pages.
Notifications and prayer requests are the owner's go-to items (2026-10-26): Prayer has its own tab, and a
notifications bell with an unread badge sits in every masthead; first launch asks for topics.
Mode: operate. Audience, purpose and constraints live in PRODUCT.md.

Chosen 2026-10-26 by the owner from three dealt structures, combined at their request: the bulletin
as home, the week as the calendar, watch-and-listen as the sermon tab, in the bulletin's look. Light is
the default; an app-wide dark mode takes the dark listen sketch's palette.

Unresolved: whether the 2026 yearly theme (website greens) replaces the brand palette; prayer wall in v1.

## Direction contract

THESIS: The app reads like the church's own bulletin: every tab opens on a navy masthead and continues as a white page with section tabs. It refuses the generic church-app tile grid and the feed of identical cards.

OWN-WORLD: Brand navy masthead with the mark's ring echo, white page, beige sand callouts, orange underline on the active section tab, aqua for actions and live state, Montserrat 700 and 400/600. Dark mode: night navy ground, raised navy surfaces, bright aqua, orange progress.

STORY: Open the app and Sunday is there: theme, welcome, both sermons with outlines and passages, this week, giving, your notes. Week answers "what's on and when"; Listen picks up where you stopped.

FIRST VIEWPORT: Navy masthead (yearly theme at 40px for a 390pt width, verse and date, next-service or live status, share and notifications), sticky section tabs that all fit (Welcome, Sermons, This week, Next, Give; "Next" is the bulletin's What's Next), welcome letter below; "My notes" in the top bar (a sheet) and as the bulletin's last section, never floating over the text; five-tab bar.

FORM: dealt structures 4, 6 and 7 of seven, combined by the owner; seed:848617a1b7d9020d. Signature move: the masthead plus sticky section tabs, shared by every tab root.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
