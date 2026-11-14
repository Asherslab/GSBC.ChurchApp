# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page and the walker's side on the phone. Read DESIGN.md first; its rules win.

## Outreach page (admin)
`/outreach` and `/outreach/{tab}`, for the leaders of the ministry that runs outreach. Every tab opens on the title band "Outreach" with a lead, then a `TabStrip`: Campaigns, Coverage, Numbers, Places, Follow-ups (counted), Reports (counted), Settings (`OutreachHead`). With no ministry running outreach, the page is the title band over an `EmptyState` that says where to turn it on.

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

## Coverage
The map (`MapView` over the region's map file) fills the work area to the left; the pane is ruled off on the right,
where every workbench keeps its record (460px, 380px at 1360px and under). It is the shared `MapDesk`. The pane, top to bottom: Campaign (and Region when there are several), the
legend (with a dashed red key when areas are skipped), two quiet `StatTile`s (streets done of the region's, share of
its letterboxes), and *Show on the map* as two `ToggleRow`s, each titled by what it draws with its count ("No junk mail
and hostile (12)", "Places outreach skips (3)"). The pane's foot holds the chosen streets (title, sides, letterboxes,
when done), Clear and *Mark as not done in* the campaign, the undo line, or with nothing chosen a hint saying how to
choose.
- **The ramp** is the signature: covered sides are drawn in deep aqua (bright aqua at night) fading to the map's own
  ground (`--map-land`, the base map's land colour in each scheme) as the rest period ends (`MapLineStyle.Covered` with an
  age), and the legend is the same ramp with "This week",
  half the rest period and "open again". Each side is drawn 3.5 m off the centre line, so a half-done street shows which.
- **Choosing:** a click picks a street, drawn in navy; *Draw round streets* (quiet, floating top left on the map) turns
  on the lasso (`MapSketch`) and becomes the primary *Stop drawing* with a hint beside it ("Drag a loop round the
  streets. Escape stops."); the drawn area shows dashed in navy. The chosen streets' sides, letterboxes and when they
  were done read under its title, then Clear (quiet, left) and *Mark as not done in* the campaign's name (right).
  Marking applies at once and a status line in its place offers *Undo*.
- **Exclusion areas** draw in danger at 12% with a dashed edge (`MapArea Dashed`); Don't letterbox, No junk mail and hostile marks are the
  map's own markers.
- At 760px and under, MapDesk puts the pane under the map.

## Numbers
Two rows of `FilterChips` (when; which campaign), four `StatTile`s (letterboxes, walked, walks, conversations) with the
change on the period before, then two columns (one at 1272px and under): the main one holds *Letterboxes* (a `TrendLine`
with the period before dashed) over *Outreach days* (a `DataTable`: the day over its campaign, letterboxes, km, walkers,
conversations); the narrow one (280 to 360px) holds *Walkers* (a `RankedList` of those counted by name, then one quiet
line for everyone else).

## Places
A `Workbench` with a rail (`FilterChips Rail`: Everything, Areas, Addresses, No junk mail, Hostile, with counts, and a
caption saying where these come from). The table has the place over a caption (kind and why) and when it was marked.
*Add a place* in the title band is a `Menu` (An area, An address). The record pane leads with `PlaceMap`, a 260px map:
an area is drawn with the polygon tool (click round, click the first point to close) and shown in danger; an address
is clicked (the nearest letterbox on that side) and shown with a pin. Delete or Clear sits in the ⋯ and asks once.

## A report on the map
The report's record shows `PlaceMap` with the place, the note, and the photo (`PictureOpen`, opening the
`PictureViewer`). *Skip an area round it…* swaps the decision for drawing on the same map, with *Skip this area* and
Back.

## Map areas (Settings)
A `Section Grouped` card after the settings groups, only for those who manage maps: a `ListGroup` of areas (state as the
subtitle, the regions' counts, size and version under it, ⋯ with Rebuild, Rename, Delete), *Add a map area* (quiet,
opening a `Sheet` with Name, Centre and How far around it), and a foot saying these change straight away.

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
