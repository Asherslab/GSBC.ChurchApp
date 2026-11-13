---
version: 1
slug: "chapp-features-news-client-newstile-razor-00a4906b"
primary_target: "src/Features/News/GSBC.ChurchApp.Features.News.Client/NewsTile.razor"
related_targets: ["src/Features/News/GSBC.ChurchApp.Features.News.Contracts/NewsTiles.cs","src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css/ui.css"]
---

# News tile: church news on Home

Scope: a News feature tile (`hub.news`) for Home, owned by News, offered in the Home editor's Add a tile. Mode: Operate
(a glance on Home that opens a post). Decision page 2026-11-13 (`.impeccable/mocks/news-tile/`): the owner loved all three
tile directions ("give me all three as variants/layouts of the tile"), so A, B and C ship as the tile's layouts; D (no
tile) was declined.

- **Who:** everyone on Home, signed out too; it shows the church news they may see (each post's audience), from the store,
  offline. Staff place and size it in a draft like any tile.
- **Content:** 0 to hundreds of posts; titles up to 120 characters; most with one picture; urgent posts on some days.
- **Untouched:** the shared `HubTile` frame, tile settings, tones and the grid; Sync (reads from the store only).

## Direction contract

THESIS: One News tile, three layouts staff choose by size: a front page in miniature (picture first), quiet until it
matters (what's new since you last looked), headlines (the newest three). Every layout gives the tile to a post marked
urgent today, as the News page does. Refuses a carousel and a second notifications inbox.

OWN-WORLD: The Hub's own tile language: `HubTile` shapes (picture, beside, behind, row, square, poster, list), theme tones,
the marker for urgency (a band in Front page, the Highlight tone in the others), the agenda's list rows for headlines,
the unread dot for new.

STORY: Glance at Home, see the newest story with its picture (or "2 new", or three headlines), tap to read that post;
on a day something's urgent, it's the first thing on the tile.

FIRST VIEWPORT: Under the latest sermon, a 2 × 2 Card: the newest post's picture filling the top, its headline and
"Church office · 2 hours ago", then the post before it on one line under a hairline. 2 × 1 puts the picture beside, 1 × 2
above, 1 × 1 behind. Urgent today: a marker band "Today: …" across the top (2 × 2) or the post itself in Highlight.

FORM: surface roll, the dealt lead (Front page) plus dealt 2 and 3 as alternates built as layouts; seed: seed:2851a64fd7eba071

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
