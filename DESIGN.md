---
name: GSBC Church App
description: Good Shepherd Baptist Church's own app, set like its Sunday bulletin.
colors:
  navy: "#2a3d5e"
  navy-deep: "#22334f"
  aqua: "#009c8f"
  aqua-deep: "#00776d"
  orange: "#e7ae74"
  orange-ink: "#8a5422"
  beige: "#d5c9b7"
  white: "#ffffff"
  sand: "#f4efe7"
  sand-strong: "#e9e1d4"
  ink: "#18233a"
  ink-2: "#545e70"
  ink-3: "#6b7384"
  line: "rgb(42 61 94 / 0.12)"
  danger: "#b4372f"
  night: "#141d31"
  night-surface: "#1d2840"
  night-surface-strong: "#26334f"
  night-masthead: "#283c62"
  night-masthead-deep: "#20314f"
  bright-aqua: "#2ec4b6"
  on-bright-aqua: "#062522"
  night-ink: "#f2ede4"
  night-ink-2: "#b2b8c5"
  night-ink-3: "#8a92a3"
  night-orange-ink: "#f0c493"
  night-line: "rgb(255 255 255 / 0.09)"
  year-green: "#4a7355"
  year-green-deep: "#3f6449"
  year-accent: "#3b6847"
  year-sage: "#84a87d"
  year-cream: "#fffcd1"
  year-sand: "#f3f2e2"
  year-sand-strong: "#e6e8d3"
  year-ink: "#1e2a22"
  year-ink-2: "#4f5e53"
  year-ink-3: "#66736a"
  year-marker: "#c9bd4a"
  year-marker-ink: "#5e5718"
  year-night: "#111a14"
  year-night-surface: "#1a261d"
  year-night-surface-strong: "#233126"
  year-night-masthead: "#24382a"
  year-bright-sage: "#a6cf9e"
  on-year-bright-sage: "#10200f"
typography:
  display:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "2.3529rem"
    fontWeight: 700
    lineHeight: 1.02
    letterSpacing: "-0.03em"
  display-year:
    fontFamily: "DM Serif Text, Georgia, serif"
    fontSize: "2.3529rem"
    fontWeight: 400
    lineHeight: 1.02
    letterSpacing: "-0.01em"
  headline:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "1.5882rem"
    fontWeight: 700
    lineHeight: 1.15
    letterSpacing: "-0.02em"
  title-lg:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "1.2941rem"
    fontWeight: 700
    letterSpacing: "-0.02em"
  title:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "1.1765rem"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-0.015em"
  body:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.9412rem"
    fontWeight: 400
    lineHeight: 1.45
  prose:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.9412rem"
    fontWeight: 400
    lineHeight: 1.62
  row-title:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.9118rem"
    fontWeight: 600
    lineHeight: 1.3
  label:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.8529rem"
    fontWeight: 600
  caption:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.8235rem"
    fontWeight: 400
    lineHeight: 1.35
  micro:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.6471rem"
    fontWeight: 600
rounded:
  inner: "9px"
  tile: "10px"
  field: "12px"
  card: "14px"
  touch: "22px"
  full: "999px"
spacing:
  hairline: "2px"
  xs: "6px"
  sm: "10px"
  md: "14px"
  gutter: "20px"
  lg: "22px"
  section: "30px"
  touch: "44px"
  row: "56px"
