# Foundations

Detail behind [DESIGN.md](../../DESIGN.md) for the look as a whole: where each colour role is used, the type steps, layout measures, shadows, shapes and motion. Read DESIGN.md first; its named rules win.

## The look in full

**Creative North Star: "The Sunday Bulletin"**

The app reads like the church's own printed bulletin brought onto a phone. Every top-level tab opens on a navy masthead carrying a large title, a short line of context in beige, and the faint ring of the church's mark bleeding off the right edge; below it the page turns white and continues as a single readable column, with sand-coloured callouts where the bulletin would use a shaded box. On the long, sectioned pages (Sunday and Listen) a sticky row of section tabs sits under the masthead with an orange underline that slides to the section in view, so the page navigates like a folded bulletin rather than a dashboard.

Density is calm and generous: one column, a 20px gutter, list rows with hairline dividers instead of card stacks, and type sized off the iOS body text setting so the congregation's own Dynamic Type choice carries through. The system rejects the generic church-app template, a uniform grid of identical icon squares, and the feed of identical cards; content sits on the page, and cards appear only where a picture leads (an event, a sermon series). Home is the one tab root laid out as tiles (the Hub, owner, 2026-10-27): a grid the church arranges, where tiles differ in size, tone and content shape, so it reads as the bulletin's front page rather than an app launcher.

Light is the default scheme. An app-wide dark scheme (night navy ground, raised navy surfaces, bright aqua, orange progress) is chosen in Appearance or follows the system. A second palette, the 2026 yearly theme (the website's greens and a serif masthead), is a full alternate. Which one everyone sees is data, not a release: staff choose the year's theme under *The app's colours* on App styling (optionally for a run of dates), an event's colours go in a draft turned on between dates, and anyone who may make that choice can preview the other palette on their own phone first.

The admin panel is the same bulletin carried to the church office's desk. A persistent navy sidebar takes the masthead's place, the work area stays white, and a compact title band replaces the phone masthead. It opens on the office week: the next seven days as a sand rail of white day columns carrying what is dated, then what needs you and the areas you look after. Areas are worked in one workbench: filters in a sand rail where a page has them, the list, and the chosen record ruled off beside it with Cancel and Save at its foot. Each job is done one way, by one shared component, on the phone and on the desk alike. The panel follows the computer's light or dark setting until someone picks Light or Dark in the Account menu.

**Key Characteristics:**
- Navy masthead on every tab root, with the mark's ring echo at 7% opacity.
- Sticky section tabs with a sliding orange underline on sectioned pages.
- A navy top bar that is transparent over the masthead and turns solid once scrolled past it.
- White page, sand callouts, hairline-divided rows; shadows only on the few boxed lists and picture cards.
- Montserrat throughout at 400, 600 and 700; DM Serif Text only for yearly-theme mastheads.
- rem type tied to the iOS body text size, so Dynamic Type scales the whole app.
- In the admin panel, a 248px navy sidebar of Jump to, pinned pages and areas that fold, an orange marker bar on the current page and orange count capsules, over a white work area that opens with a title band.
- One way per job (the pattern map): the workbench for a list and its record, Cancel bottom left and Save bottom right, Delete in the item's ⋯ menu, settings in grouped sections, underline tabs for switching views.
- Home as the Hub: a 2 to 4 column grid of 14px-radius tiles in theme tones, each tile one of six shapes, sized by the church in columns and rows, under a compact masthead.
- The calendar as one running diary: a sticky navy week band with the seven-day rail over day sections of hairline agenda rows, where regular events stay quiet and only what's different (a special, a change, a date that isn't on) speaks; an event opens on a navy head with a white date block, and is edited where it is read; a regular event has a page of its own that opens on its run of dates, and a date of it is changed on its own.
- A tab bar built from what features register, ending in More; with the Hub home it grows to Home, Listen, Prayer, More. A notifications bell with an orange unread badge joins every tab root with the inbox.

## Colours

A bulletin palette: one deep brand navy for the masthead and primary actions, warm paper neutrals for the page, aqua for things you can act on or that are live, and orange as the marker for "you are here" and "new".

