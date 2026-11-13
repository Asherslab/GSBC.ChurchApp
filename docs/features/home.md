# Home: the Hub the church arranges

The app's Home is a grid of tiles that staff arrange in the admin panel, in a draft, without a release.
Asked for by the owner on 2026-11-04 ("each tile on the hub configurable/addable … size/columns/rows,
what it says, images, links … colours based on the theme … icons uploaded as custom SVGs"). It is built
on Configuration's surfaces and the drafts system ([configuration.md](configuration.md),
[drafts.md](../architecture/drafts.md)); there is no parallel store.

## What people can do

| Who | Can |
|---|---|
| Everyone (the phone) | See Home as arranged: tiles of different sizes in the theme's colours, each opening its place; offline and live |
| `config.hub.edit` | In a draft: add, move, resize and recolour tiles on a phone canvas, set their words, picture, icon, link, audience and dates, upload SVG icons, set the grid's columns |
| `config.hub.publish` | Publish the draft, or turn it on for an event |

## Tiles

Every tile is a repeatable surface in the Hub area (`home.hub`), declared as a `HubTileKind`
(Platform.Contracts, `HubTiles.cs`). All kinds share one set of settings (`TileSettings`): width and
height in grid cells, a colour (a theme tone), an icon, its icon colour, a title, a line under it, a
picture with the part to keep in view, and what it opens. A kind can add settings of its own (This
week has *How many to show*).

- **Link tile** (`hub.tile`, Platform.Contracts): anything that is a title, a line, an icon, a picture and a
  link (Give, the Bible, Prayer, Life Groups, Ministries, Visit us, a campaign). Staff write everything.
- **Feature tiles,** only where the content is live from a feature (owner, 2026-11-04, note B):
  - **Notifications** (`hub.notifications`, owned by Notifications): real. "3 unread" with the newest
    title and the unread dot, or the staff's words when nothing is unread.
  - **Latest sermon** (`hub.sermon`, owned by Sermons since 2026-11-05): real. The newest sermon (or, in the *Where
    you're up to* layout, the one you're part-way through) with its picture and a play button that starts it in the
    player; the admin canvas shows the real newest one through `Sermons.Latest` ([sermons.md](sermons.md)).
  - **News** (`hub.news`, owned by News since 2026-11-13): real. Church news from the store, in one of three layouts
    the owner picked together on the decision page (`.impeccable/mocks/news-tile/`: "i love all three of A, B, and C,
    give me all three as variants/layouts of the tile"):
    - *Front page* (the default): the newest post with its picture (behind it at 1 × 1, beside it one row tall, above it
      when taller); at 2 × 2 the post before it on a line underneath, which opens that post.
    - *What's new*: how many posts are new since this phone last opened News (each post in the last week before the
      first time), with an unread dot and the newest headline; it opens the News tab. The last look is kept on the phone
      (`NewsLastLooked`, local preferences), not synced; the admin canvas counts the last week.
    - *Headlines*: the title with *All news*, then the newest three (or one more per extra row) with the time posted
      today or the day; the newest alone at 1 × 1.
    - **Urgent today** takes the tile in every layout, as at the top of the News page: a marker band across a front
      page with room for it (2 × 2 and bigger), otherwise the post itself in Highlight.
    - With no news it shows its title and "Nothing new yet". Its picture setting isn't used: the posts bring their own.
    - `HubTile` gained a `Banner` slot for the urgent band. Option D on the decision page, no tile at all, was declined.
  - **Waiting for their feature** (`WaitingTiles`, in Configuration until the feature exists): Sunday bulletin (date, the year's theme and verse), This week (the next
    gatherings from the church's weekly rhythm) and Outreach day (its live "outreach is on" state comes
    with Outreach) show sample content. When Sermons (and so on) is built, it takes its kind and view into
    its own feature and replaces the sample.
  - **Retired kinds:** Prayer, Give, Bible, Life Groups, Ministries and Visit us were kinds for a few hours
    on 2026-11-04. They're no longer offered (`offered: false`) but still declared, so a tile made with one
    draws and can be taken out; dev's Hub was rebuilt with Link tiles. Delete them once no placement uses
    them.
