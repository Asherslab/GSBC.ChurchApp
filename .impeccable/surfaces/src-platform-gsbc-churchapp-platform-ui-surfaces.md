---
version: 1
slug: "src-platform-gsbc-churchapp-platform-ui-surfaces"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/Surfaces"
related_targets: ["src/Hosts/App/GSBC.ChurchApp.Hosts.App/Pages/Home.razor"]
---

# GSBC app: Home, the Hub the church arranges

Scope: the app's Home (`HomeScreen`, `HubTiles` and every tile view in `Platform.Ui/Surfaces` and in the features that
own tiles), as drawn on the phone and in the admin panel's canvas and preview. Mode: operate (an app's home tab).

Audience: everyone with the app, members and visitors, in the pew on Sunday and midweek; older members included, so
type follows Dynamic Type and every tile reads in a dim auditorium and in daylight. Staff arrange it (the canvas brief).
Owner, 2026-10-27: the team likes the Hub layout best; 2026-11-04: rebuild the earlier Hub (the POC's HubHome) from
data, and keep it looking right at any phone width.

## Direction contract

THESIS: Home is the church's noticeboard arranged as a tile grid: a few things this week, each at the size its
importance earns, drawn in the theme's own colours. It refuses the identical-card feed and the uniform icon grid of a
church-app template: tiles differ in size, tone and content shape.

OWN-WORLD: The bulletin system (DESIGN.md): navy masthead above, then a grid of 14px-radius tiles with a 12px gap on
white; tones are theme role pairs (sand with ink, raised white with lift, masthead navy with its own text, live tint,
marker tint, primary, live, marker), icons in 34px row-icon tiles, the bulletin tile's faint mark bleeding off its
corner, Montserrat 700 titles and 400 captions.

STORY: I open the app on Sunday, see the latest sermon with play, this Sunday's bulletin in navy, this week's two next
things, and my unread notices; one tap opens any of them.

FIRST VIEWPORT: the masthead with the church's name and today's date, then the grid: a wide sermon tile with its cover,
the tall navy bulletin beside two square tiles (prayer and give), the wide "This week" with two entries.

FORM: inherits the POC Hub layout the owner chose on 2026-10-27, now drawn by data; seed:5ac23732b3d42ef3 (shared with
the canvas round).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
