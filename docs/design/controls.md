# Controls

Detail behind [DESIGN.md](../../DESIGN.md), whose rules win. Sizes, fills and timings live in the CSS ([components.md](components.md)); every control is live in `/gallery`.

## Buttons
44px capsules that scale on press with a light haptic; full-width block buttons are 52px at the 14px radius.
- **Primary:** navy; a section's main action (Give online). **Quiet:** field fill; secondary actions, "Not now", "Done".
- **On masthead:** white, the main masthead action (Listen); **Ghost on masthead**, translucent white, the secondary (Watch, Remind me).
- **Live:** aqua, the Watch button while a service streams (a deeper aqua in light so white text holds 4.5:1).
- **Tinted:** deep-aqua tint and label, for a helpful action beside a quiet one that mustn't compete with the primary (*Capture this page* in the admin's Send feedback).
- **Disabled** (owner, 2026-11-11, "Outline"): no fill, an inset ink-3 ring, an ink-2 label, not pressable (on-masthead-2 on the masthead, on-primary in the selection tray). It marks every disabled control: icon buttons (44px round), filter chips, steppers, chat send, switches, and a chat message waiting to send (time and files in ink-3). Disabled rows, toggle rows, choices, the admin's plain buttons and text links turn their words ink-3 instead; a field that can't be changed keeps its fill with ink-3 text.
- **Pressed** (`Pressed`): a toggle button sets `aria-pressed`; a small icon button turns navy while on.
- **Link** (`ButtonVariant.Link`): see DESIGN.md, *Pattern map*; disabled, ink-3 with no outline.

## Chips
- **Filter chips:** sand, ink-2 text; chosen is navy with white. They scroll sideways. The admin's are smaller, wrap, take the field fill, turn primary when chosen (pressed state) and can carry a count. **Rail** (`FilterChips Rail`, one group per filter in the workbench rail): an ink-3 label, then full-width left-aligned rows with their counts at the end; the chosen one is the segment tile.
- **Toggle chips:** several on at once; sand with ink-2 off, primary on, pressed state and a haptic. The row can end in a trailing slot (the calendar's "+ Browse") or wrap. The calendars in your week use it. The **quiet** form, for a set usually all on (the weekly digest's calendars, owner 2026-11-09), wraps: off is unfilled with a faint ink-3 ring, on is sand led by an accent check.
- **Passage chips:** sand, deep-aqua text, an open-book icon; tapping opens the passage in the in-app browser.
- **Pill tones:** Marker (default: orange tint, orange-ink text) for status ("Later release"), "changed in this draft" and "changed for everyone since"; Accent for a calm state and "Test" on a test notification; Masthead for the event layer; Quiet for a state that no longer matters (an expired or turned-off code, "Crash" in App health, "Not recorded" on the Safeguarding list); Danger only for a safeguarding item that has run out ("Run out", "Overdue"). Due soon and a young person without a parent stay Marker. An optional icon leads the label.
- **Reaction chips** (`BubbleReactions`, DESIGN.md, *Pattern map*): tapping toggles with a haptic; disabled where you can't react (a read-only chat, the admin check page). The picker's chips are 44px targets.
- **Pray button:** a sand capsule that turns aqua when you are praying.
- **Chosen chips (chip picker):** choosing several from a long list (the roles a tile is shown to). Each is a primary capsule with a remove button spoken "Remove" and the name; under them a search opens a listbox of matches, those starting with what's typed first, each with an ink-2 caption. Arrows move, Enter adds, Backspace on an empty search removes the last chip, Escape clears the search.