The palette is switched at the document root: `data-scheme` selects light or dark (the person's choice, or the phone's), `data-theme` selects the palette: `custom`, the theme staff chose (every theme, the brand and the 2026 theme included, is an ordinary theme staff can change, copy and delete, its CSS worked out by `ThemeDerivation`), or `brand`, the app's own last resort before a theme arrives. A preview frame (`data-palette`, `data-preview-scheme`) resets every role and follows its own scheme, whatever the page's. Every component reads semantic roles (masthead, primary, accent, live, marker, sand, ink), never a raw hex, which is what lets the four combinations work without per-component overrides.

### Primary
- **Brand Navy** (navy): the masthead ground, the stuck top bar, primary buttons, the play toggle, selected chips, and the default tint for row icons and agenda times. The colour of the brand guide; the bulletin's cover.
- **Deep Navy** (navy-deep): the masthead's deeper step, reserved for masthead-adjacent depth.
- **Masthead Tints** (on-masthead-2, masthead-chip): text and fills on the navy ground. Translucent white at 76% for the admin sidebar's resting links; translucent white at 12% (10% in dark) for ghost-on-masthead fills, the sidebar's Jump to field, its current link, its hover (at 60% of that), the rule above its foot group, the line that says pins couldn't be saved, and the narrow window's Menu button. A theme works both out so text keeps 4.5:1 on any masthead: the quieter text is moved towards the masthead text until it reads, and the chip steps towards the text colour, or darker instead when that would leave the text on it under 4.5:1.

### Secondary
- **Brand Aqua** (aqua): live state. The live dot, the "Watch" button when a service is streaming, switch-on, the selected check, and the "praying" state on the prayer wall.
- **Deep Aqua** (aqua-deep): aqua darkened for text on white and sand. Active tab-bar item, section links, passage chips, the series line on detail pages, tags, focus rings, input carets, and fact icons. Also the inbox's action line under a waiting row, the main series of a trend chart, the label line over a special or one-off event in the agenda, a special's mark on the month grid, add-rows ("+ Add a contact") and "+ New calendar" in the editors.

