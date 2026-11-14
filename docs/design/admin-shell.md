# The admin panel's shell

Detail behind [DESIGN.md](../../DESIGN.md) for the admin panel's layout and breakpoints, the workbench, the sidebar, Jump to, the title band and the office week. Read DESIGN.md first; its rules win.

## Admin panel
The 248px navy sidebar beside the work area; each page is a title band over content capped at 1440px. Full-bleed views (the workbench, the preview window, the sign-in stage) run edge to edge.

- **Office week:** the day rail over two columns, "Needs you" (the wider) and "Your areas".
- **Announcements desk:** the fortnight rail with the "Not sent yet" chips under it, then the writing column beside a sticky proof aside (280 to 340px), capped at 1180px; "Sent earlier" follows as a table.
- **Places in the app:** the directory beside a sticky 440px phone column (380 to 560px tall), capped at 1320px.
- **App styling:** a 72px title band (DESIGN.md, *Still Frame*): the title on one line, cut with an ellipsis, over one lead line; its actions never wrap. Under it the sticky phone column (at least 520px tall) with its preview chips, and the 400px panel.
- **Media:** above 900px search, New folder and Upload sit on the title row. Under it the shelf switch (left), type chips and Grid/List (right), then the path row (breadcrumbs, the open folder's actions) and the app shelf's note, all above the split view so opening a file's detail never moves the grid. Folder tiles at least 210px wide, pictures 168px.
- **Workbench (a list and its record):** as the pattern map places it, edge to edge at the window's height: the rail 248px, the record pane 360 to 440px (320 to 380px at 1360px and under), sticky and scrolling on its own so its save bar is in reach before the page scrolls. Nothing chosen, the list takes the room. At 760px and under, or when asked to be narrow (the app's form), one pane shows at a time: the chosen record replaces the list and its Close goes back (owner, 2026-11-13), unless the page says which (`Showing`); the split view's form stacks the record under the list with a hairline above. A workbench under other content is ruled off by a hairline above and keeps the work area's sides. A table hides its optional columns under 760px and tightens under 420px.
- **Breakpoints** (offset by the sidebar, DESIGN.md, *Sidebar Offset*):
  - 1328px: a person's Roles tab drops its explanation panel under the ticks; Family and Safeguarding narrow their panel to 260 to 340px.
  - 1228px: the office week stacks and its rail scrolls sideways (days at least 84px); the announcements proof drops under the writing column (up to 360px); the Feedback board's docked ticket floats.
  - 1100px: Places drops its phone under the directory (up to 420px); App styling drops its panel under the phone (640px tall).
  - 928px: Family and Safeguarding drop their panel under the rows, fields in one column.
  - 760px: App styling's title band wraps.
  - The fortnight rail keeps days at least 56px and scrolls sideways.
- **Calendar workbench:** the rail (items and calendars) uses `FoldAside`; the middle pane opens on its own head with the Agenda and Month switch beside New event, over the agenda or the month grid (up to 640px); the record pane holds the event, a calendar's settings or a break.
- **Feedback board:** the title band with search, the Board and Blocked phones tabs, then the status columns in one row, scrolling sideways. A clicked card docks its ticket in a sticky, window-tall 360 to 440px dock ruled off beside the board (together up to 1600px); under 1228px it floats over the board, lifted. A ticket's own page (`/feedback/{number}`) is a main column beside a 280 to 360px side, capped at 1320px; one column under 1100px.
- **Settings pages:** grouped sections, each a card up to 820px wide (Components, *Settings groups*).
- **Narrow (900px and under):** the sidebar becomes a sticky top strip with the mark and name and a Menu button ("Close" while open), which opens the same sidebar as a panel up to 320px wide over the scrim. Escape, a click outside or opening a page closes it; opening moves focus to Jump to, closing returns it to Menu.