- The old **link tile** (`hub.link`) still draws, as a tile, for placements made before; it isn't offered
  for new tiles.

A kind's view is registered with `AddHubTile<TView>(kind)` (Platform.Ui), which also registers the
surface, so a tile kind without a view can't exist. Views draw through the shared `HubTile` frame.

### How a tile is drawn

`HubTile` picks the shape from the size, so staff never choose a layout:

- one cell: icon at the top, words at the foot (*square*);
- wider than one cell, one row: words with the icon at the end (*row*);
- two rows or more: a poster, with the bulletin's faint mark on dark tones;
- with a picture (or a feature's cover) and more than one cell: the picture fills the top.

Small cells (a 4-column grid on a narrow phone) drop the line under the title (container query).

### Layouts: a design for every size

Owner, 2026-11-04 (note D): a size is never refused because the full content doesn't fit; the tile is
designed for what it becomes at that size, and good alternatives are selectable. Each kind declares its
layouts (`TileVariant`: key, label, the sizes it suits, when it's the default) and gets a **Layout** setting
(Automatic or one of them). Automatic picks the default for the tile's size (read against the grid, so a
3-wide tile on 2 columns is 2 × 1) and whether it has a picture; a chosen layout that doesn't suit the size
falls back to the default. The settings panel lists *Automatic: …* and every layout, with ones that don't
suit the size disabled. The catalogue is in DESIGN.md (*Home tiles and their layouts*); the decision pages,
one concept-seed round per kind with sketches at 1 × 1, 2 × 1, 1 × 2 and 2 × 2, are in
`.impeccable/mocks/decision/hub-variants/`. A future kind (Sermons, Events) declares its own the same way.

### Colours: theme tones, never a hex

A tile's colour is a **tone**, a pair of theme roles (`TileTones`), named for staff by where the colour
shows (owner, 2026-11-04, note 9: no design-token words): Soft (sand on ink), Card (surface with `--tile-lift`: in light a soft ambient halo on all sides as well as the lift below, so a white card shows on the white page without a drawn border; in dark the usual lift; a Card tile with a picture gets only the lift, no edge. Owner notes 11 and 15: a hairline looked wrong, a tinted page hid the Soft tiles), Top
of Home (masthead), Buttons (primary), Pale accent, Accent (live), Pale highlight, Highlight (marker). Each
swatch shows its name, drawn in **the draft's theme**, in light or dark as the phone preview is set (owner,
2026-11-09: in a draft with an orange masthead the swatches showed the published navy). Each chip carries the
phone's palette and scheme (`data-palette`, `data-preview-scheme`), and a draft's own theme is written for it by
`ThemeStyle` (Platform.Ui, shared with `PhoneFrame`), so the swatches change as the draft's colours change. The
canvas's *Preview in* switch is the workspace's, so the swatches and the phone always agree. The hidden contrast
proof uses the same theme, so the warning now measures a custom theme too. In dark mode the pale highlight mixes 34% of the marker, not 24%, so it doesn't
turn muddy. The text colour is the
role the theme pairs with the background (`on-masthead`, `on-primary`, …), which the theme already
keeps readable, so staff can't make unreadable text. The settings panel shows the tile in light and
dark and reads the contrast off the render. Icon colours are *Goes with the tile*, Main colour, Links
and live, Marker, Quiet, or (for uploaded icons) *Its own colours*.

### The fade over a picture

A tile whose words sit over its picture (*Picture behind*) fades a colour over the picture so the words read.
It was always the masthead's; staff now choose it (owner, 2026-11-09) from the same theme colours as the tile
(`TileSettings.Fade`, key `fade`, default *Top of Home*, so every tile made before keeps its look). The words
take that colour's paired text colour. The choice shows under Picture only while the words are over the
picture, and the phone shows it live; it's a tile setting, so drafts, copies and the library carry it.