### Tertiary
- **Bulletin Orange** (orange): the marker. The sliding section-tab underline, outline bullets, unread dots, the unread badge on the bell, audio progress and the player's scrubber, today's event dot on the day rail, and text selection. In the admin panel: the sidebar's current-page bar, its count capsules and the filled pin of a pinned page, a sorted column's arrow (in orange ink), draft and scheduled capsules on the office rail, the "Not sent yet" chips, and the 2px outline on the chosen capsule or chip. In the calendar: today's dot on the week rail, a changed event's mark on the month grid, the change note's box (at 22%) and its "Changed" pill, and the count capsules in the calendar workbench's column ("Needs details", "Asked to publish"). On App styling: the inset 2px ring on the chosen theme card and the count capsule on the Drafts menu.
- **Orange Ink** (orange-ink): orange darkened for text, used in pills ("Later release"), warm row icons, count capsules in tabs and segments, "Time sensitive" on the lock-screen proof, the "Changed" lead in an event's change note, the widening sentence that warns an edit will reach more dates, and "Chosen in this draft" on a theme card.
- **On Marker** (on-marker): the near-black text on a solid orange fill (the preview banner, the canvas's size and change capsules, a Marker-toned Home tile).
- **Brand Beige** (beige): the masthead note line, the day rail's event dots, and the admin sidebar's Pinned and Everything labels, its area chevrons, Jump to's key hint and the mark; beige reads as text only on navy.

### Neutral
- **Page White** (white): the page and surface in light scheme, and all text on the masthead.
- **Sand** (sand) and **Strong Sand** (sand-strong): the bulletin's shaded boxes. Callouts, the sermon head, the player, notes, chips; strong sand for image placeholders and progress tracks.
- **Field** (field) and **Field Edge** (field-edge): what you type into or set (owner, 2026-11-11). Fields are the field fill with a 1px field edge (ink-3 at 70%), as Inputs describe; quiet buttons, filter chips, the segmented track and steppers take the fill without the edge, and the switch's off track takes the edge over a fill of its own (ink-3 at 24% over the surface). In light the fill is sand; in dark it is the panel lifted 7% towards white (#2d374d in the brand), which every theme works out the same way, so a field still shows on a raised navy panel.
- **Ink** (ink), **Ink 2** (ink-2), **Ink 3** (ink-3): headings and titles, then body prose and subtitles, then inactive tabs, hints and trailing metadata. Ink 3 is also a trend chart's comparison series, always dashed.
- **Hairline** (line): row dividers, the section-tab bar's bottom edge, the tab bar's top edge, pressed-row fill.
- **Danger** (danger): something that has failed or run out. The failing status word on the device check, and a safeguarding item that has run out: the Danger pill ("Run out" on the Safeguarding list, "Overdue" on the phone's training row), the flag notice on a person's Safeguarding tab (danger at 12% with danger text), and the blue card's status capsule (danger text on white). It is also the text of danger actions and a field's problem line, as Components describe. Due soon stays the marker (orange, or orange-ink text), so only what has already run out is red.
- **Selected Fill** (selected-fill): the chosen row in a list on the page, a tonal step (sand in light) rather than a shadow. Inside the workbench's sand list column the step goes the other way, to the segment colour (white in light), so the selected row still stands off its ground.
- **Fill on Surface** (fill-on-surface): a shaded box inside a panel; sand in light, strong sand in dark, so it never vanishes into a raised navy panel. The quiet pill, the chosen place, the layout picker's choices, the icon picker's cells, the Add a tile cards, and avatars, row-icon tiles and fact icons. Fields and quiet buttons take Field instead.
- **Device Bezel** (device-bezel): the black ring and cutout of the phone frame; device hardware, never a UI colour.

### Dark scheme
- **Night** (night) is the page; **Night Surface** (night-surface) doubles as sand, so callouts and the player become raised navy panels, with **Night Surface Strong** (night-surface-strong) for tracks and placeholders.
- **Night Masthead** (night-masthead) keeps the masthead a step lighter than the page so it still reads as a masthead.
- **Bright Aqua** (bright-aqua) replaces navy as primary and replaces both aqua roles, with **On Bright Aqua** (on-bright-aqua) as its text. Orange stays the marker; **Night Orange Ink** (night-orange-ink) carries orange text.
- Warm off-white ink (night-ink, night-ink-2, night-ink-3) rather than pure white.
- **The date block in dark** is a raised navy tile (night-surface, the surface role) with its numerals and month in bright aqua (the primary role), its weekday in night-ink-3; it reads as a dark tile on the night masthead, not a white one. Under the yearly palette the same roles give a year surface with sage numerals.
- **Night Field** (night-field, #2d374d) and **Night Field Edge** (night-field-edge, night-ink-3 at 70%) are Field in the dark scheme.
- **Night Selected Fill** (night-selected-fill) is the selected row, the same raised step as a selected segment; Fill on Surface becomes **Night Fill on Surface** (night-fill-on-surface, the strong surface). Under the yearly palette in dark the selected row is **Year Night Selected Fill** (year-night-selected-fill) and fills on the panel use Year Night Surface Strong.

### 2026 yearly palette
- **Year Green** (year-green) replaces navy as the masthead and live colour; **Year Green Deep** (year-green-deep) is primary; **Year Accent** (year-accent) is the text-on-light action colour.
- **Year Cream** (year-cream) is the masthead note; **Year Sand** and **Year Sand Strong** are the shaded boxes; green-tinted ink (year-ink, year-ink-2, year-ink-3) replaces navy-tinted ink.
- **Year Marker** (year-marker) and **Year Marker Ink** (year-marker-ink) replace orange as the marker.
- In dark, **Year Night**, **Year Night Surface**, **Year Night Masthead** and **Year Bright Sage** (with on-year-bright-sage) mirror the brand dark roles.

## Typography

**Display Font:** Montserrat (with -apple-system, system-ui, sans-serif); DM Serif Text (with Georgia, serif) for mastheads in the 2026 yearly palette only.
**Body Font:** Montserrat (with -apple-system, system-ui, sans-serif)
**Mono Font:** ui-monospace (with SF Mono, Menlo, Consolas, monospace; `--font-code`), at caption size for permission nodes and patterns in the admin panel's developer roles tool, at 0.88em for code in formatted text (inline code, code blocks, and a table's or directive's source line in the editor), and at micro size for a picture's library path under it in the editor.

**Character:** The brand-pinned Montserrat, used as the brand guide sets it: Bold for titles, Regular for reading, with 600 doing the work of labels and row titles. Tight negative tracking on the big sizes gives the masthead the bulletin's set-headline feel; the yearly theme swaps only the masthead face, so it changes the cover, not the page.

The root size is the iOS body text style (`font: -apple-system-body`) when the platform supports it, with a 17px fallback; every size is a rem multiple of that root. A member who raises their text size in iOS settings gets a larger app throughout, not just larger body copy. The body runs at 0.9412rem (16px at the default root).

### Hierarchy
- **Display** (700, 2.3529rem / 40px, line-height 1.02, -0.03em, balanced, max 13ch): the masthead title only. Under the yearly palette it is DM Serif Text 400 at -0.01em.
- **Headline** (700, 1.5882rem / 27px, 1.15, -0.02em): sermon and event detail titles.
- **Title Large** (700, 1.2941rem / 22px, -0.02em): the welcome letter heading and sheet headings.
- **Title** (700, 1.1765rem / 20px, 1.25, -0.015em): section headings; also the fallback cover title.
- **Body** (400, 0.9412rem / 16px, 1.45): default text. **Prose** keeps the size at line-height 1.62 in ink-2, capped at 65ch, for the welcome letter and descriptions.
- **Row title** (600, 0.9118rem / 15.5px, 1.3): list rows, toggle rows, callout text. Outline point headings step up to 0.9706rem at 700.
- **Label** (600, 0.8529rem / 14.5px): section tabs, section links, the series link, the top bar title (0.9412rem).
- **Caption** (400, 0.8235rem / 14px, 1.35): row subtitles, metadata, hints, chips and small buttons (at 600).
- **Micro** (600, 0.6471rem / 11px): tab-bar labels, the unread badge (700), day-rail weekdays (+0.04em). In the admin panel, uppercase for the sidebar's Pinned and Everything labels (+0.06em), Jump to's key hint as set (+0.04em, beige), the Places directory's group heads and App styling's panel groups (+0.06em, ink-3) and the office week's weekdays (+0.04em).
- **Date numeral** (700, 2rem / 34px, 1.05, -0.02em, tabular): the day of the month in an event's date block, between a micro weekday (700, +0.06em, ink-3) and a pill-size month (700), both uppercase. The compact block in the admin panel sets it at headline size.
- **Pill** (700, 0.7059rem / 12px, 1.3): pills, the label line over a special in the agenda and on an event head, the sidebar's count capsules (tabular), the date line inside an office-week capsule (at the capsule's weight), and the whole capsule on the fortnight rail.

The admin panel keeps the 17px root (there is no iOS body style on a desk) and the same rem steps: its title band uses Headline, its section heads Title, its tables row-title size at 400 with caption-size headers.

## Layout

A single scrolling column per screen inside a fixed shell; the page scroller hides its scrollbar and contains overscroll. Content sits on a 20px side gutter. Sections are separated by 30px (22px for the first after the masthead), section heads by 12px, and stacks inside a section by 14px (22px for looser stacks). The page reserves the tab bar's height plus the bottom safe area plus 32px at its foot.

Every screen is built from the same frame: a 48px top bar (plus the top safe area) laid over the masthead, the masthead itself pulled up under the bar so its navy reaches the top of the screen, then optional sticky section tabs that stop under the top bar, then sections. Tab roots carry the bell in the top bar; pushed detail pages carry a back button labelled with the parent. A page appears at once, with no fade or slide: a fade lets the masthead's navy, which sits behind every page for the overscroll, show through as a grey veil.

Spacing comes from one scale in `tokens.css`: 2, 4, 6, 8, 12, 16, 20, 24, 32 and 48px (`--space-3xs` to `--space-4xl`), with named rhythms on top: the gutter (20px), the stack (14px), the section gap (30px, 22px for the first) and the gap under a tab strip (20px). New spacing uses them; the drift ratchet only lets raw pixel spacing go down.

Rows are at least 56px tall, interactive targets at least 44px. The tab bar is a fixed five-column grid with 48px items above the bottom safe area. Section anchors keep a scroll margin of the top safe area plus 100px so a jumped-to heading lands below the sticky bars.

**With the keyboard up** (owner, 2026-11-11/12) the screen ends where the keyboard begins, and nothing moves on its own. The app follows the keyboard itself (Capacitor Keyboard with `resize: none`): it sets `keyboard-open` on the root and `--kb` to the keyboard's height, accessory bar included, and the shell shrinks to sit on it, so the status bar, the top bar and the masthead stay put and nothing slides under the clock. The tab bar and the preview banner step aside, the page's foot reserve drops to 32px, and the space behind the keyboard is page colour, never the masthead's navy. The focused field is always shown above the keyboard and above any foot bar. A sheet sits on the keyboard and is never taller than the space above it; it slides in first and only then focuses its search field. A foot bar (Send, Save) stays docked just above the keyboard with only its actions and any warning; its explanatory lines and quiet links come back when the keyboard goes. The chat composer docks on the keyboard the same way. On Android the window itself resizes, so `--kb` stays 0 and only the class applies. The rules live in their own block at the end of `ui.css`.

The calendar's Week view replaces section tabs with a sticky week band: the masthead ("This week", the Week and Month switch beside the title) ends 14px early and the band carries on in masthead navy, holding the beige date range and the seven-day week rail, and pins under the top bar while the days scroll beneath (the top bar turns solid after 90px). Under it, the calendar chips (14px down), then each day as a section 20px apart. Month keeps the masthead (the month as title, then a row of the year, the switch and the step buttons) over the chips, a 7-column grid padded 12 by 14px, its key, and the chosen day's agenda. An event page has no masthead of its own: its navy head (date block and words) sits under a solid top bar, then a 16px-gutter body. Prayer, More and the settings pages are short enough to need neither tabs nor a band.

## Elevation

Mostly flat and tonal. Depth comes from colour steps (navy masthead over white page, sand boxes on white, raised navy surfaces in dark) and from frosted bars, not from shadows. The section-tab bar, the event editor's foot bar and the tab bar are 90% opaque page colour with a backdrop blur (18px, 18px and 20px, saturated) so content scrolls visibly beneath them, edged by a hairline rather than a shadow. The calendar's week band is the masthead carried on as a sticky band: solid navy, no blur, no shadow.

### Shadow Vocabulary
- **Lift** (`0 1px 2px rgb(24 35 58 / 0.06), 0 10px 24px -14px rgb(24 35 58 / 0.3)`; in dark `0 1px 0 rgb(255 255 255 / 0.04) inset, 0 14px 28px -18px rgb(0 0 0 / 0.7)`): boxed lists and the event feature card only.
- **Segment** (`0 1px 3px rgb(0 0 0 / 0.14)`): the selected segment of a segmented control.
- **Knob** (`0 2px 6px rgb(0 0 0 / 0.2)`): the switch thumb.
- **Play overlay** (`0 10px 30px rgb(0 0 0 / 0.35)`): the round play button over a video hero.
- **Badge ring** (`0 0 0 2px` masthead colour): separates the unread badge from the bell.
- **Pop** (`0 0 0 1px` hairline, `0 18px 36px -16px` scrim; `0 18px 40px -12px` for the Account menu and the date and time popovers): menus, popovers and the chip picker's matches, anything that opens over the page from a button.
- **Drag lift** (`filter: drop-shadow(0 18px 26px` ink at 30%`)`, with the tile scaled to 1.04 while it moves): a Home tile while it is being moved or resized on the admin canvas, nowhere else.

## Shapes

Soft, rounded, never sharp. The working radius is 14px for anything box-shaped (callouts, sermon head, player, notes, boxed lists, feature cards, series covers, full-width buttons); 12px for the smaller boxes (status strip, fields, segmented track, now-playing art); 10px for tiles inside rows (row icons and thumbnails); 9px for a selected segment inside its track. Anything sized to a touch target is a capsule: buttons at 44px tall with a 22px radius, chips, passage chips, the pray button, pills, round icon buttons and the play toggle. Sheets round only their top corners at 22px and carry a 38 by 5px grabber. Speaker photos are circles. The calendar's date block is a 78px tile at the 14px radius (62px at 12px when compact); week-rail days and month-grid days round at 12px; month marks are 5px dots, and "not on" is a 6px open ring. Home tiles take the 14px radius and clip what they hold (a picture, the bleeding mark); their icons sit in the 34px, 10px-radius row tile. The blue card takes the 14px radius and clips a ring at its top right: a 150px circle drawn as an 18px stroke of the masthead chip colour, set 44px out past the top and right edges; its status is a 24px capsule.

The admin panel uses the same scale: sidebar links, area heads, pin buttons and the Jump to field, Jump to's rows, sortable header buttons and office-week capsules at 9px; the rail's day columns at 10px inside its 14px sand track; fields, the search field and the chosen-place token at 12px; filter chips and count capsules as capsules; media thumbnails at 8px, like the app's. A dashed line means "you can change this": the upload drop zone (1.5px of ink-3 at 45% on fill-on-surface at the 14px radius, turning deep aqua on hover or focus), and the same frame on the phone when Send feedback has nothing chosen yet. The event head is never outlined: it previews, and fields edit (owner, 2026-11-08). Folder tiles and the media grid's thumbnails take the 12px field radius (folder tiles with a hairline border, their icon tile at 9px); breadcrumbs and folder-picker rows round at 9px; the selection tray takes the 14px radius. Menus and their items round at 12px and 9px, the Account menu and the date popover at 14px, its days and times at 9px. Board columns take 14px and their cards 10px; a filter in the rail is a 9px row, not a capsule. In the picture viewer the controls are 44px circles and the filmstrip's 56px thumbnails round at 9px.

The phone frame carries device geometry, not UI shapes: a 46px screen radius inside a 9px bezel ring, a Dynamic Island capsule (124 by 36px) or a notch (160 by 32px, its bottom corners at 20px). The lock-screen proof draws a lock screen the same way: a 3.2rem tabular clock, and a 20px app icon at a 28% radius. These match the hardware being drawn; they are not part of the radius scale and never appear on a UI box.

The church mark appears only as a mask of the supplied artwork, recoloured with the current text colour: at 7% opacity bleeding off the masthead's right edge, at 10% inside fallback covers, and in the footer and boot screen.

## Motion
One curve, `cubic-bezier(0.22, 1, 0.36, 1)`, for presses, sheets and the section-tab underline. Pages appear at once (no entry fade or slide), and a screen opened again draws the data it last showed at once (`LiveData`'s `Remember`), refreshing it in place. Presses scale (0.94 to 0.98). The live dot pulses on a 1.6s cycle. A paged sheet slides a new page 14px in from the side it comes from (260ms), and the current page dot widens over 240ms. In the admin panel the sidebar's marker bar scales in over 240ms, an area's chevron turns over 200ms while its pages fade 4px into place over 200ms, hovers and fills ease over 160ms (120ms for table rows), and sort arrows fade over 160ms. Week-rail days fill over 160ms and month-grid days over 140ms; the rich text toolbar's buttons ease their fill over 140ms. A swipe from the left edge of a pushed page drags it away and goes back. On the Hub canvas tiles glide to new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)` (a curve of their own), and a dropped tile settles on a gentle `linear()` spring. Reduced motion collapses every animation and transition to 1ms, in the app and the admin panel alike; on the Hub canvas the drag still works but tiles move without gliding or lifting.

## More do's

- **Do** add sticky section tabs with the sliding orange underline to any long page divided into named sections, and keep every tab label visible at 390pt width.
- **Do** put the notifications bell in the top bar of every tab root.
- **Do** mark the chosen tile on the Hub canvas with the 2px marker ring, its size capsule in words and the corner grip, and warn in plain words only when a tone's text is hard to read in light or dark.
- **Do** keep a page's bars and path row above its list so choosing an item never moves the list.
- **Do** put a phone preview's choices under it as one sentence of sand chips that change only the phone.
- **Do** edit a regular event on its own page, from its run of dates.
- **Don't** let the Hub become a uniform grid of identical icon squares or a feed of identical cards.