components:
  button-primary:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.touch}"
    padding: "0 18px"
    height: "44px"
    typography: "{typography.label}"
  button-block:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.card}"
    height: "52px"
    width: "100%"
  button-quiet:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.touch}"
    padding: "0 18px"
    height: "44px"
  button-on-masthead:
    backgroundColor: "{colors.white}"
    textColor: "{colors.navy}"
    rounded: "{rounded.touch}"
    padding: "0 18px"
    height: "44px"
  button-live:
    backgroundColor: "{colors.aqua}"
    textColor: "{colors.white}"
    rounded: "{rounded.touch}"
    padding: "0 14px"
    height: "36px"
  button-disabled:
    backgroundColor: "{colors.sand-strong}"
    textColor: "{colors.ink-3}"
    rounded: "{rounded.touch}"
  masthead:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    typography: "{typography.display}"
    padding: "56px 20px 22px"
  status-strip:
    textColor: "{colors.white}"
    rounded: "{rounded.field}"
    padding: "10px 12px 10px 14px"
    typography: "{typography.caption}"
  section-tab:
    textColor: "{colors.ink-3}"
    typography: "{typography.label}"
    padding: "14px 0 12px"
  section-tab-active:
    textColor: "{colors.ink}"
  topbar-stuck:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    height: "48px"
  tab-bar-item:
    textColor: "{colors.ink-3}"
    typography: "{typography.micro}"
    height: "48px"
  tab-bar-item-active:
    textColor: "{colors.aqua-deep}"
  row:
    textColor: "{colors.ink}"
    typography: "{typography.row-title}"
    height: "56px"
    padding: "12px 0"
  row-icon:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.navy}"
    rounded: "{rounded.tile}"
    size: "34px"
  callout:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.card}"
    padding: "15px 16px"
  sermon-head:
    backgroundColor: "{colors.sand}"
    rounded: "{rounded.card}"
    padding: "12px"
  passage-chip:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.aqua-deep}"
    rounded: "{rounded.full}"
    padding: "0 11px"
    height: "32px"
  chip:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "36px"
  chip-selected:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  pill:
    textColor: "{colors.orange-ink}"
    rounded: "{rounded.full}"
    padding: "3px 9px"
  segmented:
    backgroundColor: "{colors.sand}"
    rounded: "{rounded.field}"
    padding: "3px"
  segmented-selected:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.inner}"
    height: "36px"
  player:
    backgroundColor: "{colors.sand}"
    rounded: "{rounded.card}"
    padding: "14px 16px 16px"
  player-toggle:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.full}"
    size: "48px"
  field:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "13px 14px"
  sheet:
    backgroundColor: "{colors.white}"
    rounded: "22px 22px 0 0"
    padding: "10px 20px 20px"
  badge-count:
    backgroundColor: "{colors.orange}"
    textColor: "#2a1a08"
    rounded: "9px"
    height: "18px"
    typography: "{typography.micro}"
---

# Design System: GSBC Church App

## Overview

**Creative North Star: "The Sunday Bulletin"**

The app reads like the church's own printed bulletin brought onto a phone. Every top-level tab opens on a navy masthead carrying a large title, a short line of context in beige, and the faint ring of the church's mark bleeding off the right edge; below it the page turns white and continues as a single readable column, with sand-coloured callouts where the bulletin would use a shaded box. On the long, sectioned pages (Sunday and Listen) a sticky row of section tabs sits under the masthead with an orange underline that slides to the section in view, so the page navigates like a folded bulletin rather than a dashboard.

Density is calm and generous: one column, a 20px gutter, list rows with hairline dividers instead of card stacks, and type sized off the iOS body text setting so the congregation's own Dynamic Type choice carries through. The system rejects the generic church-app tile grid and the feed of identical cards; content sits on the page, and cards appear only where a picture leads (an event, a sermon series).