### The top of Home

The masthead's title and the line under it are a Hub surface of their own, **Top of Home** (`hub.top`, one per
Home, an arrangement like the grid) (owner, 2026-11-09). The title is the staff's words (40 characters,
"Good Shepherd" to start, also the top bar's title once Home scrolls); the line under it is *Today's date* (as
before), *Your own words* (80 characters; left empty, nothing shows) or *Nothing*. `HubSurfaces.Top.In`
reads it for `HomeScreen`, so the app, the canvas and every preview draw the same. A title longer than 16
characters may wrap, balanced, instead of running off the phone. On the canvas, click the masthead to change it.

### Uploaded SVG icons

- Staff upload an SVG from a tile's Icon choices. It's read in the browser, cleaned, shown on a sand
  and a navy tile in light and dark, named, and only then added: an **icon** placement
  (`hub.icon`, in the Hub area) whose setting holds the cleaned drawing. Tiles refer to it as
  `custom:<placement id>`. It travels in the public placements feed, so it draws offline at once.
- **Cleaning** (`SvgIcons.Sanitise`, Platform.Contracts) is an allowlist: shapes, groups, gradients,
  clip paths and masks; paint, stroke and geometry attributes; `url(#id)` references only. Everything
  else is dropped: scripts, event attributes, `style`, `foreignObject`, `image`, `use`, `a`, animation,
  external references, DTDs and entities. The result is stored normalised (a `viewBox`, no size). The
  server cleans it again on every save (`INormalises`), and the phone cleans before drawing.
- **Following the theme:** by default every paint becomes `currentColor` (`SvgIcons.Monochrome`), so
  the icon takes the tile's icon colour; `none` stays, so outlines stay outlines.
- Why not a Media file: an `<img>` can't take `currentColor`, so the drawing has to be inline, and
  keeping it in the setting means no second fetch and no offline gap.

### The tile library

Staff keep configured tiles to add again (owner, 2026-11-04, note C). **Save to the library** on a tile stores a
**saved tile** (`hub.saved`, in the Hub area): its name, its kind and every value, as a template. It shows under
*Your library* in Add a tile; adding it makes a **new, independent tile** with those values, ready to change.
**Make a copy** does the same for a tile on the canvas. It all goes through drafts and `config.hub.edit`
like any placement.

**A name of its own, one per name** (owner, 2026-11-04, notes 17–19). A saved tile's **name in the library**
is its own value (`Saved.Name`), separate from the tile's title, and never shown in the app; Save to the
library opens a small sheet with the name, filled in from the title. Names are unique in the library,
compared trimmed and ignoring case (`UniqueNames`): when the name is taken, the sheet offers **Replace
"Ministries"** (the saved tile takes this tile's values) or **Save as a new name** (fills in "Ministries 2"
to adjust). The server enforces it through the surface's `uniqueBy` setting: saving a draft or publishing
is refused when a placement it adds or renames shares a name with another. Only a changed placement can
clash, so duplicates saved before the rule still open, rename and publish; renaming one settles it.

**Managing the library** lives where people meet it: **Manage** beside *Your library*, in Add a tile and in
the settings panel when nothing is chosen (one component, `SavedTiles`). Manage turns each saved tile into
its name, renamed as you type, and **Take out**; a tile taken out leaves a line with **Undo** until Done.
Chosen over a "…" menu per tile (a menu inside a sheet needs a popover layer, and rename then needs a
second dialog); the decision page is `.impeccable/mocks/decision/hub-library/`.

Decided: **templates, not linked copies.** A linked copy would change every tile made from it when the
template changes, which surprises staff who added it to change a word or a colour; a template keeps each
tile its own. Saved tiles travel in the public placements feed like every setting, so they hold nothing
private. A picture in a saved tile isn't held "in use" by Media; if it's deleted, adding the saved tile is
refused until another picture is chosen.

