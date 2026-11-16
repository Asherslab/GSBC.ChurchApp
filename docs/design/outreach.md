# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page and the walker's side on the phone. Read DESIGN.md first; its rules win.

## Outreach page (admin)
`/outreach` and `/outreach/{tab}`, for the leaders of the ministry that runs outreach. Every tab opens on the title band "Outreach" with a lead (none on Map, so the map gets the room), then a `TabStrip`: Campaigns, Map (counting new reports), Days, Numbers, Follow-ups (counted), Settings (`OutreachHead`). The old tab addresses (`/outreach/coverage`, `/places`, `/areas`, `/reports`) open the Map with that job. The page draws the head once, outside the tab it swaps in, so it stays put while a tab loads; each tab puts its own buttons (*New campaign* …) in the band through `SectionContent` (`OutreachHead.ActionsSection`), and its body remembers what it last showed (`Remember`), so a tab opened again draws at once and refreshes in place. With no ministry running outreach, the page is the title band over an `EmptyState` that says where to turn it on.

## Campaigns
A `Board` like Life Groups' (owner, 2026-11-15, #108; decision page `.impeccable/mocks/outreach-campaign-stages/`, the owner
picked B, *cards open in place*). *New campaign* sits in the title band's actions and as the Preparing column's add,
for those who may plan.
- **Always on:** General outreach in a sand strip above the board: the label ruled off on the left, its name (opens it),
  bundles, letterboxes this month, its next outreach day ("No outreach day planned" when there's none) and a small
  quiet *Open* button.
- **Columns:** Preparing, Ongoing, Wrapping up, and *Finished in* a year (narrow; a `Segmented` year switch when there's
  more than one year). Empty columns say what lands there.
- **A card:** the name (the whole card opens it; a stretched link) with its ⋯ (*Move to* each other stage, Delete last
  and red), what's handed out and its bundles, the dates and a countdown in ink at 600 ("starts in 11 weeks", "wraps up
  7 Dec", "ends in 5 days"), a `SpanLine` (start to end, today's dot in deep aqua, the wrap-up stretch in the marker) for
  every dated card (Preparing's has no dot, since today is before it starts), an area line on Preparing cards ("No
  area yet" in marker ink), "Church told 13 Nov" once told, letterboxes and this week's, "About 38 of 60 bundles out",
  and the next outreach day under a hairline, in marker ink when there's none ("No outreach day before it ends").
  Cards drag between columns (or Alt and an arrow) for those who may plan. A finished campaign is a line: name,
  letterboxes, dates.
- **Opening a campaign** widens its column (`BoardColumn Wide`; on this board up to 480px, at least 400px) and the card becomes the
  record, ringed in the marker: `PaneHead` (Delete in its ⋯, Close), the `StagePipeline` (moves at once), the three
  quiet `StatTile`s, then Name; What's handed out and Bundle size; Starts and Ends; Wrap up before it ends (days, its hint
  naming the day it moves and that the church is told) and Bundles made; *Show a leaderboard*; Cancel and Save in the
  card's foot. General outreach opens in its strip instead (no dates, stages or wrap-up). A finished campaign's dates,
  bundle size and wrap-up are disabled with a line saying to move it back to Ongoing. A new campaign opens as a card at
  the top of Preparing. `?campaign=<id>` opens one directly. While another campaign is open, Finished folds into the board's folded strip.
- **At 1100px and under** the columns and the closed cards tighten (less padding, the card's name at label size).
- **At 1024px** the four columns fit; with a record open the board scrolls sideways, as boards do.
- **Deleting** asks in a `Sheet` that says what goes with it, or why it can't go yet; *Keep it* is the quiet way out.

## Follow-ups
The same `Workbench` shape. Each table has the item over a caption (the place), when it came in, and a `Pill` for its state (the usual pill while it needs doing, quiet once done). Its `Empty` slot says what arrives here and when.
- **Record pane:** a `PaneHead` (Delete in its ⋯), a `FactList` (Contact, Where, Campaign; or Where and Now), the walker's note at body size, a hint, and the actions.
- **A follow-up** has one action: *Mark as followed up*, or a quiet *Mark as not followed up* once done.

## Settings
The settings in the work area, each group a `Section Grouped` card with short fields two to a line (`ui-pair--wide`), each number's unit inside its field (`Unit`) and a hint under it saying what it's for. Six groups: Rest period and what's kept, How long a walk takes, Doorknocking (in pairs), Walking between streets, Telling walkers about the length, and What counts as a walk, whose lead says why: a walk left running in a car or on a bus marks nothing done (#104). There are no walking hours: people may walk at any time (owner, 2026-11-15, #103).
- **Each group saves by itself:** its own `SaveBar` in its own sand foot, saving or cancelling only that group's fields, with its own warning. A group still being changed keeps what was typed when the page reloads.
- **Read only:** fields are disabled and each foot says "Only outreach leaders can change these."
- **Leaving with changes** asks first (`UnsavedGuard`), on Settings and on a campaign.

## The Map tab: one workspace for everything on the map
`/outreach/map` (`OutreachMap`, on the shared `MapWorkspace`), chosen on the decision page
`.impeccable/mocks/outreach-workspace/` (*A dock of jobs down the map's left edge*; owner, 2026-11-15, after #107: "a
single large map admin panel … a map, with a good overlay with tools/etc, collapsable panes … all in one place"). It
replaced the Coverage, Places, Areas and Reports tabs, the Map areas pages under Settings and the planner's own page.

- **The dock** (80px, white, ruled off): Coverage, Places, Saved areas, Reports (counting new ones), Map areas, then a
  rule and Plan a day. Each shows only to those who may use it (Places: settings or review; Saved areas and Plan a day:
  plan; Reports: review; Map areas: `outreach.maps.manage`). The open job is in Selected Fill with an orange bar at its
  edge; pressing it again folds its drawer away, and the drawer's own chevron does too.
- **The drawer** (320px, 280px at 1200px and under; planning a day 440px, 380px at 1360px and under, 340px at 1200px
  and under; white, ruled off): a `PaneHead` naming the job, its list or settings, and a white foot ruled off by a
  hairline for its actions (*Draw round streets*, *Draw an area* and *Pick a letterbox*, *Draw a new
  area*, *Add a map area*, a change's `SaveBar`).
- **The map** fills the rest to the window's foot, every built region as one map (below). Floating on it: *Layers* top
  left (a `Popover` of `ToggleRow`s, each layer but Coverage with its count: Coverage, Places outreach skips, No junk mail and
  hostile, Saved areas, Walkers' reports; only the layers the person may see; hidden while drawing), the coverage legend
  and figures bottom left whenever coverage is shown and its drawer isn't, and the attribution bottom right.
- **The card** (`MapCard`, top right, 22rem): whatever was clicked on the map or in a drawer's list (a street or the
  streets drawn round, a skipped place, a mark, a report, a saved area) with its `PaneHead` and ⋯, and its Save or
  decision in the card's white foot, ruled off by a hairline. One thing is chosen at a time across every layer; a click goes to the open job
  first, then reports, places, saved areas and coverage.
- **Drawing** shows the navy `MapDrawBar` top centre: what to do ("Skip area: click each corner"), *Undo last point*,
  *Cancel* and a white *Finish* (enabled at three corners). Instructions never sit in the attribution's grey (#106).
  On a map narrower than 700px the bar spans the map and wraps. Escape, Backspace and Enter still work.
- **Remembered on this computer** (`OutreachMapMemory`, local storage): the job, whether its drawer is out, the layers,
  the view and the campaign (owner, 2026-11-15: "whatever I had last"; the first time, Coverage with coverage, places,
  marks and reports on).
- **At 760px and under** the dock runs across the top, the map comes next and the drawer under it; on a map narrower
  than 640px (and always at 760px and under) the card sits along the map's foot, and while drawing the card is hidden.
- **Planning a day keeps its own map** (*Claude's default*, 2026-11-15): the planner's route labels, brush and lasso
  previews and hover facts belong to its map, so choosing a day swaps in the planner's map inside the same dock and
  drawer; Layers and the coverage legend aren't shown there (streets done recently show grey on the planner's map).
  Folding the planner into the one map is open.

## Days
The same `Workbench` as Campaigns. *New outreach day* in the title band.
- **The table** has two columns. *Outreach day* is the weekday and date with a `Pill` for its state beside it (To plan
  in marker, On now in accent, Ready and Done quiet) over a caption: the times and the campaign. *Routes* is how many
  over a caption of letterboxes, or "Being planned" / "None yet". Days still to come first, soonest first, then past days, newest first.
- **The record pane:** `PaneHead` (Delete in its ⋯), three quiet `StatTile`s (routes, letterboxes, claims), then the way
  in: one primary button, *Plan the routes*, *Carry on planning* or *Open the routes*, over a hint that says where the
  day stands. Then Campaign, Day, Starts and Ends two to a line, Where people meet, What people do (`Segmented`), Note
  for walkers, and the `SaveBar` in the pane's foot.

## The day's routes (the planner)
A mode of the Map tab (owner, 2026-11-15), `/outreach/map?day={id}` (the old `/outreach/days/{id}/plan` forwards there):
the dock stays, *Plan a day* is open, and the drawer (440px, 380px at 1360px and under, 340px at 1200px and under;
it folds away like any job's) heads with the day ("Outreach Saturday 6 December" over the
times, meeting place, campaign and totals) and *Leave this day's routes*. `SaveStatus` ("Saves as you go", "Draft saved")
and *Print the maps* sit in the title band's actions. *Plan a day* without a day lists the days to come, with *New day, under Days* in its foot.
- **The map:** the brand base map; each route in its colour with a ring label (a page-coloured capsule ringed in the
  route's colour): the number when zoomed out ("8 · review" when flagged), "4 · 137 · 74 min" from zoom 15 ("· needs
  review" after it when flagged). Every route keeps its label and no two labels overlap (`MapSketch`, [maps.md](maps.md)). Choosing a route fills its label in its colour, on top, and keeps the others to
  their number; their lines fade toward grey. Streets left out or done recently in ink-3; the
  area at 12%; the meeting point (the map's navy place marker, *Claude's adaptation*: orange means "you" on the phone)
  and the chosen route's start. The nine route colours
  (`RouteColours`) each hold 4.5:1 with white text and 3:1 against the dark map and pane.
- **The rail** (top left, a white panel on the Pop shadow): select, draw the area, scissors, brush, lasso, eraser, start
  pin, meeting point, a rule, undo, redo; the tool in use is navy. Tools that need a chosen route are disabled until
  there is one. Each tool's one-line instruction shows as a navy capsule top centre; drawing the area shows the `MapDrawBar` instead, beside the rail, with *Undo last point*, *Cancel* and *Finish*.
- **Hover** (top right): the route's number, name, letterboxes, km, time, bundles, its claims on the day, and why it
  needs review in marker ink.
- **The chosen route** (bottom centre, above the map's credits): its dot, number, letterboxes and time; Lock, Split in
  two, and a ⋯ with Rename, Merge with route N and Leave its streets out (red, last). On a map narrower than 640px the
  bar and the hint sit beside the rail instead of centred.
- **A route's time** is the same number everywhere (the planner's labels, table and hover, the printed sheet, the phone's day
  list and route page): walking the route and delivering to its letterboxes, by the settings' *How long a walk takes*.
  It leaves out the walk from the meeting place and back, as its distance does (2026-11-15).
- **The drawer:** `Segmented` Plan / On the day. Plan with no routes: a `PaneHead` "Plan the routes", *Draw the area on the
  map*, *Or start from a saved area*, and how big each route is (`RouteSize`: by letterboxes, minutes or bundles, in a
  sand box). With routes: one caption line (where they came from, the size, and *5 need a look* as a link that picks the
  next flagged route), *Include streets done recently* when there are any, the routes `DataTable` (dot, number and name
  over a *Needs review* pill and the reason, letterboxes, time; the time column drops at 1360px and under), then
  *Change how big routes are* and *Save this area for next time* as links.
- **The foot:** a status line at the right ("Not published yet: walkers can't see these routes."), then Rebalance and
  Regenerate as small quiet buttons (left) and *Publish 9 routes* or *Publish the changes* (right). Publishing asks in a `Sheet`, listing
  routes that still need a look.
- **On the day:** three quiet `StatTile`s (walking now, % of letterboxes done, routes with no one), an info `Notice`
  naming the open routes, and the routes table with who has each and a state `Pill` (Open in marker, Walking in accent, Claimed and
  Done quiet) with a ringed `ProgressTrack` in live aqua once walking (and when done); open routes first. The draft's save status hides.
  What's walked is drawn on the map.
- **At 760px and under** the map sits above the drawer and hover facts don't show.

## Saved areas (Map tab)
The drawer lists them as `ListRow`s (name over its note); *Draw a new area* in its foot starts drawing. Saved areas
draw dashed in deep aqua under the Saved areas layer; a click inside one, or its row, opens its card: Name and Note,
*Draw it again* and *Delete this area…* in the ⋯, the `SaveBar` in the card's foot. The chosen area draws solid.

## Printed route maps
One A4 page a route, black on white: the route number at 26pt and its name, the day's line, the facts in bold, the QR
code top right with "Scan to open it in the app" and the code; the map in a ruled frame (route 3.4pt black, connectors
dashed and drawn once where a walk-through goes out and back (two passes with offset dashes read as a solid line),
streets 1pt grey, main roads 2.4pt grey, arrows, Start and Meet here, street names with a white halo (the
route's own in bold beside the line, or level beside a short street; every other named street in view along its
straightest stretch, never over the route or another name), scale,
north, a key under it); the streets in order in two columns; a hairline over the notes; the attribution at the foot.

## How the day is going (phone)
Masthead "How the day is going" with the day and "on now". Two quiet `StatTile`s, an info `Notice` with the open routes,
then a `Section` *Routes* of `ListRow`s (route title, who and letterboxes, a state `Pill`, a `ProgressTrack` below),
open routes first; a row opens the route.

## The admin maps: one map of everywhere we have
Every outreach map in the admin panel (Coverage, a place or a report, a saved area, the planner, map areas) draws every built
region as one seamless map (`OutreachAtlas.Source`, one map file per region; owner, 2026-11-15: "i should be able to
scroll around the whole map we have"). Where our maps end is plain (`MapReach`): everything outside the built regions'
squares sits under the map veil over a low-zoom map of all of Australia (deeper and hatched outside Queensland, where areas
can't be added), the edge is a thin ink line, and squares still to build or switched off show in the Map areas page's
dashed looks with "Waiting to build", "Building…", "Switched off" or "No homes" (to people who look after maps). A key,
"Where walkers have maps", sits bottom left while the view reaches past our maps, and when the view is mostly beyond them
a note top centre says "Past the edge of our maps" (`OutreachMapKey`, `OutreachMapOutside`; owner's pick, 2026-11-16).
Detail: [maps.md](maps.md), *Past the edge of our maps*. Before the overview is built, the map is held to our regions,
hatched beyond, with "No map here yet". The phone keeps one region at a time (offline downloads).

## Coverage
A job on the Map tab. Each region's streets load as it comes into view. The drawer, top to bottom: Campaign, the
legend, two quiet `StatTile`s (streets done of those on the map in view, share of the letterboxes in view; "Counting…"
until every region in view has its streets, never a part count); its foot has a hint and *Draw round streets*. With the
drawer away, a floating legend bottom left names the campaign and the streets done in view over the ramp.
What's drawn is under *Layers*.
- **The ramp** is the signature: covered sides are drawn in deep aqua (bright aqua at night) fading to the map's own
  ground (`--map-land`, the base map's land colour in each scheme) as the rest period ends (`MapLineStyle.Covered` with an
  age), and the legend is the same ramp with "This week",
  half the rest period and "open again". Each side is drawn 3.5 m off the centre line, so a half-done street shows which.
- **Choosing:** a click picks a street, outlined in ink with its coverage still showing inside (`MapLineStyle.Chosen`); *Draw round streets* turns on the lasso with the `MapDrawBar`
  ("Drag a loop round the streets", Cancel); the drawn area shows dashed in ink. The card: the streets' title, their
  sides, letterboxes and when they were done, then Clear (quiet, left) and *Mark as not done* (right) in its foot.
  Marking applies at once and the card then says they're open again with *Undo*.
- **Exclusion areas** draw in danger at 12% with a dashed edge (`MapArea Dashed`); Don't letterbox, No junk mail and hostile marks are the
  map's own markers.

## Numbers
Two rows of `FilterChips` (when; which campaign), four `StatTile`s (letterboxes, walked, walks, conversations) with the
change on the period before, then two columns (one at 1272px and under): the main one holds *Letterboxes* (a `TrendLine`
with the period before dashed) over *Outreach days* (a `DataTable`: the day over its campaign, letterboxes, km, walkers,
conversations); the narrow one (280 to 360px) holds *Walkers* (a `RankedList` of those counted by name, then one quiet
line for everyone else).

## Places
A job on the Map tab. The drawer: *Type an address to skip* (`SearchField`; three letters or more search the
addresses of the built regions, "3/12 Smith St" and "12 smith street albany" both work; matches as `ListRow`s, and a
match opens a new address card with its letterbox pinned and the map on it; #105), `FilterChips` (Everything, Areas,
Addresses, No junk mail, Hostile, with counts), then the places as `ListRow`s (title over kind and why). Its foot:
*Draw an area* and *Pick a letterbox*. On the map, areas draw in danger at 12% with a dashed edge, addresses and marks
as the map's markers; a click on one, or inside an area, opens its card: an area's Name, why and Note (*Draw it again*
and *Delete this area…* in its ⋯), an address's why, Who asked, Note and *Show walkers a Don't letterbox marker*, or a
mark's facts with *Clear* in its ⋯; the `SaveBar` in the card's foot. A new address takes a click on its letterbox too.

## Reports (Map tab)
New reports are orange markers with "!" (the Marker Rule: new) under the Walkers' reports layer; the chosen one is
ringed. The drawer: `Segmented` *To look at* / *Dealt with*, the reports as `ListRow`s (reason over place and when,
a *To review* `Pill`). The card: reason, place and when, *Delete this report…* in its ⋯, the photo (`PictureOpen`), the
walker's note and a hint; in its foot *Never deliver there* (block), *Skip an area round it…* (draws with the
`MapDrawBar`, then *Skip this area* and Back), and *It's been fixed* and *Dismiss it* side by side.
Notifications open `/outreach/reports`, the Map with Reports out; `?report={id}` opens one.

## Map areas (Map tab)
Moved from Settings into the Map tab on 2026-11-15 (owner: "can also be used to expand and change "Map Areas""); built
first on the decision page's *A map desk per area* (`.impeccable/mocks/outreach-areas/`, C). An area is a set of
squares (regions of about 10 km), so it is worked on the map, never typed in as a latitude. Only for those who manage
maps; its squares show on the map only while its drawer is out.

- **The list** (the drawer): each area a `ListRow` with its state as the subtitle, a `ProgressTrack` while it builds and
  its regions' counts and size; ⋯ with Rebuild, Rename and Delete (`MapAreaSheets`; Delete asks once). *Add a map area*
  in the foot. A click on a square on the map opens its area.
- **An area** (`?job=map-areas&area={id}`): *All map areas* back, its `PaneHead` with the regions as the lead and the
  same ⋯, the state `Pill` with its detail, a `ProgressTrack` while building (Try again if it failed), a key drawn with
  the squares' own looks, a warning `Notice` *Still in use* naming each campaign or day that uses a region being
  switched off, then the regions grouped *Changes*, *On (n)* and *Off (n)*, each a `ToggleRow` with its facts (hovering
  one outlines its square). On the map, a click on a square switches it off or on and a + square on the edge adds one;
  square names are quiet notes in their corners. The foot says what the change frees and builds over the `SaveBar`
  (*Switch off 2 regions*, *Add 1 region*); switching off asks once in a `Sheet` saying what phones drop, its size, what
  uses it, and that coverage and history stay. `UnsavedGuard` holds the change.
- **Square looks** (`MapAreaDrawing`): built in live, solid; waiting to build in live, dashed; off in ink 3, dashed with
  the 12% wash; no homes an ink 3 outline; can be added an ink 3 dashed outline with a "+"; adding in orange ink;
  switching off in danger, dashed.
- **Add a map area** (`?job=map-areas&area=new`): a `SearchField` (*Find a town or suburb*) with the matches as
  `ListRow`s (kind, distance and direction from the church), or a click on the map to set the middle (a pin); *How far
  around it* (1 to 30 km) and *Name*; a status line with the regions that have homes, letterboxes and the download; the
  squares with each one's estimated size. While Map areas is out the map pans freely, so a town far from our maps can
  be reached. The foot is the `SaveBar` `New` *Add and build it*.
## On the phone

The walker's side of Outreach (`Features.Outreach.App`), built on the decision page's *Words first*
(`.impeccable/mocks/outreach-walking/`, 2026-11-15): a walk is read at a glance, so the street you're on leads in the
masthead's navy at display size and the map supports it. Maps are `MapView` ([maps.md](maps.md)).

- **Outreach** (`/outreach`): the masthead, then your walk if one is on (a navy `Card`), the outreach day if one is on
  (a section with *Pick a route*), then the ask: a sand box with *Got an hour? Walk from your door*, a line on how it
  works, a `Segmented` length (30 min, 1 hr, 2 hr, Distance, then 1 to 8 km), what you carry as a row opening a `Sheet` of
  `ToggleRow`s, and the block *Plan my walk*. *Around you* is a 13.5rem rounded map, not interactive, with streets walked
  lately in grey. Then the next outreach days and the rows My outreach and Offline maps.
- **Your walk** (`/outreach/walk`, solid top bar): the map full width (38% of the screen, at least 14rem), framed on
  the walk, connectors, a long walk dotted and labelled, recent coverage and the start; then the figures (distance and time at headline size,
  letterboxes, bundles and *back by* in a caption), the reach note in a sand box with its two ways to get more as link
  buttons, warnings as `Notice`s, and *Start the walk*. *Change the walk* holds the length, letterboxes or doorknocking,
  *I'm walking with someone*, where you start (tap the map, or find a street in a `Sheet` with a `SearchField`). Empty
  states say why there's no plan (no location, no maps yet, outside the area, getting the map, no street near).
- **Walking** (`/outreach/walking`, a full-screen task: `Page Focused`, so the tab bar steps aside while a walk is on and
  ‹ Outreach is the way back, the walk carrying on; the map takes the room the tab bar left and the walk's bar sits just
  above the home indicator): the masthead is the walk: a status line (Walking or Paused and the time at the
  left, the campaigns at the right), the street at display size, *Houses on your left · 4 to go* at title size, and the
  next turn with its arrow over a hairline (*Then left into …* and its letterboxes). Then the map, centred on you at
  street level while it follows you, framing you, the street you're on and the next one (the whole route and you before the first fix); a drag stops that and puts a quiet small *Follow me* at the map's bottom right. Then
  progress (letterboxes and distance over the orange `ProgressTrack`), what you just marked as a status line with Undo
  (for 12 seconds), a warning `Notice` if location is off, two 3.5rem quiet buttons side by side (No junk mail or At a
  door; Spoke to someone), and a bar: Pause (Carry on when paused) at the left, the walk's ⋯ `Menu` in the middle (Not a
  home or wrong on the map, Skipped this bit, Follow me on the map or Stop following me), Finish at the right.
- **The end** (on the walking page once finished): a masthead (*Well walked*, or *Thanks for knocking*), the figures, and
  *Check what you covered*: the planned streets as a line of stops (a 3px rule with a dot per stop, deep aqua once
  walked), each a `ToggleRow` saying walked, partly walked or not; the `SaveBar` with *Keep walking* and *Save the walk*.
- **Sheets:** *Tell staff about this place* (a stacked `Segmented` reason, a required note, an optional photo in a
  `FilePick` box, *Send for review*); *At this door* (four block buttons, then the follow-up with its consent
  `ToggleRow` and fields, or the hostile explanation and *Don't come back here*). Both ask before losing what was typed.
- **An outreach day** (`/outreach/days/{id}`): masthead with the campaign and when, a `FactList` (meet at, what it is,
  what's handed out), the day's note, *How the day is going* for leaders, and the routes as `ListRow`s: the route's
  number in a 34px disc of its colour, its name, distance · time · letterboxes in tabular figures, then who's on it
  ("Sam and Jo are on it", or *Open: no one on it yet* in deep aqua) and a *Yours* `Pill` on your own. **A route** (`/outreach/r/{code}`, the QR's link): the map with the route and the meeting
  point, the route's title and figures, *Start this route*, *Who's taking it* (first name and *I'll take this route*),
  and its streets.
- **My outreach** and **Offline maps**: masthead pages; totals at headline size over a caption, walks as rows; *Count me by
  name* in a grouped section that changes straight away; each region a row with its size and Keep or Remove.
