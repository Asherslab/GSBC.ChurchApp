# Outreach

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's Outreach page. Read DESIGN.md first; its rules win.

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