### The grid

- `hub.grid` (one per Home) holds the column count: 2 to 4, 2 by default.
- `HubGrid.Pack` places tiles in order and fills gaps (dense packing, as CSS `grid-auto-flow: dense`
  would), and the phone draws each tile at its packed cell, so the canvas and every phone agree.
- **A tile wider than the grid fills the width.** Heights beyond 4 rows are held to 4.
- A cell is roughly square: its height is the column width, between 88 and 132 points. A tile one row
  tall that fills the width is as tall as its content (a list row, like This week's entries).

### Who sees it

Everyone, people signed in, or **people in roles**: one or more roles, chosen by searching (owner,
2026-11-04: a select with every role in it doesn't scale, and allowed only one). Anyone holding any of
them sees the tile. A role audience is checked through Access: Access
gives everyone who holds a role (or a role that includes it) a membership grant,
`roles.held.<role id>` (`RoleMembership`, Platform.Permissions), compiled with their other grants and
sent to their phone in the grants feed; the phone asks `IAccess` as for any permission. Dates are
church days, as for every placement.

## The admin canvas

Drafts → a draft → **Home screen** (desktop; the narrow layout keeps the list editor):

- **Toolbar:** Columns (2, 3, 4), **Preview the draft as** (anyone with all tiles, a visitor, someone
  signed in, someone in a role a tile is for), **Preview in** Light or Dark (the phone only; the admin panel keeps its own mode, and the
  switch starts on it), **Preview the draft on** a date, and Add a tile. These are worded as previews (owner, 2026-11-09)
  because "Show Home for: Everyone" read as if it decided who sees Home. Below it, a toggle lists tiles not on
  the phone now (hidden, dated, for another audience, or taken out in this draft, with *Put back*).
- **Width:** in canvas mode the drafts column folds away below 1648 px (an *All drafts* link takes its
  place), so the phone and the settings always sit side by side. The settings column scrolls on its own
  and the phone stays in view (owner, 2026-11-04, note 2, option B: pinning the middle would still leave
  the settings' foot out of reach on a short window).
- **The phone:** Home drawn by the app's own `HomeScreen`, fitted under the draft head so the whole phone shows without scrolling (never smaller than 60%, reached only on a window about 800 tall; owner note 12)
  and kept in view while the settings scroll. Tiles changed in this draft carry an orange *Changed* or
  *Added* capsule. Its status bar behaves as the app's does: clear over the masthead, and solid with the
  page's title once the screen scrolls past it (`PhoneFrame`, the same `is-stuck` rule as `Page`). It used
  to be solid all the time and overlapped the top of "Good Shepherd" by a few points, which Safari showed
  as a cut.
  Click a tile to choose it: an orange outline, its size, and a corner grip.
- **Dragging, as on an iPhone's home screen** (owner, 2026-11-04, notes 5 and 14; round 3 after the owner's
  recordings): the tile lifts as a copy in a drag layer over the phone's screen (opaque, 1.05 larger, a deep shadow,
  above the masthead and the tab bar, under the status bar) and follows the pointer with no lag, while its
  own cell goes invisible so its gap stays. There is no slot box and no column guides: the gap the tile left is where it will land, as on
  iOS. The target is the grid cell under the centre of the tile's first (top-left) cell, so a wide or tall
  tile snaps by its top-left corner; it changes only once that point is 20% past the current cell's edge and
  has rested in the new cell for 130 ms, and letting go applies a pending target at once. Every plan starts
  from the arrangement the drag began with, so the picture depends only on where the tile is held now, not
  on the path it took, and wandering off and coming back puts everything back. `HubGrid.Plan`
  (Platform.Contracts, asked through `HubCanvas.Plan`) tries a swap with each tile under the tile's target
  area, every insertion point, and two "flow around" orders (the tile pinned at the target and the others
  packed around it in their order; and the same with the displaced tiles placed first, allowed only to move
  down or sideways, never above the held tile). It keeps the order that lands the tile exactly on the target,
  preferring the fewest moves outside the target area, then the fewest tiles cascading from below, then the
  fewest moves overall; a tile dropped on another of its size swaps the two. So a small tile held over a big
  one makes the big one give way and the row above fill in from below, and a big tile moved down a row lets
  the tiles it passed fill the row above. Only when no order can land it there or cover that cell (a 1 × 1
  held to the right of an empty row) does nothing move. Tests: `HubGridTests`. The others glide to their new
  places in 220 ms (the page packs with `pack`, a port of `HubGrid.Pack`). Letting go settles the tile into
  its gap with a gentle spring, the shadow fading as it lands, then saves; Escape, or letting go off the
  phone, glides everything back and scrolls the phone back smoothly. Letting go waits for a plan still being
  asked for, so a tile rested over its target never drops into the old order. Holding a tile near the phone's top or bottom
  scrolls the screen every frame, faster the deeper into the 72 px edge zone, never past the end of the grid,
  and the grid stays still until the scroll stops. Keyboard arrows move the chosen tile one cell in that
  direction through the same plan (they used to move it one place in reading order).
  Dragging the grip resizes, and the others make room the same way. Keyboard: Tab to a tile, arrows move it
  a place, Shift with arrows resizes it, and the others glide too; each move is announced. With reduced
  motion, tiles move without gliding or lifting. Choosing a tile scrolls the phone only as far as needed to
  show it (it used to snap by 72 px, which collapsed the masthead after a click).
- **A drag always ends clean** (owner, 2026-11-04, after a tile was left invisible twice): the drop sends
  the whole planned order to Blazor (`HubCanvas.Arranged`, then `OnMove` with the tiles in sequence), not
  just the dragged tile's new index, because a plan can slide other tiles past each other and a single move
  would save a different picture from the one dropped into. The plan is asked synchronously
  (`invokeMethod`) so a save or render in progress can't delay the reflow. Every way a drag can end runs
  one idempotent clean-up: drop, Escape, letting go off the phone, `pointercancel`, `lostpointercapture`,
  the window losing focus or the tab hiding, a right click (its menu is suppressed mid-drag), a second
  pointer (ignored), a new drag during the drop glide (the earlier copy is replaced and its cell shown),
  and Blazor re-rendering mid-drag (the page re-asserts the planned layout and the dragged cell's lift).
  A self-heal (`heal`, from a MutationObserver on the grid and after every drag) re-derives the idle
  layout from Blazor's own DOM order and spans, strips stray lift classes, transforms and copies, and
  holds the dropped picture only until Blazor's render lands (1.5 s at most). Edge scrolling arms only
  once the pointer has left the edge zone or moved half a row, so picking up a tile in the last visible
  row no longer scrolls the phone at once. The lifted copy's pictures load eagerly (the tile's own are
  lazy), so the copy no longer shows a blank block for its first frames. Repros, run on dev's admin in
  Chromium and WebKit: quick repeated drags, a drag while the draft saves, a second tab moving a tile
  mid-drag, Escape during the glide, a new drag during the glide, the tab hiding, pointer capture lost, a
  right click. The actual cause of the owner's invisible tile wasn't reproduced; the self-heal makes the
  state impossible to keep.
- **Why the drag was wrong in Safari:** the phone was fitted with CSS `zoom`, and WebKit reports boxes
  inside a zoomed element unscaled, so every position the drag measured was off (the slot landed away from
  the pointer and the layout thrashed). The phone is now scaled with `transform: scale()`
  (`gsbcShell.fitToSpace`), whose boxes every browser reports on screen.
- **The page never scrolls on the canvas** (note 12): the workbench is exactly the window's height, and the
  drafts list, the middle column and the settings each scroll on their own. Since App styling (2026-11-09,
  [drafts.md](../architecture/drafts.md)) the phone is a sticky column as tall as the window and the settings
  column scrolls with the page.
- **Settings:** size steppers, its layout ("Best for this size (now …)" or one chosen; layouts that need
  a picture or a bigger tile are greyed out with the reason), colour swatches with their names, the chosen
  icon with **Change icon** opening the grid and upload, words, picture (the picture sheet, see [media.md](media.md)) and the part to keep in
  view, Opens (a place through the place picker, or a web page), who sees it, its dates and Shown. When the
  words on a colour are hard to read in light or dark, a plain warning says so and suggests another colour;
  otherwise nothing is said (owner, 2026-11-04, note 9: a ratio means nothing to staff). With nothing chosen: the
  icons uploaded so far.
- **Add a tile:** your own (Plain tile) or from the app, each with its size and, where it applies,
  *Sample until … is built*.

Everything saves into the draft a second after a change, and phones previewing it follow.

Decided on the decision page (`.impeccable/mocks/decision/hub/`), by the build while the owner slept:
the phone is the canvas (the owner's brief named it), with a list for what the canvas can't show and
*Show it as* taken from the other two sketches. Direction contracts:
`.impeccable/surfaces/tures-configuration-client-components-hub-32ff780a.md` (canvas) and
`.impeccable/surfaces/src-platform-gsbc-churchapp-platform-ui-surfaces.md` (Home).

## Places that aren't built yet

Feature tiles open their place: `/sunday`, `/week`, `/outreach`, `/life-groups`
and `/ministries` are declared in `ComingSoonDestinations` (Configuration) and drawn by the app's
*coming soon* page. A feature that's built removes its path there and declares its own; two features
declaring one path fail at start-up, so it can't be forgotten.

## Decided while building (2026-11-04, owner asleep)

- **Tile kinds are surfaces;** the feature that will own a kind declares it. Waiting kinds live in
  Configuration, which no feature references.
- **Shapes are automatic** from size and picture, not a staff choice.
- **Row height is a near-square cell (88 to 132 points),** and a full-width one-row tile fits its content.
- **SVG icons live in settings, not Media** (see above). Upload size: 200 KB in, 40 KB once cleaned.
- **A role audience is "holds the role anywhere".** Owner (`*`) holds every role's membership, so the
  owner sees every role's tiles.
- **The role list** for the audience picker comes from a new `[SignedIn]` `Access.ListRoleNames`
  (names and descriptions only).
- **No separate proof.** The settings panel used to draw the tile again in light or dark; the toolbar's
  Preview switch shows the real tile on the phone, so the panel only measures the words' contrast on hidden
  copies (built-in palette) and warns when they're hard to read.
- **The old link tile** isn't offered; existing ones still draw.
- **Layout defaults** (from the decision pages): words tiles show words and icon without a picture, the
  picture behind at 1 × 1 and above when larger; the sermon is cover first; the bulletin is the poster
  family; This week is the agenda; Notifications is count and newest at 1 × 1, newest in full one row
  wide, unread dots when tall; Outreach is the invitation. Alternatives are listed in DESIGN.md.
- **Margins** match the POC (20 px gutter, 12 px gap, 22 px under the masthead); the first install had a
  doubled inset, fixed before the owner's note A was written up. This week's *Calendar ›* sits beside its
  heading again.
- **Varied defaults** (finish review): Outreach in Pale highlight, Life Groups (Pale highlight) and
  Ministries (Pale accent) as a 1 × 1 pair, Visit us Card, so the lower Hub isn't a run of identical sand rows.

## Open

- Outreach's live state ("Outreach is on till 3pm") waits for Outreach.
- Tests: `HubGridTests`, `SvgIconTests` (Platform.Contracts.Tests), `HubTileTests`
  (Configuration.Api.Tests), the role membership test in `AccessEvaluatorTests`, `PreviewRules`.
