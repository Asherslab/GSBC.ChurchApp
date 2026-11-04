---
version: 1
slug: "edia-gsbc-churchapp-features-media-client-75160210"
primary_target: "src/Features/Media/GSBC.ChurchApp.Features.Media.Client"
related_targets: ["src/Features/Media/GSBC.ChurchApp.Features.Media.Admin"]
---

# Media library: folders, two shelves

Scope: the admin panel's Media page (`MediaLibrary`, `MediaPage`) and the picture sheet that chooses
media for tiles, settings and rich text (`MediaChoice`). Mode: operate. Desktop first (1280 to 1920);
a narrow window must not break.

Audience and job: office staff keeping a growing library tidy (sermon art, event posters, tile
pictures, PDFs), and the same staff choosing a picture while editing a draft. Success: find a file in
seconds, file new uploads where they belong, move many at once, and never break what uses a file.
Owner's answers (2026-11-04): one folder per file; staff folders are purely organising; system
folders are seen by their audience plus `media.system.view`; files show as a grid with a list toggle;
layout "Two shelves" chosen on the decision page.

Content ranges: tens to low thousands of files; folders 0 to ~40 per level, 1 to 4 levels deep;
names up to 80 characters; system shelf empty until a feature (group chats) files something.

States: loading, empty library, empty folder, empty search, filter with no match, a system folder the
viewer can't open, a deleted folder (someone else deleted it), a failed move, many chosen.

## Direction contract

THESIS: The library is two shelves, the staff's Library and the app's own files, switched at the top;
inside a shelf you walk folders by breadcrumbs and folder tiles above a picture grid. It refuses the
three-pane file manager with a tree beside everything.

OWN-WORLD: The admin panel's bulletin-at-a-desk world unchanged: white work area, sand (#f4efe7)
segmented shelf switch and folder tiles with hairline borders, navy primary capsules, the orange
marker ring for chosen pictures, aqua-deep links and drop highlight, Montserrat 400/600/700, a lock
glyph and plain caption wherever the app owns something.

STORY: I open Media, see my folders as tiles and the newest pictures under them, open Sermons, pick
three pictures, drag them onto "2026 series" (or press Move to…), and they're filed; searching finds
any file and says which folder it's in.

FIRST VIEWPORT: Title band "Media" with lead, then search, New folder and Upload; a row with the
Library / Made by the app switch on the left and type filters plus Grid/List on the right;
breadcrumbs; one row of folder tiles; the picture grid filling the rest; the detail pane opens beside
the grid for one chosen file; a navy tray docks at the foot when files are chosen, with Clear and
Move to….

FORM: dealt lead (structure 6 of 7, "Two shelves"), locked by the owner on the decision page;
seed:2ab87ac659c29cc0.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