Light is the default scheme. An app-wide dark scheme (night navy ground, raised navy surfaces, bright aqua, orange progress) is chosen in Appearance or follows the system. A second palette, the 2026 yearly theme (the website's greens and a serif masthead), is built as a full alternate so staff can compare it with the brand palette; which one ships is not decided.

**Key Characteristics:**
- Navy masthead on every tab root, with the mark's ring echo at 7% opacity.
- Sticky section tabs with a sliding orange underline on sectioned pages.
- A navy top bar that is transparent over the masthead and turns solid once scrolled past it.
- White page, sand callouts, hairline-divided rows; shadows only on the few boxed lists and picture cards.
- Montserrat throughout at 400, 600 and 700; DM Serif Text only for yearly-theme mastheads.
- rem type tied to the iOS body text size, so Dynamic Type scales the whole app.
- Five-tab bar (Sunday, Week, Listen, Prayer, More) and a notifications bell with an orange unread badge on every tab root.

## Colors

A bulletin palette: one deep brand navy for the masthead and primary actions, warm paper neutrals for the page, aqua for things you can act on or that are live, and orange as the marker for "you are here" and "new".

The palette is switched at the document root: `data-scheme` selects light or dark, `data-theme` selects the brand palette or the 2026 yearly palette. Every component reads semantic roles (masthead, primary, accent, live, marker, sand, ink), never a raw hex, which is what lets the four combinations work without per-component overrides.

### Primary
- **Brand Navy** (navy): the masthead ground, the stuck top bar, primary buttons, the play toggle, selected chips, and the default tint for row icons and agenda times. The colour of the brand guide; the bulletin's cover.
- **Deep Navy** (navy-deep): the masthead's deeper step, reserved for masthead-adjacent depth.

### Secondary
- **Brand Aqua** (aqua): live state. The live dot, the "Watch" button when a service is streaming, switch-on, the selected check, and the "praying" state on the prayer wall.
- **Deep Aqua** (aqua-deep): aqua darkened for text on white and sand. Active tab-bar item, section links, passage chips, the series line on detail pages, tags, focus rings, input carets, and fact icons.

### Tertiary
- **Bulletin Orange** (orange): the marker. The sliding section-tab underline, outline bullets, unread dots, the unread badge on the bell, audio progress and the player's scrubber, today's event dot on the day rail, and text selection.
- **Orange Ink** (orange-ink): orange darkened for text, used in pills ("Later release") and warm row icons.
- **Brand Beige** (beige): the masthead note line and the day rail's event dots, the one place beige reads as text, on navy.

### Neutral
- **Page White** (white): the page and surface in light scheme, and all text on the masthead.
- **Sand** (sand) and **Strong Sand** (sand-strong): the bulletin's shaded boxes. Callouts, the sermon head, the player, fields and notes, quiet buttons, chips, the segmented track, row-icon tiles; strong sand for image placeholders, progress tracks, the switch off-state and disabled buttons.
- **Ink** (ink), **Ink 2** (ink-2), **Ink 3** (ink-3): headings and titles, then body prose and subtitles, then inactive tabs, hints and trailing metadata.
- **Hairline** (line): row dividers, the section-tab bar's bottom edge, the tab bar's top edge, pressed-row fill.
- **Danger** (danger): the failing status word on the device check; nowhere else.

### Dark scheme
- **Night** (night) is the page; **Night Surface** (night-surface) doubles as sand, so callouts and the player become raised navy panels, with **Night Surface Strong** (night-surface-strong) for tracks and placeholders.
- **Night Masthead** (night-masthead) keeps the masthead a step lighter than the page so it still reads as a masthead.
- **Bright Aqua** (bright-aqua) replaces navy as primary and replaces both aqua roles, with **On Bright Aqua** (on-bright-aqua) as its text. Orange stays the marker; **Night Orange Ink** (night-orange-ink) carries orange text.
- Warm off-white ink (night-ink, night-ink-2, night-ink-3) rather than pure white.

### 2026 yearly palette (under comparison)
- **Year Green** (year-green) replaces navy as the masthead and live colour; **Year Green Deep** (year-green-deep) is primary; **Year Accent** (year-accent) is the text-on-light action colour.
- **Year Cream** (year-cream) is the masthead note; **Year Sand** and **Year Sand Strong** are the shaded boxes; green-tinted ink (year-ink, year-ink-2, year-ink-3) replaces navy-tinted ink.
- **Year Marker** (year-marker) and **Year Marker Ink** (year-marker-ink) replace orange as the marker.
- In dark, **Year Night**, **Year Night Surface**, **Year Night Masthead** and **Year Bright Sage** (with on-year-bright-sage) mirror the brand dark roles.
- **Year Sage** (year-sage) appears only as a swatch in the Appearance palette picker.

### Named Rules
**The Roles Not Hexes Rule.** Components use the semantic roles (masthead, primary, accent, live, marker, sand, ink, line) and never a palette hex, because every role is redefined by the scheme and the yearly theme. The only literal colours in components are the palette-picker swatches, the black video hero, and white on the play overlay.

**The Marker Rule.** Orange means "here" or "new": the active section, the unread item, how far you've listened. It is never a button fill and never decoration.

**The Aqua Acts Rule.** Aqua is for live state, for things you can act on (links, passages, the active tab), and for one short line of metadata at most (a series or recurrence line, a "Streamed on YouTube" tag). Navy is for the primary action itself. Don't use aqua for headings or running text.

## Typography

**Display Font:** Montserrat (with -apple-system, system-ui, sans-serif); DM Serif Text (with Georgia, serif) for mastheads in the 2026 yearly palette only.
**Body Font:** Montserrat (with -apple-system, system-ui, sans-serif)

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
- **Micro** (600, 0.6471rem / 11px): tab-bar labels, the unread badge (700), day-rail weekdays (+0.04em).

### Named Rules
**The Dynamic Type Rule.** Every font size is in rem off the iOS body root. Never set a font size in px; the only px type is the 17px root fallback for platforms without `-apple-system-body`.

**The One Family Rule.** Montserrat is the only face on the page. The serif belongs to the yearly palette's masthead title and nowhere else.

**The Numbers Line Up Rule.** Times, durations, dates in the day rail and positions use tabular figures.

## Layout

A single scrolling column per screen inside a fixed shell; the page scroller hides its scrollbar and contains overscroll. Content sits on a 20px side gutter. Sections are separated by 30px (22px for the first after the masthead), section heads by 12px, and stacks inside a section by 14px (22px for looser stacks). The page reserves the tab bar's height plus the bottom safe area plus 32px at its foot.

Every screen is built from the same frame: a 48px top bar (plus the top safe area) laid over the masthead, the masthead itself pulled up under the bar so its navy reaches the top of the screen, then optional sticky section tabs that stop under the top bar, then sections. Tab roots carry the bell in the top bar; pushed detail pages carry a back button labelled with the parent and slide in 20px from the right.

Rows are at least 56px tall, interactive targets at least 44px. The tab bar is a fixed five-column grid with 48px items above the bottom safe area. Section anchors keep a scroll margin of the top safe area plus 100px so a jumped-to heading lands below the sticky bars.

The Week tab replaces section tabs with a seven-column day rail inside the masthead; Prayer, More and the settings pages are short enough to need neither.

### Named Rules
**The Masthead First Rule.** Every tab root and every settings page opens on a masthead with a title; long, sectioned pages add sticky section tabs under it. Detail pages with a picture lead with the picture instead (a 16:9 hero under a solid navy top bar).

**The One Column Rule.** No grids of tiles on a tab root. The only multi-column layouts are the two-up series grid on Listen and the day rail.

## Elevation & Depth

Mostly flat and tonal. Depth comes from colour steps (navy masthead over white page, sand boxes on white, raised navy surfaces in dark) and from frosted bars, not from shadows. The section-tab bar and the tab bar are 90% opaque page colour with a backdrop blur (18px and 20px, saturated) so content scrolls visibly beneath them, edged by a hairline rather than a shadow.

### Shadow Vocabulary
- **Lift** (`0 1px 2px rgb(24 35 58 / 0.06), 0 10px 24px -14px rgb(24 35 58 / 0.3)`; in dark `0 1px 0 rgb(255 255 255 / 0.04) inset, 0 14px 28px -18px rgb(0 0 0 / 0.7)`): boxed lists and the event feature card only.
- **Segment** (`0 1px 3px rgb(0 0 0 / 0.14)`): the selected segment of a segmented control.
- **Knob** (`0 2px 6px rgb(0 0 0 / 0.2)`): the switch thumb.
- **Play overlay** (`0 10px 30px rgb(0 0 0 / 0.35)`): the round play button over a video hero.
- **Badge ring** (`0 0 0 2px` masthead colour): separates the unread badge from the bell.

### Named Rules
**The Flat Page Rule.** Rows, callouts, the sermon head and the player sit flat on the page; only a boxed list or a picture card may lift. If something needs separating, give it sand, not a shadow.

## Shapes

Soft, rounded, never sharp. The working radius is 14px for anything box-shaped (callouts, sermon head, player, notes, boxed lists, feature cards, series covers, full-width buttons); 12px for the smaller boxes (status strip, fields, segmented track, now-playing art); 10px for tiles inside rows (row icons and thumbnails); 9px for a selected segment inside its track. Anything sized to a touch target is a capsule: buttons at 44px tall with a 22px radius, chips, passage chips, the pray button, pills, round icon buttons and the play toggle. Sheets round only their top corners at 22px and carry a 38 by 5px grabber. Speaker photos are circles.

The church mark appears only as a mask of the supplied artwork, recoloured with the current text colour: at 7% opacity bleeding off the masthead's right edge, at 10% inside fallback covers, and in the footer and boot screen.

## Components

### Buttons
Capsules with a firm press: they scale to 0.96 on press with an ease-out curve, and fire a selection or light haptic.
- **Shape:** capsule (22px radius at 44px tall); full-width block buttons are 52px tall at the 14px card radius.
- **Primary:** navy fill, white label, 600 weight, 18px side padding. Used for the main action of a section (Give online, Send to the pastoral team).
- **Quiet:** sand fill, ink label. Secondary actions and "Not now"/"Done".
- **On masthead:** white fill with navy label, the main action inside a masthead (Listen). **Ghost on masthead:** translucent white fill (12%) with white label, the secondary masthead action (Watch, Remind me).
- **Live:** aqua fill, the Watch button while a service is streaming. In the light scheme the fill is a deeper aqua (#00877c) so white text holds 4.5:1.
- **Disabled:** strong-sand fill, ink-3 label, not interactive.
- **Icon buttons:** 44px round, transparent, with a translucent fill and a 0.94 scale on press.

### Chips
- **Filter chips:** 36px capsules in sand with ink-2 text; selected becomes navy with white text. Scroll horizontally without a scrollbar.
- **Passage chips:** 32px capsules in sand with deep-aqua text and a small open-book icon; tapping opens the passage in the in-app browser.
- **Pill:** a small (0.7059rem, 700) capsule of orange at 28% with orange-ink text, for status such as "Later release".
- **Pray button:** a 36px sand capsule that turns aqua when you are praying.

### Segmented control
A sand track (12px radius, 3px inset) of equal segments; the selected segment is a white tile (9px radius) with the segment shadow. Used for Automatic, Light, Dark.

### Lists and rows
The workhorse. A row is a full-width line at least 56px tall: optional leading tile or thumbnail, a title (row title) with an optional caption subtitle, and trailing metadata with a chevron (or an external-link icon when it leaves the app). Rows are divided by hairlines, start with a hairline above the list, and fill with the hairline colour while pressed.
- **Leading tiles:** 34px, 10px radius; sand with navy icon by default, aqua at 16% with deep-aqua icon for actions, orange at 30% with orange-ink icon for warm items, sand with ink-3 for quiet ones.
- **Thumbnails:** 96px wide at 16:9, 8px radius, because sermon graphics are made for 1920 by 1080 screens; a progress track (4px, strong sand, orange fill) can sit under the subtitle for sermons in progress.
- **Agenda rows:** a 66px tabular time column in navy before the title; past entries fade to 55%.
- **Inbox rows:** a 9px orange dot marks unread, with the title at 700; read rows drop the dot and the title to 500.
- **Toggle rows:** a title and caption beside a 51 by 31px switch (strong sand off, aqua on, white knob).
- **Boxed list:** the same rows inside a white, lifted, 14px panel with 16px side padding.

### Cards / Containers
- **Callout:** sand, 14px radius, 15 by 16px padding, a bold lead line then text. The bulletin's shaded box (Your challenge this week).
- **Sermon head:** a sand band (14px radius, pulled 12px into the gutter) holding a 112px 16:9 thumbnail, the sermon title and "speaker · watch or listen", linking to the sermon.
- **Feature card:** the only picture card, for upcoming events: a 16:7 cover over a white body, 14px radius, lifted.
- **Covers:** images fill their box; sermon artwork boxes are always 16:9 so the graphic's text is never cropped. With no image, a navy (or aqua-tinted, or orange-tinted) block with the mark at 10% and an optional title.
- **Series cards:** a 16:9 cover, then the series name and message count, two to a row.

### Inputs / Fields
- **Style:** borderless sand fill, 12px radius, 13 by 14px padding, body size; a 600 caption label above in ink-2. Notes fields use the 14px radius and a 180px minimum height.
- **Focus:** no outline; a 2px inset ring in deep aqua. Elsewhere focus-visible is a 2px deep-aqua outline at 2px offset.

### Navigation
- **Top bar:** 48px plus the top safe area, three-column (back, centred title, actions). Transparent over the masthead with white icons; once the page scrolls past the masthead (a per-page threshold) it turns solid navy and the title fades up into view. Detail pages with a hero use it solid from the start.
- **Tab bar:** fixed, five equal items (Sunday, Week, Listen, Prayer, More), each a 24px icon over an 11px 600 label in ink-3; the active tab is deep aqua. Tapping the active tab scrolls to the top. While audio plays, three small orange bars animate beside the Listen icon on every screen. Frosted page colour with a hairline top edge.
- **Notifications bell:** a 44px icon button in the top bar of every tab root, with an 18px orange count badge ringed in masthead navy; it opens the inbox.

### Masthead (signature)
The cover of every tab root: navy ground, the mark at 7% opacity, 210px wide, bleeding off the right; the display title (balanced, up to 13 characters a line); a note line in beige at 600 (verse and date, a date range, a one-line purpose); then an optional slot for the screen's live element. The masthead sits under the transparent top bar, padded down by the safe area plus 56px.
- **Status strip:** inside the Sunday masthead, a 12px-radius strip of translucent white (12%) with a clock icon or a pulsing aqua live dot, the next service or "Live now", and a ghost or live button.
- **Now playing:** inside the Listen masthead, 136px 16:9 artwork beside the title. With nothing playing it shows the latest sermon with Listen and Watch buttons; once audio is playing it pins that sermon instead, with a 4px orange progress line and a transport row (elapsed, 15s back, a 56px white play/pause disc, 30s forward, remaining).
- **Day rail:** inside the Week masthead, seven equal day buttons (weekday micro label, tabular date, event dot); today is a white tile with navy text and an orange dot.

### Section tabs (signature)
A sticky, horizontally scrolling row of 600 labels in ink-3 (18px apart) that pins directly under the top bar on sectioned pages. As you scroll, the tab for the section in view turns ink and a 3px orange underline slides and resizes to sit under it (240ms ease-out); the row scrolls itself to keep the active tab visible. Tapping a tab smooth-scrolls to its section with a selection haptic. All of Sunday's five tabs (Welcome, Sermons, This week, Next, Give) fit a 390pt width.

### Sermon outline and player
- **Outline:** point headings at 700, items in ink-2 prose with a 6px orange dot bullet, passage chips beneath; a "Main text" line leads with its chip.
- **Player:** a sand panel (14px radius) with a 48px navy round play toggle, label and tabular position, 15s back and 30s forward skip buttons, and an orange-accented scrubber.

### Sheet
A bottom sheet over a 38% black scrim: page-coloured, top corners 22px, a grabber, a title-large heading and ink-2 lead, rising 320ms with the ease-out curve. Used for My notes, the first-launch topics welcome and confirmations.

### Empty state
Centred: a 40px deep-aqua icon, a 1.0588rem ink title, an ink-2 line, and an optional quiet button.

### Motion
One curve, `cubic-bezier(0.22, 1, 0.36, 1)`, for presses, sheets, the section-tab underline and page entry. Tab roots fade in from 40% (220ms); pushed pages fade and slide 20px from the right. Presses scale (0.94 to 0.98). The live dot pulses on a 1.6s cycle. Reduced motion collapses every animation and transition to 1ms.

## Do's and Don'ts

### Do:
- **Do** open every tab root on a masthead with the title in display type, a beige note line and the mark at 7% opacity bleeding off the right edge.
- **Do** add sticky section tabs with the sliding orange underline to any long page divided into named sections, and keep every tab label visible at 390pt width.
- **Do** put the notifications bell in the top bar of every tab root.
- **Do** build content as hairline-divided rows on the white page, and use sand (callouts, sermon head, player, fields) for anything the bulletin would shade.
- **Do** size type in rem off the iOS body root and keep Montserrat at 400, 600 and 700.
- **Do** read colours through the semantic roles so light, dark and the yearly palette all work without component overrides.
- **Do** keep touch targets at 44px or more and give presses a scale and a haptic.

### Don't:
- **Don't** lay tab roots out as a grid of tiles or a feed of identical cards.
- **Don't** float buttons over the reading column; actions belong in the top bar, in a section, or in a sheet.
- **Don't** use orange as a button fill or decoration; it marks position and newness only.
- **Don't** add shadows to rows, callouts or the player; only boxed lists and picture cards lift.
- **Don't** use DM Serif Text anywhere but the yearly palette's masthead title.
- **Don't** redraw or restyle the church mark beyond recolouring the supplied artwork.
- **Don't** set font sizes in px.
