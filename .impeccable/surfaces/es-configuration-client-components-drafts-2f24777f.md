---
version: 1
slug: "es-configuration-client-components-drafts-2f24777f"
primary_target: "src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Drafts"
related_targets: ["src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Admin/Pages","src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Hub","src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Tabs"]
---

# GSBC app: App styling (was Drafts)

Scope: the admin panel's App styling page (`/app-styling`, old `/drafts` links still work), desktop first: the
app as everyone sees it, changing Colours, Home screen and Tab bar, the drafts that carry changes, publishing,
turning a draft on for an event, publishing on a date, undoing a publish. Admin panel only. Mode: operate.

Audience: office staff, every few weeks. Mostly two jobs (owner, 2026-11-09): **preparing a season** (next
year's theme published on 1 January, a conference's Home and tabs on between dates) and **quick fixes** (a
tile's words, published the same day). Owner: "drafts shouldn't be the front and center feature of it, the
intent is to view & modify the styling, drafts is a vehicle". A year-long theme changes yearly; events make
brief changes. Words: "What everyone sees" (never "Live"); no branches, diffs or conflicts.

Decided with the owner, 2026-11-09: landing shows the app as everyone sees it; *Change it* (or clicking the
phone) carries on in your one open draft, starts one named after what you change when you have none, or asks
which when you have several; Church year folds into *Coming up* (clicking one shows that day on the phone and
opens its draft); the name is App styling.

## Direction contract

THESIS: The page is the app, described in one sentence and shown on a phone; drafts are how a change travels,
reached from one chip. It refuses the three-pane drafts workbench (list, editor, detail) and any layout that
changes shape between areas.

OWN-WORLD: The bulletin system (DESIGN.md) at its quietest: white page, title band, one ink sentence with the
facts at 600, the phone frame as the only object with weight, sand chips for preview choices and drafts, a
hairline list of the app's parts, orange only for what's chosen or changed, navy for the one primary action.

STORY: I open App styling and read what everyone sees and what's coming. I click the Bible tile, press Change
it, fix its words, and Publish now. Or I open my 2027 theme draft from Your drafts, choose its colours, and set
it to publish on 1 January.

FIRST VIEWPORT: Title band "App styling", the sentence beneath (theme, tiles, tabs, next dated change), "Your
drafts (n)" chip at the top right. Below, two fixed columns: the phone (true size, scaled to the window) with
preview chips under it (as whom, light or dark, phone, day), and a 400px column: What everyone sees (Colours,
Home screen, Tab bar as hairline rows), Coming up. In a draft the sentence becomes the draft's name and save
line, with Undo, Redo, Share, More, Publish now; the column holds the chosen part's settings. Nothing moves
between areas.

FORM: dealt lead of the re-rolled hand after the owner's steer ("one stage, three areas, but quieter"):
"A sentence, the phone, one panel", position 7 on the round-two list; seed:b1b04867c98ae322.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
