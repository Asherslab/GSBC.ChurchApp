# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page. Read DESIGN.md first; its rules win.

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
- **The table:** *Outreach day* (the weekday and date over a caption: the times and the campaign), *Routes* (how many over
  a caption of letterboxes, or "Being planned" / "None yet") and a `Pill` for its state: To plan (marker), Ready, On now
  (accent), Done (quiet). Days still to come first, soonest first, then past days, newest first.
- **The record pane:** `PaneHead` (Delete in its ⋯), three quiet `StatTile`s (routes, letterboxes, claims), then the way
  in: one primary button, *Plan the routes*, *Carry on planning* or *Open the routes*, over a hint that says where the
  day stands. Then Campaign, Day, Starts and Ends two to a line, Where people meet, What people do (`Segmented`), Note
  for walkers, and the `SaveBar` in the pane's foot.

## The day's routes (the planner)
A page of its own, `/outreach/days/{id}/plan`: the title band ("Outreach Saturday 6 December", the times, the meeting
place, the campaign and the totals; back to Outreach days) with `SaveStatus` ("Saves as you go", "Draft saved") and
*Print the maps*, over a `MapDesk`.
- **The map:** the brand base map; each route in its colour with a capsule label "4 · 137 · 74 min" (`MapSketch`
  labels); the chosen route full colour and the rest faded; streets left out or done recently in ink-3; the area drawn
  at 12%; the meeting point and the chosen route's start.
- **The rail** (top left, a white panel on the Pop shadow): select, draw the area, scissors, brush, lasso, eraser, start
  pin, meeting point, a rule, undo, redo; the tool in use is navy. Tools that need a chosen route are disabled until
  there is one. Each tool's one-line instruction shows as a navy capsule top centre.
- **Hover** (top right): the route's number, name, letterboxes, km, time, bundles, its claims on the day, and why it
  needs review in marker ink.
- **The chosen route** (bottom centre): its dot, number, letterboxes and time; Lock, Split in two, and a ⋯ with Rename,
  Merge with route N and Leave its streets out (red, last).
- **The pane:** `Segmented` Plan / On the day. Plan with no routes: a `PaneHead` "Plan the routes", *Draw the area on the
  map*, *Or start from a saved area*, and how big each route is (`RouteSize`: by letterboxes, minutes or bundles, in a
  sand box). With routes: one caption line (where they came from, the size, how many need a look), *Include streets
  done recently* when there are any, the routes `DataTable` (dot, number and name over a review line in marker ink,
  letterboxes, time), then *Change how big routes are* and *Save this area for next time* as links.
- **The foot:** a status line at the right ("Not published yet: walkers can't see these routes."), then Rebalance and
  Regenerate (quiet, left) and *Publish 9 routes* or *Publish the changes* (right). Publishing asks in a `Sheet`, listing
  routes that still need a look.
- **On the day:** three quiet `StatTile`s (walking now, % of letterboxes done, routes with no one), an info `Notice`
  naming the open routes, and the routes table with who has each and a `ProgressTrack` in live aqua; open routes first.
  What's walked is drawn on the map.
- **At 760px and under** the map sits above the pane; the hint moves to the map's foot and the chosen route's bar to its
  top right; hover facts don't show.

## Areas
The same `Workbench`. The record pane has a 288px map of the area with *Draw it on the map* / *Draw it again*, Name and
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
