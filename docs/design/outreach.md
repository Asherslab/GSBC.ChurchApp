# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page. Read DESIGN.md first; its rules win.

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
The map (`MapView` over the region's map file) fills the work area to the left; a 360px rail is ruled off on the right,
where every workbench keeps its record. It is the shared `MapDesk`. The pane, top to bottom: Campaign (and Region when there are several), the
legend (with a dashed red key when areas are skipped), two quiet `StatTile`s (streets done of the region's, share of
its letterboxes), and *Show on the map* as two `ToggleRow`s with what each draws. The pane's foot holds the chosen
streets (title, sides, letterboxes, when done), Clear and *Mark as not done*, or the undo line.
- **The ramp** is the signature: covered sides are drawn in deep aqua (bright aqua at night) fading to the map's own
  ground (`--map-land`, the base map's land colour in each scheme) as the rest period ends (`MapLineStyle.Covered` with an
  age), and the legend is the same ramp with "This week",
  half the rest period and "open again". Each side is drawn 3.5 m off the centre line, so a half-done street shows which.
- **Choosing:** a click picks a street, drawn in navy; *Draw round streets* (floating top left on the map) turns on the
  lasso (`MapSketch`), and the drawn area shows dashed in navy. The chosen streets' sides, letterboxes and when they
  were done read under its title, then Clear (quiet, left) and *Mark as not done* (right). Marking applies at once and a
  status line under it offers *Undo*.
- **Exclusion areas** draw in danger at 12% with a dashed edge (`MapArea Dashed`); Don't letterbox, No junk mail and hostile marks are the
  map's own markers.
- At 760px and under, MapDesk puts the pane under the map.

## Numbers
Two rows of `FilterChips` (when; which campaign), four `StatTile`s with the change on the period before, then two
columns (one at 1272px and under): *Letterboxes* (a `TrendLine` with the period before dashed) and *Outreach days*
(a `DataTable`: the day over its campaign, letterboxes, km, walkers, conversations), and *Walkers* (a `RankedList` of
those counted by name, then one quiet line for everyone else).

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
