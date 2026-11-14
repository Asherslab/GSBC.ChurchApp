# Home and the tab bar

Detail behind [DESIGN.md](../../DESIGN.md) for the Hub: its grid, tiles and their layouts, the Home masthead, the Hub canvas and the Tab bar canvas. Read DESIGN.md first; its rules win.

## Home
Home is the Hub: a compact masthead, then one grid of tiles on the gutter. The grid has 2 to 4 equal columns (staff's choice, 2 by default); tiles pack in order, each taking the first place it fits. A row stays near square: one column's width, held between 88px and 132px. A tile spans 1 to 4 columns and 1 to 4 rows; one as wide as the grid and one row tall sizes to its content (an agenda line).

## Home tiles and their layouts (signature)
See docs/features/home.md. A tile is never refused for its size: each kind declares **layouts** (`TileVariant`), and
the tile draws the one chosen, or **Best for this size**, the default for its size and picture. A layout that
doesn't suit the size, or needs a picture the tile hasn't got (`TileVariant.NeedsPicture`), falls back to the
default. Sizes are read against the grid (a 3-wide tile on 2 columns is 2 × 1). Add new layouts to this table as they
come up (owner, 2026-11-04).

The frame draws six shapes (`TileShape`): **square** (icon top, words at the foot), **row** (words, icon at the end),
**poster** (tall: icon top, a larger title, a go line; the mark bleeds off dark tones), **picture** (picture or cover
above the words), **behind** (picture fills the tile, words over a masthead-deep shade in `on-masthead`) and
**beside** (a square picture at the start, needs two columns).

**Picture layouts on every kind** (owner, 2026-11-04, note 22): every kind with its own catalogue also offers Picture
behind, above and beside (`TileVariants.Pictures`), suiting the same sizes as a Link tile's and the default when there
is a picture (Latest sermon and News have their own, below). In them a kind's content shrinks to a title, one line and, from two cells up, its go line (*Picture layouts draw* below). **Best for this size** keeps a kind's designed layout until a picture is chosen.
A kind none of whose layouts show a picture hides the Picture field (`HubTileKind.UsesPicture`); today every kind uses one.

| Kind | Layout (suits) | Draws | Default |
|---|---|---|---|
| Link tile (Give, Bible, Prayer, Life Groups, Ministries, Visit us, campaigns) | Words and icon (any) | square / row / poster by size | without a picture |
| | Picture behind (any) · above (2 rows+) · beside (2 columns+) | behind · picture · beside | with a picture: behind at 1 × 1, above at two rows or more, beside at one row wider |
| Latest sermon | Cover first (any) | 1 × 1: cover and play; one row: title beside the cover; taller: cover above the title, 2 × 2 adds the length; the tile's picture replaces the cover | always |
| | Where you're up to (any) | play, title and how long (progress once Sermons plays), no cover | never |
| | Picture behind (more than one cell) | title, date and play over the shade | never |
| Sunday bulletin | Poster (any) | 1 × 1: "Sunday 8 Nov" and the open words; one row: theme with date · verse; taller: the bulletin cover | always |
| | Theme first (any) | the year's theme as the title at every size | never |
| | Verse (more than one cell) | the week's verse large, the reference small | never |
| | *Picture layouts draw* | the theme, the date and verse, "Read the bulletin" | |
| This week (one-off events, specials and dates not on) | Agenda (any) | 1 × 1: the next thing over when ("Tomorrow 7pm", "Not on Saturday"); one row: up to three; taller: as many as fit, up to *How many to show*; nothing coming: "Nothing special coming up" | always |
| | Just a link (any) | icon, title and "3 things coming up" | never |
| | *Picture layouts draw* | the title, the next thing and when, "Calendar" | |
| Notifications | Count and newest (any) | bell, "3 unread", the newest title | 1 × 1 |
| | Newest in full (any) | the newest notice and its first lines, the count above | one row, wider |
| | Unread dots (any) | the newest titles each with an unread dot | two rows or more |
| | *Picture layouts draw* | the count with an unread dot and the newest title (the staff's words when nothing is unread) | |
| | (nothing unread) | the staff's words, any layout | |
| Outreach day | Invitation (any) | 1 × 1: icon and title; one row: a row with the line; taller: a poster with "Find a route" | always |
| | Just a link (any) | icon and title | never |
| | *Picture layouts draw* | the invitation and its line, "Find a route" when taller | |
| News (posts bring their own pictures; no Picture field) | Front page (any) | the newest headline and when ("From Kids ministry · 2 hours ago" for a ministry's post at 2 × 2) with its picture: behind at 1 × 1, beside at one row, above when taller; 2 × 2 adds the post before it on one line, opening that post; no picture keeps icon and words | always |
| | What's new (any) | posts new since this phone last opened News, with an unread dot: 1 × 1 "2 new" over the title; 2 × 1 "2 new in News" over the newest headline; 1 × 2 adds when; 2 × 2 the headline, "2 hours ago · 1 more new" and "Read it"; none new: "Nothing new since you last looked" | never |
| | Headlines (any) | 1 × 1: the newest headline, when under it; bigger: the title and "All news" over agenda rows, three at one row or 1 × 2, five at 2 × 2 (one more per extra row, two at two columns), each up to two lines (one at 240px wide and over) | never |
| | (urgent today) | takes the tile in every layout: a Front page of 2 × 2 or bigger keeps its lead and gains a marker band "Today: …" (`HubTile`'s `Banner`); anything smaller, and What's new, becomes the post itself in Highlight with the News icon, "Today · " and its first words; Headlines lists it first with "Today" and a Today pill | |
| | (no news) | the tile's title and "Nothing new yet" | |

In the editor a layout that doesn't suit the tile stays listed but greyed, its caption saying why: "Needs a wider
tile", "Needs a taller tile", "Needs a bigger tile", or "Needs a picture: choose one under Picture". The automatic
choice reads "Best for this size (now Picture beside)".

Recorded on the decision pages but not built: This week as a day strip, the sermon series card, Outreach's map card
(needs real routes), and News as no tile at all (declined). The pages are in `.impeccable/mocks/decision/hub-variants/<kind>/`.

## Home masthead
The compact masthead: the title "Good Shepherd" on one line (the display size or 10.5% of the screen width, whichever is smaller), today's date under it as the beige note beside the sync indicator, and no section tabs. The top bar is clear over the masthead, so the status bar sits on navy, and turns solid with the title after 44px of scroll, on the admin canvas's phone too.

## Hub tiles
- **Tones:** eight theme role pairs (staff name, token): **Soft** (`sand`, the default), **Card** (`raised`, lifted all round with `--tile-lift` so it reads on the white page, never a drawn border), **Top of Home** (`masthead`), **Buttons** (`primary`), **Pale accent** (`live-soft`), **Accent** (`live`), **Pale highlight** (`marker-soft`) and **Highlight** (`marker`). Each tone's text is its pair's (Top of Home: on-masthead, with the beige note for lines); on the solid tones the secondary line, icons and agenda rules take the text colour over the ground; on the pale tones the secondary line is the tile's ink mixed over the ground, never ink-2. In dark, Pale highlight mixes in more marker with an ink go line (`--tile-soft-mix`, `--tile-soft-ink`, `--tile-warm-go`); Pale accent keeps its light mix.
- **Frame:** the title balanced, breaking only between words, never hyphenated; up to two secondary lines (three on a poster); a go line with a chevron. A tile can carry a live dot or an orange unread dot by its title.
- **Corner mark on picture tiles** (owner, 2026-11-04, note 21): a picture behind or above shows › (in the app) or ↗ (a web page) in a round chip at the top right; beside, the plain mark. Not on a tile with its own go line or play button.
- **Fitting the words:** each tile is a size container. At 112px wide or less the lines and go icon drop and the title shrinks. At 200px or less the play disc moves to the top right, an agenda's time stacks above its event, and unread-dot titles may take two lines. A poster 240px wide or more carries a larger icon.
- **Icon:** the row tile; its colour, for staff: Goes with the tile, Buttons, Accent, Highlight, Grey, or Its own colours (only for an uploaded drawing).
- **Mark:** a solid-toned poster two or more rows tall carries the church mark at 7% off its bottom-right corner. With no picture, a sermon cover is the masthead with the mark.
- **Press and focus:** the whole tile is one target; a tile's own action (the primary play disc) sits at its bottom right above it.
- **Agenda inside a tile:** hairline-ruled entries with a tabular time column; a date that isn't on reads "Not on" there, its title at 400. On the phone each entry is its own button opening that date's page; the rest of the tile opens the calendar.
- **Ministries and Life Groups tiles:** Ministries, when you're in any: "Your ministries" with up to two lines (a newest post title, or the name and your place). Life Groups, in a group: "Your Life Group" and "Name · Room, 9am"; with new groups coming, the staff's title over "New groups from 1 February". Otherwise the staff's title and words.
- **Banner:** `HubTile`'s `Banner` slot is a marker strip across the top, above the picture, opening its own target (a `HubListEntry` with `hub-tile__banner`): icon, one line of words, chevron. The corner mark moves under it. Only News's urgent band uses it.
- **Date after the title:** `hub-list--after` is for a single "what came before" row under a tile's lead: the title ellipsized, the date after it, quiet. Lists keep a when-first column with ages: "Just now", "46 min ago", "3 hours ago", "Yesterday", the weekday within the week, then "9 Nov".

## Hub canvas (signature)
Home arranged on App styling's phone inside a draft: the phone is the canvas; the panel holds the Home screen controls or the chosen tile's settings.
- **Home screen panel:** a caption on moving and resizing by mouse and keyboard; Add a tile (primary) beside Columns (`Segmented`); then "Not on the phone for a visitor", each saying why, tiles taken out in the draft muted with *Put back*; that list scrolls on its own. Opening the tile library closes the panel.
- **Stage:** the phone frame, scaled with a transform (never CSS zoom, whose boxes WebKit misreports), never below half size, the preview chips under it; it stays put while the panel scrolls, and nothing on it can be text-selected.
- **On a tile:** hover draws an inset marker ring; the chosen tile a marker ring, its size in a marker capsule in words ("2 wide, 1 tall") and a corner grip at its bottom right. Other tiles show what changed in the draft as a marker capsule in the same place.
- **Moving a tile:** after 5px of travel the tile lifts (a copy in a drag layer over the phone's screen, above the masthead and the tab bar, scaled to 1.05, the drag lift) and follows the pointer 1:1 while its own cell goes invisible. Nothing marks the landing place but the gap itself, as on an iPhone's home screen: no slot box, no column guides. The grid repacks live in the page by the same packing as `HubGrid`: once the tile's top-left cell has rested 130ms over a new cell the order changes (always planned from the arrangement the drag started with, so the picture depends only on where the tile is held), and the neighbours glide to their new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)`. Only the tiles that must move do: a small tile over a big one makes it give way downwards and the row above fills from below; nothing ever jumps. Holding the tile near the phone's top or bottom scrolls the screen every frame, faster the nearer the edge, never past the end, and the grid stays still until the scroll stops. Arrow keys move the chosen tile one cell in that direction through the same plan. Letting go settles the tile into its gap over 220ms on a gentle spring (`linear()` with a 3% overshoot) while its shadow fades; Escape, or letting go off the phone, glides everything back and scrolls the phone back smoothly. Resizing from the grip keeps the drag lift without the scale and repacks the others live as the size changes. With the keyboard, arrows move the chosen tile and Shift with arrows resizes it, and the others glide the same way. Reduced motion drops the glide and the lift's scale. Edge scrolling arms only once the pointer has left the edge zone or moved half a row. However a drag ends (drop, Escape, a lost pointer, the tab hiding), the grid is left clean: no hidden or shifted tile, and the dropped picture is exactly the order saved.
- **Settings panel:** under a *Home screen* back link: size (steppers), layout, colour, icon, words, picture, opens, and who sees it and when; actions under a hairline, a danger action on its own line.
- **Colour swatches:** an "Aa" chip per tone, drawn in the draft's theme and the preview's scheme so it matches the phone, over its staff name; the chosen one outlined in marker. A caption says the words change colour by themselves to stay easy to read. Under Picture, while the words sit over the picture, the same swatches pick the colour that fades over it (**Fade over the picture**), and the words take its paired text colour.
- **Top of Home on the canvas:** the masthead is one target; its *Changed* capsule sits under the status bar.
- **Layout picker:** stacked boxes, each name over what it draws; the chosen one ringed in marker; one that doesn't suit is ink-3 and says why.
- **Contrast:** no light and dark proof on show. The tile is measured off screen in both schemes; only under 4.5 : 1 does a warning appear at the top of the panel ("Words on this colour are hard to read in dark mode. Try another colour.").
- **Icon picker:** "Change icon" (Done while open) opens a grid of icons and the upload; the icon colour select follows once there is an icon.
- **Add a tile:** a sheet of kinds under Your library, Your own and From the app, each a card with its row tile (warm for the church's own and saved templates, accent for the app's), name, a line and its starting size.

## Tab bar canvas
The same phone as the Hub canvas, showing the draft's tab bar, with the Tab bar panel beside it.
- **On the phone:** clicking a tab (ringed in marker) opens it in the panel. Home and More open the Tab bar panel and can't be chosen (tooltips: Home is always first, More always last).
- **Tab bar panel:** a head with "Home first, up to three tabs you choose, then More."; Add a tab (primary, for whoever may change it); then *On the bar, in order*: numbered sand rows with the tab's icon and word, a marker dot when the draft changed it, and quiet *Move left* and *Move right*. Home opens the list and More closes it, as outlined rows with a lock ("Always first", "Always last" as tooltip and for screen readers). Tabs not on the bar for the person previewed follow, saying why, and a caption says a place that isn't a tab is still in More.
- **Settings panel:** the tile settings' shape under a *Tab bar* back link: Opens (the place field, without places another tab already opens), On the tab (the word, its 12-letter limit as a hint, and the icon picker without *No icon* or uploads; with no icon yet "Comes with the place you choose"), Who sees it and when with *On the bar* (the switch that puts a starting tab back), and *Take it off the bar* as the danger action.
