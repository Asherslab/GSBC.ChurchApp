---
version: 1
slug: "rmons-gsbc-churchapp-features-sermons-app-28c36235"
primary_target: "src/Features/Sermons/GSBC.ChurchApp.Features.Sermons.App"
related_targets: ["src/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Client","src/Platform/GSBC.ChurchApp.Platform.Ui/AudioControls.razor"]
---

# Sermons on the phone

Scope: the Sermons tab (`/sermons`) and what it opens: a sermon, a series, a speaker, search; the player at the top
of the tab (Platform.Ui `AudioControls`) and the tab's play/pause icon. Mode: Operate (a member or visitor listening and catching up).

- **Audience and job:** the congregation, on their own phones, midweek and on the way to church. Pick up the sermon they
  were part-way through (if they listened in the past week), else the latest; browse by series and speaker; find one.
- **Owner's answers (2026-11-05):** keep going if listened in the past week, otherwise the latest; a "Sermons" tab
  beside Home; **audio first** (video equal where it exists, but Listen leads).
- **Content:** the church's real sermons from Sync (artwork, series, speakers' photos); progress from the merge rule.
- **States:** nothing synced yet (first open, offline), no progress, part-way through, finished, a sermon gone, search
  with no results, a speaker with no photo, a series with one sermon.
- **Constraints:** DESIGN.md (the Sunday Bulletin world, Montserrat, roles not hexes), Dynamic Type, offline, AOT.

## Direction contract

THESIS: The tab opens on your sermon, not the catalogue. Below it, short shelves the church already thinks in: the
latest few, its series as covers, its preachers as faces, one search away from everything. Refuses a feed of identical
cards and a podcast app's library grid.

OWN-WORLD: The Sunday Bulletin as DESIGN.md records it: a navy masthead with the mark's ring at 7%, white page,
hairline rows with 16:9 thumbnails, sand fields, aqua for links and the series line, orange only for how far you got.
Series as 16:9 covers at the 14px radius, speakers as circle photos.

STORY: The member sees what they were listening to, with how long is left, and keeps going in one tap; or the newest
sermon with Listen and Watch. They browse a series or a speaker in a swipe, or search, and every sermon remembers
where they were.

FIRST VIEWPORT: Masthead "Sermons" with a beige note ("Keep going · 22 min left", or the day and service of the
latest); inside it the 136px artwork beside the title and speaker, an orange progress line when underway, and
"Keep listening"/"Listen" (on-masthead button) with "Watch" (ghost) beside it. Under it a sand search field ("Search
sermons, series, speakers") that opens search. Then "Latest" (three rows, "All 155 ›"), "Series" (a sideways shelf of
covers), "Speakers" (faces). Once something has been played, the masthead is the player instead: cover and title (opens the sermon), scrubber with times and a speed chip, back 15 · play/pause · forward 30; the tab icon is play or pause (owner, 2026-11-09).

FORM: Shelves, third on the ranked list (1 the POC's section tabs, 2 Sunday by Sunday, 3 shelves, 4 series wall,
5 search-led, 6 the year of preaching with series bands, 7 three segmented views), dealt as a card and chosen by the
owner over THE ROLL. seed:61faa7a4cdf6ce28. Signature move: the masthead
that knows where you were (Keep going), with the bulletin's orange progress line.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