## Admin workbench and drafts
- **Workbench:** the record is flat, never a card; it opens on a `PaneHead` and ends in its `SaveBar`, docked at the pane's foot and flush with the window's bottom however short the record (#63). Adding opens a blank record, already chosen, in the same pane.
- **Pane head:** the title (at most three lines) over a lead; at the right, quiet icon buttons: the ⋯ menu (Delete last), *Open as a page* and Close.
- **Tab strip:** 44px tabs over a hairline; the selected one turns ink with the orange underline. A tab can carry a marker-tinted count; a trailing slot holds a deep-aqua add action. It switches what is shown, never scrolls the page. **Link mode:** each tab has its own address and the chosen one is the current page (a ministry's areas); a tab replaces the address rather than adding to history. **Sticky** (phone): pinned under the top bar, one row scrolling sideways. **Narrow:** at 760px and under every strip is one unwrapped row with the selected tab scrolled into view; one that isn't sticky fades its end to show there is more.
- **Save status:** one caption line, polite to screen readers: a spinner and "Saving…"; a deep-aqua check and "Saved"; a pencil and "You have changes that aren't saved" (a form saved with a button); or a cloud-off icon and "Couldn't save. Trying again by itself." in orange ink with an underlined "Try now". An optional detail follows after a middle dot.
- **Selected row:** DESIGN.md, *Tonal Selection*; it also marks the open draft in the Drafts menu. In the workbench's sand list a list-style table hides its header, so the selected row is a white segment tile.

## Admin sidebar (signature)
The masthead turned on its side. Areas are cut by the job staff are doing, in the order they reach for them (owner, 2026-11-12): Sundays & sermons, This week, Ministries & groups, Safety, People & access, The app, Technical. The foot, ruled off and pushed to the bottom, holds actions features add (Send feedback, for staff who may send it), then Account, always last, opening the Account menu.
- **Jump to:** first, "Jump to…" with its key hint ("⌘K" on a Mac, "Ctrl K" elsewhere).
- **Home, then Pinned:** Home sits alone under Jump to; pinned pages follow under "Pinned" in the order pinned. Pins are kept with the account, so they follow it to any computer (owner, 2026-11-09). With pins, the areas get an "Everything" label; without, neither label shows.
- **Area heads:** a chevron and the area's name. Nothing else at its end unless something inside waits on someone: a folded area then shows the orange count of what's waiting, never a count of pages (owner, 2026-11-14).
- **Folding:** an area starts open when the person sees 8 pages or fewer, folded otherwise; folding is remembered on that computer. The current page's area opens by itself, unremembered. Pages are alphabetical and indented, with names short enough for one line (*Import sermons*).
- **Links:** an icon and the title, cut with an ellipsis. The current link takes the masthead tint and the orange marker bar at the left edge; a pinned page is current only under Pinned. A count of things waiting (`INavCounts`) sits at the end in an orange capsule, only above zero, refreshing as you move between pages.
- **Pin:** each page row has a pin button at its end, shown on hover or focus and always on touch screens; a pinned page's is orange and filled. Pinning is only in the sidebar, not on each page (owner, 2026-11-09).
- **Actions in the foot:** a `SidebarAction` a feature registers (shown only with its permission) opens a sheet instead of going somewhere: a button drawn as a link, current while its sheet is open, announced as opening a dialog. Send feedback is first (speech-bubble icon); Account is last and opens a popover.
- **A failed save:** "Couldn't save your pins." with an underlined "Try now".

Focus rings in the sidebar and on the narrow Menu button are orange.

## Jump to
Finds any page the person's role opens, by name, area or summary: a `Sheet` named "Jump to a page", untitled, near the top of the window at 720px and wider. The search field ("Type the name of a page", focused on open) over up to 12 matches, each its icon, title, and area and summary ("The app · …"). Arrow keys move the active (sand) row, wrapping; Enter or a click opens the page and closes the sheet. A polite status says how many match; with none, "No page called “…” that your role opens." ⌘K or Ctrl K opens and closes it from anywhere unless another sheet is open, and focus returns where it was.

## Title band
The admin page head: a headline-size title (the focus target for navigation), an optional caption lead up to 72ch or a lead slot, an optional deep-aqua back link above, and actions at the end, wrapping. It sets the browser title to "Page · Good Shepherd admin".

## Office week (signature)
- **Day rail:** one shared rail, a week on the home page and a fortnight on Announcements: a sand track of white day columns, each a weekday ("Today" in orange ink) over a tabular date. Today is ringed in primary; past days are in ink-3.
- **Fortnight:** fourteen days from this week's Monday. A span over the whole rail reads "The whole fortnight" instead of "All week".
- **Capsules:** each dated item spans the days it covers, a title over its date range, packed into the highest band where it touches no other. Three kinds: **Everyone** (what everyone sees) in primary; **Event layer** in an accent tint; **Draft** (not out yet) in a marker tint. The chosen one has a 2px marker outline at a 2px offset.
- **Moments:** an item at one time (an announcement's send time) reserves its day and the next two (fewer at the rail's end) so its title gets two lines, and another item that close moves down a band; its second line is the time alone ("7am"), the full day and time in its tooltip.
- **Legend and quiet state:** a legend appears only when something is dated, listing only the kinds on the rail in the page's own words (the home page's Draft: "Not out yet: a draft or a scheduled announcement"). With nothing dated, one centred caption line.
- **Needs you:** hairline rows with a marker-tinted pencil tile, a title link, a relative date and a small quiet action. With nothing waiting, one line with a deep-aqua check.
- **Your areas:** hairline rows linking to each area, each with a one-line purpose.

## Split view
`SplitView` is the workbench without a rail, kept for pages not yet moved onto it (a drift ratchet counts them and only goes down). Its record pane is the workbench's; at 760px and under it drops beneath the list.