## Segmented control
A field-fill track of equal segments; the chosen one is a white tile with the segment shadow (Automatic, Light, Dark; the inbox's Waiting and Everything; an announcement's Now and Later). A segment can carry a count, shown only above zero. A radio group: one Tab stop on the chosen segment; arrows, Home and End move it.
- **Labelled:** in a form, a label and hint above the track; stacked, each option gets a detail line. A bare track only switches what a screen shows.
- **Rows** (DESIGN.md, *Pattern map*): touch-height radio rows, the ring filling with the accent when chosen; no track.
- **On masthead:** the track is the masthead chip, the chosen segment a page-surface tile with primary text (the calendar's Week and Month switch, beside the masthead title).

## Month of days
`MonthDays`: a month to look at, not a date field (marks in DESIGN.md, *Pattern map*). 44px day tiles in tabular figures, today ringed in primary; the marks are marker fills (all of the day for all day, the top half for mornings, the bottom half for evenings, a light fill for some hours), a skipped day struck through in ink-3. Choosing a day opens what's on it. Past days can't be chosen; days outside the month are hidden. Each day's label says the date and its mark in words. A caption key sits under the grid.

## Stage pipeline
For a status moving forward through stages, where a segmented track read "like tabs, not progression buttons" (owner, 2026-11-11): one row of chevron-cut 44px stage buttons filling the width, labels wrapping to two lines in a narrow column and kept to one from 600px.
- **Passed:** deep-aqua tint, led by a check. **Current:** solid deep aqua, not pressable, marked current for screen readers. **Ahead:** fill-on-surface, ink-2.
- **Focus:** no ring; a stronger tint and an underlined label. **Narrow (440px or less):** the checks go. **While moving:** all disabled with the progress cursor.
- **Read only (`ReadOnly`):** plain labels, the current still marked for screen readers; for the person it belongs to, who can see but not move it.

The docked feedback ticket's status, and a prayer request's care steps (New, Followed up, Answered) on the phone and the Prayer desk; the requester sees it read only (Sent, On the wall, Answered). Pressing a stage moves to it, backwards too; leaving the sequence (*Mark not doing*) is a quiet action beside it, not a stage.

## Inputs / Fields
- **Style:** field fill and edge at the 12px radius, an ink-2 label above, ink-2 placeholders (notes at 14px). Every field, the composer and the admin's plain inputs included.
- **Focus:** an inset deep-aqua ring replaces the edge; elsewhere focus-visible is a deep-aqua outline.
- **Rich text** (`RichTextField`): see [posts.md](posts.md), *Formatted text*. The format bar sticks to the top of what scrolls, on the ground it sits in (`--rich-text-ground`, the writing page sets `--page`) so it never shows as a band; on the desk it stays one row, folding Code, Highlight, Strikethrough, Quote, the lists and Link into its ⋯ (in that order) when there isn't room, and on the phone it slides; its menus are the shared `Menu`, and on the phone Insert, the style and ⋯ open sheets. `RichTextEditor` (older event and settings fields) keeps its button row and "How it looks" preview until those move.
- **Search field (admin):** a 40px field up to 420px wide with a leading search icon; it searches 250ms after typing stops.
- **Copy field:** a labelled read-only field with a quiet Copy beside it that turns to "Copied" with a check, and a polite caption beneath (a hint, or how to copy by hand if copying fails). For sign-in codes and recovery links; selecting it selects the whole value.
- **Code field** (`CodeField`, Platform.Ui): for a typed or pasted code (a parent's sign-in code, an app reviewer's). It capitalises and dashes every four characters ("zf3b2td9" shows ZF3B-2TD9), with or without a pasted dash; the placeholder shows the shape (ABCD-EFGH) and the phone keyboard opens in capitals.
- **Copy button:** the same, where the value is already shown: "Copy link" (link icon) or "Copy the code" (copy icon), turning to "Copied" or "Couldn't copy: select the address", spoken politely. In Places in the app it sits between the primary action and the quiet ones.
- **Who sees it (audience picker):** segmented Everyone, Signed in and Roles (a fourth "Set up specially" only for an audience the three can't express); under Roles, the chip picker ("Find a role") with a hint. One audience can name several roles.
- **Picture field** (`PictureField`, Platform.Ui): the one way to choose a picture in a form. Empty, an add row ("Add a picture", "Choose a picture"). Chosen, the picture as people will see it, 160 to 240px wide: in the app's 16:9 frame when the surface uses one (an event: a picture far from 16:9 shown whole on the masthead colour), otherwise in its own proportions on sand. Beside it, how the app shows it ("The app shows a 1280×1707 copy · 256 KB") over quiet Change and Remove. A non-picture file shows its name and the file icon.
- **File pick** (`FilePick`, Platform.Ui): the add row that opens the photo library or files. `Kinds` (pictures; pictures and documents; pictures and videos); `Multiple` up to `Limit`, keeping the first that fit and saying so ("Only the first 4 were kept; that's the most at once."); `TooLarge` turns a file away in words. While keeping: "Keeping it on this phone…" in ink-3; problems as one warning notice under it. Its content can be replaced (Send feedback's empty frame, the filmstrip's add tile). `Box` draws it as `DropBox` with `Label` and `Hint`; `Icon` replaces its icon (the composer's paperclip).
- **Drop box** (`DropBox`, Platform.Ui; DESIGN.md, *Pattern map*): an optional accent icon over the two lines, what it takes ("Drop a document or picture here") over how. Hover, focus and files dragged over it (or its `DropOn` area) turn it deep aqua; busy, ink-3 with the progress cursor; `Chosen` makes the frame solid. It hosts any file input as its child, so features that read the file themselves (the media library, a sermon's audio, a Home icon) use it as is. `TakesPaste` takes files pasted on the page; `DropOn` takes drops on a surrounding element (marked `is-dropping`); `OnChosen` hands everything to the caller instead of keeping it. A file of a kind it doesn't take is turned away in words.
- **Showing any file** (`MediaPreview`, Platform.Ui): `MediaImage`, `MediaVideo` (inline, metadata only until played; muted without controls as a thumbnail), else its name and file icon. A video not ready is an empty placeholder.
- **Media chooser:** a wide sheet, "Choose a picture", opening on the last folder used: a search ("Find a picture in every folder", or "Find a file…"), a quiet Upload, the breadcrumbs (Library first), subfolder tiles, then 4:3 thumbnails with name and use ("Not used yet", "Used in 3 places"). A search looks in every folder: "Results for “…” in every folder", each item saying its folder ("In Sermons › 2026 series"). An empty folder is an empty state ("No pictures in this folder"). The chosen item is ringed in marker with a marker tick. The foot holds the chosen name (or "Nothing chosen yet"), Cancel and Choose (disabled until something is chosen); double-clicking chooses. Opening it moves focus into the sheet without scrolling the page behind.
- **Place field:** how staff choose what something opens. Empty, a quiet button with a link icon ("Choose a place in the app"); the map pin is kept for physical places (an event's place, the site map). Chosen, a token: the row's tile (sand for pages, accent for things in them), the name over where it is or its date (ellipsised), a quiet Change and an icon-only clear ("Open nothing"). A place that can no longer open takes the warm marker tile with an alert and says why. Change opens a wide sheet with the Places directory, listing only what the audience can open.

## Menus and popovers
- **Menu:** one quiet ⋯ button. Its panel opens below, held to the right edge (`AlignStart` for the left), or above when it would run past the bottom of what holds it (the window, the tab bar, a scrolling sheet or pane) and there is room. When it fits neither way in a short sheet it is placed in the window, never cut off; only a too-short window makes it scroll (#69), capped to the bigger side, and then a soft shadow at its top or bottom edge shows there is more. Items are 44px rows: an accent icon (a check for the chosen one), the label, an optional ink-2 line; hover and focus take Selected Fill. Arrows, Home and End move; Escape or a click outside closes. A menu with words (`Text`, `Chevron`) shows the current choice; `Block` makes it full width. Items come from `this.Item(...)` (`MenuItems`), so each action runs on the page that made it and that page redraws after it.
- **Anchored menu** (`Menu Anchor`): the same panel opened by something not drawn as a button, at the box it's given (a gallery picture's ⋯ in the editor); it closes the same ways and hands focus back. A one-line item centres its label; one with a detail line keeps its icon level with the label (#95).
- **Role menu** (DESIGN.md, *Pattern map*): shows the role ("Team", "Leader"), the current one ticked.
- **Popover:** the same surface for a panel that isn't actions. **Account menu:** from the sidebar's Account row, a 312px popover at the window's bottom left: name and email over a hairline, *Light or dark* (`SchemeChoice`), a rule, then *Your account* and *Sign out*.

## Putting things in order
`ReorderList` (DESIGN.md, *Pattern map*).
- **Rows:** at least 56px, hairline-divided: the grip, the row's content (a toggle row inside drops its hairline), an optional trailing slot, a ⋯ menu ("Move …").
- **Grip:** a 44px target; only it starts a drag, so the rest of the row scrolls the page. In `Quiet` it sits inside the row so the chosen row's fill holds it (#61), shows on hover or focus but always on touch screens, and a caption under the list says how ("Drag a calendar by its handle to change the order everyone sees. With the keyboard: Alt ↑ or Alt ↓ on its handle.").
- **Lifted row:** follows the pointer with the Pop shadow while the others slide aside. On release the new order shows at once and holds until the caller's list changes, so nothing snaps back or waits for the server (#58, #62). On the phone a queued change stays until Sync confirms it; the page's own tab strip changes once, at the drop.
- **Move up and Move down:** off at the ends; every move is announced politely ("Moved News to 2 of 4.").
- **Preview strip** (`Preview`): a line above the list following the drag live. A ministry's tabs show as words, the fixed first one (About) with the marker underline as the chosen tab; tabs that are off are left out.

## Settings groups
`Section Grouped`, with an optional `Foot` (DESIGN.md, *Pattern map*).
- **Admin:** a card the full width of its pane (owner, 2026-11-12, #71), short fields two to a line in `ui-pair--wide` (two columns once each can be 18rem), ringed by a hairline and lifted. Its title is at label size so the page or sheet title leads (2026-11-12); a sand foot holds the save bar or "These change straight away".
- **Phone:** an inset group under an ink-2 caption heading, rows on the group colour (white on a sand page in light, a raised panel in dark); a foot is an ink-2 caption or the group's save bar. A page of grouped sections takes the group ground.
- **Both:** the last row of a group has no hairline under it; the card's ring is the only edge (2026-11-12). A ⋯ menu in a card's head adds no height to it (2026-11-13).

## Date and time fields
See DESIGN.md, *Pattern map*. Admin: placeholders "Like 15 Nov", "Like 7:30pm", a calendar or clock button at the field's right end; the read-back is led by an accent check, and what it can't read is a problem line.
- **Date popover:** the month between step buttons, weekday heads, the days (Sundays in accent, today ringed in primary, the chosen day primary), and quick choices as filter chips (only those in the field's range; none with `Shortcuts="false"`).
- **Months and years:** the month heading opens the twelve months under a year stepper; the year opens twenty years, ‹ › stepping twenty. Out-of-range ones are ink-3. Choosing a year goes to its months, a month to its days; Escape steps back a level before closing.
- **Time popover:** the chosen time has an accent check. `DateTimeField` puts both under one label.

## Picture viewer
`PictureOpen` (a zoom cursor) opens the picture over the whole window on near-black: the title line ("Picture 2 of 5 · …"), 44px Download and Close, previous and next either side, and a filmstrip with the current one ringed in marker. Arrows, Home and End move, Escape closes, a swipe across turns and a swipe down closes. Under 640px the step controls go.

## Boards
`Board` and `BoardCard` (feedback tickets).
- **Columns:** sand, scrolling sideways (`Compact` beside an open record), each with a count. An empty column says so in a sentence in a dashed frame; a column can show only its latest and say how many more; *Add one here* is an accent link at its foot. A folded column is a strip with its name and count set vertically.
- **Moving:** drag, or Alt and the arrow keys; the column being dropped on gets an accent ring.
- **Card:** optional picture, meta line, title, labels; the open card is ringed in marker.

## Data table
The admin's working list in the workbench's list pane; search and filters live in the rail, or a toolbar row above when there's no rail. Sortable headers show their arrow on hover, in orange ink on the sorted column. Numbers and dates right-aligned in tabular figures, long text cut at 30ch. Selectable rows take Selected Fill when chosen (lighter on hover), focus is an inset deep-aqua ring, arrow keys move the selection. An empty table shows an empty state beneath.

## Charts
- **Stat tile:** value, label, optional change and explanation. A value too small to show (Fewer than 5) is the quiet tile, in ink-2.
- **Ranked list:** hairline rows of rank (ink-3, tabular), label and value (ink-2, tabular). Unnumbered for counts that aren't a ranking (Insights' "How phones are set up").
- **Trend line:** the main series deep aqua ending in a labelled dot, the comparison dashed ink-3 (its legend swatch too), the plot capped at 760px, a hover tooltip. The table of every number under the chart, behind a deep-aqua summary, is the accessible view; columns aren't focusable. Under two points it is a sand panel with one caption line.

## Breadcrumbs
A navigation landmark, "Where you are", of crumbs with optional icons (an image for the Library, a folder, a lock for what the app owns). Earlier crumbs are deep-aqua buttons; the last, the current place, is ink. A crumb can take a drop: only the one under the drag lights, deep aqua. At the start of a row the crumbs pull left so their text lines up with the content below.

## Selection tray
A navy bar docked at the foot of the work area while several things are chosen, carrying what can be done to all of them. It reads the count ("3 files chosen", spoken politely), then Clear and the actions; a primary action in it turns white with ink text. It rises in, except with reduced motion. Media shows it only for two or more files: one chosen file is the split view's detail.
