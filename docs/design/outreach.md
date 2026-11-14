# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page and the walker's side on the phone. Read DESIGN.md first; its rules win.

## Outreach page (admin)
`/outreach` and `/outreach/{tab}`, for the leaders of the ministry that runs outreach. Every tab opens on the title band "Outreach" with a lead, then a `TabStrip`: Campaigns, Days, Areas, Follow-ups (counted), Reports (counted), Settings (`OutreachHead`). With no ministry running outreach, the page is the title band over an `EmptyState` that says where to turn it on.

## Campaigns
A `Workbench`: the campaigns `DataTable` in the main pane, the chosen campaign in the record pane. *New campaign* sits in the title band's actions for those who may plan.
- **The table** has two columns. *Campaign* is the name over a caption: what's handed out and its dates ("Always there" for general outreach). *Letterboxes this month* is a bare, tabular count. A separate When column pushed the count out of the list with the record pane open at 1024px, so the dates live in the caption.
- **Head:** a `PaneHead` titled by the campaign ("A new campaign" when new), its lead saying it keeps its own record of streets done; Delete in its ⋯ (not for general outreach).
- **Numbers band:** for a saved campaign, three quiet `StatTile`s in one row under the head: letterboxes, km walked and outreach days, tabular figures over caption labels, closed off by a hairline. This is the campaign's own coverage, shown rather than explained.
- **Fields:** Name, What's handed out, Starts and Ends two to a line with a hint under them, then *Show a leaderboard* as a `ToggleRow`.
- **Foot:** the `SaveBar` (and any warning) docks at the pane's foot in `editor-foot`, as the event editor's does. At 760px and under, the foot runs edge to edge of the record pane, past its gutter.
- **Deleting** asks in a `Sheet` that says what goes with it, or why it can't go yet; *Keep it* is the quiet way out.
- **Nothing chosen:** an `EmptyState` in the record pane.

## Follow-ups and Reports
The same `Workbench` shape. Each table has the item over a caption (the place), when it came in, and a `Pill` for its state (the usual pill while it needs doing, quiet once done). Its `Empty` slot says what arrives here and when.
- **Record pane:** a `PaneHead` (Delete in its ⋯), a `FactList` (Contact, Where, Campaign; or Where and Now), the walker's note at body size, a hint, and the actions.
- **A follow-up** has one action: *Mark as followed up*, or a quiet *Mark as not followed up* once done.
- **A new report** has a block *Never deliver there* over two quiet buttons of equal width, *It's been fixed* and *Dismiss it*, with a hint above saying what each changes.

## Settings
The settings in the work area, each group a `Section Grouped` card with short fields two to a line (`ui-pair--wide`) and a hint under each saying its unit. Six groups: Rest period and what's kept, How long a walk takes, Daylight and pairs, Walking between streets, Telling walkers about the length, What counts as a walk.
- **Each group saves by itself:** its own `SaveBar` in its own sand foot, saving or cancelling only that group's fields, with its own warning. A group still being changed keeps what was typed when the page reloads.
- **Read only:** fields are disabled and each foot says "Only outreach leaders can change these."
- **Leaving with changes** asks first (`UnsavedGuard`), on Settings and on a campaign.

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
A page of its own, `/outreach/days/{id}/plan`: the title band ("Outreach Saturday 6 December", the times, the meeting
place, the campaign and the totals; back to Outreach days) with `SaveStatus` ("Saves as you go", "Draft saved") and
*Print the maps*, over a `MapDesk`.
- **The map:** the brand base map; each route in its colour with a ring label (a page-coloured capsule ringed in the
  route's colour): the number when zoomed out ("8 · review" when flagged), "4 · 137 · 74 min" from zoom 15 ("· needs
  review" after it when flagged). Every route keeps its label. Choosing a route fills its label in its colour, on top, and keeps the others to
  their number; their lines fade toward grey. Streets left out or done recently in ink-3; the
  area at 12%; the meeting point (the map's navy place marker, *Claude's adaptation*: orange means "you" on the phone)
  and the chosen route's start. The nine route colours
  (`RouteColours`) each hold 4.5:1 with white text and 3:1 against the dark map and pane.
- **The rail** (top left, a white panel on the Pop shadow): select, draw the area, scissors, brush, lasso, eraser, start
  pin, meeting point, a rule, undo, redo; the tool in use is navy. Tools that need a chosen route are disabled until
  there is one. Each tool's one-line instruction shows as a navy capsule top centre.
- **Hover** (top right): the route's number, name, letterboxes, km, time, bundles, its claims on the day, and why it
  needs review in marker ink.
- **The chosen route** (bottom centre, above the map's credits): its dot, number, letterboxes and time; Lock, Split in
  two, and a ⋯ with Rename, Merge with route N and Leave its streets out (red, last). On a map narrower than 640px the
  bar and the hint sit beside the rail instead of centred.
- **The pane:** `Segmented` Plan / On the day. Plan with no routes: a `PaneHead` "Plan the routes", *Draw the area on the
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
- **At 760px and under** the map sits above the pane and hover facts don't show.

## Areas
The same `Workbench`. The record pane has a map of the area (288px tall, 224px at 1360px and under, at the field radius with a hairline) with *Draw it on the map* / *Draw it again*, Name and
Note, and the `SaveBar`. A new area starts with the drawing tool on.

## Printed route maps
One A4 page a route, black on white: the route number at 26pt and its name, the day's line, the facts in bold, the QR
code top right with "Scan to open it in the app" and the code; the map in a ruled frame (route 3.4pt black, connectors
dashed, streets 1pt grey, main roads 2.4pt grey, arrows, Start and Meet here, street names with a white halo, scale,
north, a key under it); the streets in order in two columns; a hairline over the notes; the attribution at the foot.

## How the day is going (phone)
Masthead "How the day is going" with the day and "on now". Two quiet `StatTile`s, an info `Notice` with the open routes,
then a `Section` *Routes* of `ListRow`s (route title, who and letterboxes, a state `Pill`, a `ProgressTrack` below),
open routes first; a row opens the route.

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
- **Walking** (`/outreach/walking`): the masthead is the walk: a status line (Walking or Paused and the time at the
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
  what's handed out), the day's note, *How the day is going* for leaders, and the routes as rows with *Open* or *Yours*
  `Pill`s and who's taking them. **A route** (`/outreach/r/{code}`, the QR's link): the map with the route and the meeting
  point, the route's title and figures, *Start this route*, *Who's taking it* (first name and *I'll take this route*),
  and its streets.
- **My outreach** and **Offline maps**: masthead pages; totals at headline size over a caption, walks as rows; *Count me by
  name* in a grouped section that changes straight away; each region a row with its size and Keep or Remove.
