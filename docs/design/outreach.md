# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page and the walker's side on the phone. Read DESIGN.md first; its rules win.

## Outreach page (admin)
`/outreach` and `/outreach/{tab}`, for the leaders of the ministry that runs outreach. Every tab opens on the title band "Outreach" with a lead, then a `TabStrip`: Campaigns, Follow-ups (counted), Reports (counted), Settings (`OutreachHead`). With no ministry running outreach, the page is the title band over an `EmptyState` that says where to turn it on.

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
  zoom 17 while it follows you (the walk and you together while you're off it); a drag stops that and puts a quiet small *Follow me* at the map's bottom right. Then
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
