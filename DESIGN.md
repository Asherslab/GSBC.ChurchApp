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
  on-marker: "#2a1a08"
  beige: "#d5c9b7"
  white: "#ffffff"
  sand: "#f4efe7"
  sand-strong: "#e9e1d4"
  ink: "#18233a"
  ink-2: "#545e70"
  ink-3: "#6b7384"
  line: "rgb(42 61 94 / 0.12)"
  on-masthead-2: "rgb(255 255 255 / 0.76)"
  masthead-chip: "rgb(255 255 255 / 0.12)"
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
  selected-fill: "#f4efe7"
  fill-on-surface: "#f4efe7"
  night-selected-fill: "#34446a"
  night-fill-on-surface: "#26334f"
  year-night-selected-fill: "#2f4335"
  device-bezel: "#000000"
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
  pill:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "0.7059rem"
    fontWeight: 700
    lineHeight: 1.3
  date-numeral:
    fontFamily: "Montserrat, -apple-system, system-ui, sans-serif"
    fontSize: "2rem"
    fontWeight: 700
    lineHeight: 1.05
    letterSpacing: "-0.02em"
  code:
    fontFamily: "ui-monospace, SF Mono, Menlo, monospace"
    fontSize: "0.8235rem"
    fontWeight: 400
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
  tile-gap: "12px"
  gutter: "20px"
  tile-row-min: "88px"
  tile-row-max: "132px"
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
  pill-quiet:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink-2}"
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
  segmented-count:
    textColor: "{colors.orange-ink}"
    rounded: "{rounded.full}"
    height: "18px"
    padding: "0 5px"
    typography: "{typography.pill}"
  row-unread-dot:
    backgroundColor: "{colors.orange}"
    rounded: "{rounded.full}"
    size: "9px"
  inbox-action:
    textColor: "{colors.aqua-deep}"
    typography: "{typography.label}"
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
  sheet-cover:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    padding: "10px 20px 24px"
  page-dot:
    backgroundColor: "{colors.line}"
    rounded: "{rounded.full}"
    size: "8px"
  page-dot-current:
    backgroundColor: "{colors.orange}"
    rounded: "{rounded.full}"
    width: "20px"
    height: "8px"
  row-reading:
    textColor: "{colors.ink}"
    typography: "{typography.row-title}"
    padding: "22px 0"
  fact-icon:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.navy}"
    rounded: "{rounded.tile}"
    size: "34px"
  text-link:
    textColor: "{colors.aqua-deep}"
    typography: "{typography.label}"
    height: "44px"
  row-selected:
    backgroundColor: "{colors.selected-fill}"
    rounded: "{rounded.field}"
    padding: "0 12px"
  tab-strip-tab:
    textColor: "{colors.ink-3}"
    typography: "{typography.label}"
    height: "44px"
  tab-strip-tab-active:
    textColor: "{colors.ink}"
  preview-banner:
    backgroundColor: "{colors.orange}"
    textColor: "#2a1a08"
    typography: "{typography.caption}"
    padding: "6px 8px 6px 20px"
    height: "44px"
  preview-banner-exit:
    backgroundColor: "#2a1a08"
    textColor: "{colors.orange}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "36px"
  badge-count:
    backgroundColor: "{colors.orange}"
    textColor: "#2a1a08"
    rounded: "9px"
    height: "18px"
    typography: "{typography.micro}"
  admin-sidebar:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    width: "248px"
    padding: "22px 14px 16px"
  sidebar-group-label:
    textColor: "{colors.beige}"
    typography: "{typography.micro}"
    padding: "0 10px 6px"
  sidebar-link:
    textColor: "{colors.on-masthead-2}"
    typography: "{typography.label}"
    rounded: "{rounded.inner}"
    height: "38px"
    padding: "0 10px"
  sidebar-link-active:
    backgroundColor: "{colors.masthead-chip}"
    textColor: "{colors.white}"
  sidebar-count:
    backgroundColor: "{colors.orange}"
    textColor: "#2a1a08"
    rounded: "{rounded.full}"
    height: "20px"
    padding: "0 6px"
    typography: "{typography.pill}"
  title-band-title:
    textColor: "{colors.ink}"
    typography: "{typography.headline}"
  title-band-lead:
    textColor: "{colors.ink-2}"
    typography: "{typography.caption}"
  office-rail:
    backgroundColor: "{colors.sand}"
    rounded: "{rounded.card}"
    padding: "10px"
  office-rail-day:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.tile}"
    padding: "10px 12px 12px"
  office-rail-span-everyone:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.inner}"
    padding: "6px 10px"
    typography: "{typography.caption}"
  office-rail-span-layer:
    textColor: "{colors.aqua-deep}"
    rounded: "{rounded.inner}"
    padding: "6px 10px"
    typography: "{typography.caption}"
  office-rail-span-draft:
    textColor: "{colors.orange-ink}"
    rounded: "{rounded.inner}"
    padding: "6px 10px"
    typography: "{typography.caption}"
  office-rail-day-fortnight:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.tile}"
    padding: "8px 8px 10px"
    width: "56px"
  office-rail-span-fortnight:
    rounded: "{rounded.inner}"
    padding: "5px 7px"
    typography: "{typography.pill}"
  announcement-chip:
    textColor: "{colors.orange-ink}"
    rounded: "{rounded.full}"
    height: "34px"
    padding: "7px 12px"
    typography: "{typography.caption}"
  lock-proof:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "18px 12px 14px"
  lock-proof-note:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.touch}"
    padding: "10px 12px 12px"
  copy-field-input:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    height: "40px"
    padding: "0 12px"
    typography: "{typography.row-title}"
  stat-tile-value:
    textColor: "{colors.ink}"
    typography: "{typography.display}"
  stat-tile-value-quiet:
    textColor: "{colors.ink-2}"
    typography: "{typography.title}"
  table-header:
    textColor: "{colors.ink-3}"
    typography: "{typography.caption}"
    padding: "8px 12px"
  table-cell:
    textColor: "{colors.ink}"
    padding: "12px"
  table-row-selected:
    backgroundColor: "{colors.selected-fill}"
  search-field:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    height: "40px"
    padding: "0 12px"
  filter-chip:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.full}"
    height: "34px"
    padding: "0 12px"
    typography: "{typography.caption}"
  filter-chip-selected:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  sign-in-card:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "28px 28px 20px"
    width: "460px"
  row-selected-icon:
    backgroundColor: "{colors.white}"
    textColor: "{colors.navy}"
    rounded: "{rounded.tile}"
    size: "34px"
  sheet-wide:
    backgroundColor: "{colors.white}"
    padding: "10px 28px 20px"
    width: "760px"
  copy-link-button:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    height: "36px"
    padding: "0 14px"
    typography: "{typography.caption}"
  place-group-head:
    textColor: "{colors.ink-3}"
    typography: "{typography.micro}"
  place-explorer-aside:
    width: "440px"
  place-explorer-title:
    textColor: "{colors.ink}"
    typography: "{typography.title}"
  place-field-chosen:
    backgroundColor: "{colors.fill-on-surface}"
    rounded: "{rounded.field}"
    height: "56px"
    padding: "8px 8px 8px 12px"
  open-card-title:
    textColor: "{colors.ink}"
    typography: "{typography.title-lg}"
  masthead-home:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    typography: "{typography.display}"
    padding: "40px 20px 18px"
  hub-tile:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.card}"
    padding: "14px 14px 16px 16px"
  hub-tile-raised:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
  hub-tile-masthead:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  hub-tile-marker:
    backgroundColor: "{colors.orange}"
    textColor: "{colors.on-marker}"
  hub-arrange-size:
    backgroundColor: "{colors.orange}"
    textColor: "{colors.on-marker}"
    rounded: "{rounded.full}"
    padding: "2px 8px"
    typography: "{typography.pill}"
  hub-arrange-grip:
    backgroundColor: "{colors.orange}"
    rounded: "{rounded.inner}"
    size: "18px"
  hub-swatch:
    rounded: "{rounded.field}"
    height: "40px"
  chip-picker-chip:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.full}"
    height: "32px"
  layout-picker-choice:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "10px 12px"
  layout-picker-choice-on:
    backgroundColor: "{colors.white}"
  breadcrumb-link:
    textColor: "{colors.aqua-deep}"
    rounded: "{rounded.inner}"
    height: "34px"
    padding: "0 8px"
    typography: "{typography.row-title}"
  selection-tray:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.card}"
    height: "58px"
    padding: "8px 10px 8px 22px"
  folder-tile:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    height: "64px"
    padding: "10px 14px"
  folder-tile-hover:
    backgroundColor: "{colors.sand}"
  folder-tile-icon:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.navy}"
    rounded: "{rounded.inner}"
    size: "38px"
  media-card-thumb:
    backgroundColor: "{colors.sand-strong}"
    textColor: "{colors.ink-3}"
    rounded: "{rounded.field}"
  folder-picker-row:
    textColor: "{colors.ink}"
    rounded: "{rounded.inner}"
    height: "42px"
    padding: "0 12px"
  agenda-row:
    textColor: "{colors.ink}"
    typography: "{typography.row-title}"
    height: "44px"
    padding: "10px 0"
  agenda-row-time:
    textColor: "{colors.navy}"
    typography: "{typography.label}"
    width: "4.75rem"
  agenda-row-label:
    textColor: "{colors.aqua-deep}"
    typography: "{typography.pill}"
  agenda-row-quiet:
    textColor: "{colors.ink-2}"
  agenda-row-quiet-time:
    textColor: "{colors.ink-3}"
  agenda-row-off:
    textColor: "{colors.ink-3}"
  week-band:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    padding: "0 20px 14px"
  week-rail-day:
    textColor: "{colors.white}"
    rounded: "{rounded.field}"
    height: "44px"
    padding: "6px 0 7px"
  week-rail-day-here:
    backgroundColor: "{colors.masthead-chip}"
  week-rail-day-today:
    backgroundColor: "{colors.white}"
    textColor: "{colors.navy}"
  segmented-on-masthead:
    backgroundColor: "{colors.masthead-chip}"
    textColor: "{colors.white}"
    rounded: "{rounded.field}"
    padding: "3px"
  segmented-on-masthead-selected:
    backgroundColor: "{colors.white}"
    textColor: "{colors.navy}"
    rounded: "{rounded.inner}"
    height: "32px"
  toggle-chip:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.full}"
    height: "36px"
    padding: "0 14px"
    typography: "{typography.caption}"
  toggle-chip-on:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  month-grid-day:
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    height: "46px"
    typography: "{typography.row-title}"
  month-grid-day-today:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  event-head:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    typography: "{typography.headline}"
    padding: "6px 20px 22px"
  event-head-compact:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    typography: "{typography.title}"
    padding: "16px"
  date-block:
    backgroundColor: "{colors.white}"
    textColor: "{colors.navy}"
    rounded: "{rounded.card}"
    typography: "{typography.date-numeral}"
    width: "78px"
    padding: "8px 0 9px"
  date-block-compact:
    rounded: "{rounded.field}"
    typography: "{typography.headline}"
    width: "62px"
    padding: "6px 0"
  date-block-night:
    backgroundColor: "{colors.night-surface}"
    textColor: "{colors.bright-aqua}"
  event-change-note:
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "11px 14px"
    typography: "{typography.caption}"
  event-off-note:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.field}"
    padding: "11px 14px"
    typography: "{typography.caption}"
  add-row:
    textColor: "{colors.aqua-deep}"
    typography: "{typography.label}"
    height: "44px"
  editor-foot:
    padding: "12px 20px"
  widening:
    textColor: "{colors.orange-ink}"
    typography: "{typography.caption}"
  calendar-aside-item:
    textColor: "{colors.ink}"
    rounded: "{rounded.tile}"
    height: "40px"
    padding: "0 12px"
    typography: "{typography.label}"
  calendar-aside-item-current:
    backgroundColor: "{colors.white}"
  calendar-aside-calendar:
    rounded: "{rounded.tile}"
    height: "44px"
    padding: "0 4px 0 12px"
  count-capsule:
    backgroundColor: "{colors.orange}"
    textColor: "{colors.on-marker}"
    rounded: "{rounded.full}"
    height: "20px"
    padding: "0 6px"
    typography: "{typography.pill}"
  rich-text-tool:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.inner}"
    height: "36px"
    padding: "0 10px"
    typography: "{typography.caption}"
  rich-text-tool-pressed:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  rich-text-input:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "13px 14px"
    typography: "{typography.body}"
---

# Design System: GSBC Church App

## Overview

**Creative North Star: "The Sunday Bulletin"**

The app reads like the church's own printed bulletin brought onto a phone. Every top-level tab opens on a navy masthead carrying a large title, a short line of context in beige, and the faint ring of the church's mark bleeding off the right edge; below it the page turns white and continues as a single readable column, with sand-coloured callouts where the bulletin would use a shaded box. On the long, sectioned pages (Sunday and Listen) a sticky row of section tabs sits under the masthead with an orange underline that slides to the section in view, so the page navigates like a folded bulletin rather than a dashboard.

Density is calm and generous: one column, a 20px gutter, list rows with hairline dividers instead of card stacks, and type sized off the iOS body text setting so the congregation's own Dynamic Type choice carries through. The system rejects the generic church-app template, a uniform grid of identical icon squares, and the feed of identical cards; content sits on the page, and cards appear only where a picture leads (an event, a sermon series). Home is the one tab root laid out as tiles (the Hub, chosen 2026-10-27): a grid the church arranges, where tiles differ in size, tone and content shape, so it reads as the bulletin's front page rather than an app launcher.

Light is the default scheme. An app-wide dark scheme (night navy ground, raised navy surfaces, bright aqua, orange progress) is chosen in Appearance or follows the system. A second palette, the 2026 yearly theme (the website's greens and a serif masthead), is a full alternate. Which one everyone sees is data, not a release: staff choose it in Configuration's Colours area, optionally for a run of dates, and anyone who may make that choice can preview the other palette on their own phone first.

The admin panel is the same bulletin carried to the church office's desk. A persistent navy sidebar takes the masthead's place, the work area stays white, and a compact title band replaces the phone masthead. It opens on the office week: the next seven days as a sand rail of white day columns carrying what is dated, then what needs you and the areas you look after. Areas are worked as hairline tables with the chosen record's detail ruled off beside them. The panel follows the computer's light or dark setting.

**Key Characteristics:**
- Navy masthead on every tab root, with the mark's ring echo at 7% opacity.
- Sticky section tabs with a sliding orange underline on sectioned pages.
- A navy top bar that is transparent over the masthead and turns solid once scrolled past it.
- White page, sand callouts, hairline-divided rows; shadows only on the few boxed lists and picture cards.
- Montserrat throughout at 400, 600 and 700; DM Serif Text only for yearly-theme mastheads.
- rem type tied to the iOS body text size, so Dynamic Type scales the whole app.
- In the admin panel, a 248px navy sidebar with beige group labels, an orange marker bar on the current page and orange count capsules, over a white work area that opens with a title band.
- Home as the Hub: a 2 to 4 column grid of 14px-radius tiles in theme tones, each tile one of six shapes, sized by the church in columns and rows, under a compact masthead.
- The calendar as one running diary: a sticky navy week band with the seven-day rail over day sections of hairline agenda rows, where regular events stay quiet and only what's different (a special, a change, a date that isn't on) speaks; an event opens on a navy head with a white date block, and is edited where it is read.
- A tab bar built from what features register, ending in More; with the Hub home (favoured 2026-10-27) it grows to Home, Listen, Prayer, More. A notifications bell with an orange unread badge joins every tab root with the inbox.

## Colors

A bulletin palette: one deep brand navy for the masthead and primary actions, warm paper neutrals for the page, aqua for things you can act on or that are live, and orange as the marker for "you are here" and "new".

The palette is switched at the document root: `data-scheme` selects light or dark (the person's choice, or the phone's), `data-theme` selects the palette (`brand` or `year-2026`, staff's choice). Every component reads semantic roles (masthead, primary, accent, live, marker, sand, ink), never a raw hex, which is what lets the four combinations work without per-component overrides.

### Primary
- **Brand Navy** (navy): the masthead ground, the stuck top bar, primary buttons, the play toggle, selected chips, and the default tint for row icons and agenda times. The colour of the brand guide; the bulletin's cover.
- **Deep Navy** (navy-deep): the masthead's deeper step, reserved for masthead-adjacent depth.
- **Masthead Tints** (on-masthead-2, masthead-chip): text and fills on the navy ground. Translucent white at 76% for the admin sidebar's resting links; translucent white at 12% (10% in dark) for ghost-on-masthead fills, the sidebar's current link, its hover (at 60% of that) and the rule above its foot group.

### Secondary
- **Brand Aqua** (aqua): live state. The live dot, the "Watch" button when a service is streaming, switch-on, the selected check, and the "praying" state on the prayer wall.
- **Deep Aqua** (aqua-deep): aqua darkened for text on white and sand. Active tab-bar item, section links, passage chips, the series line on detail pages, tags, focus rings, input carets, and fact icons. Also the inbox's action line under a waiting row, the main series of a trend chart, the label line over a special or one-off event in the agenda, a special's mark on the month grid, add-rows ("+ Add a contact") and "+ New calendar" in the editors.

### Tertiary
- **Bulletin Orange** (orange): the marker. The sliding section-tab underline, outline bullets, unread dots, the unread badge on the bell, audio progress and the player's scrubber, today's event dot on the day rail, and text selection. In the admin panel: the sidebar's current-page bar and its count capsules, a sorted column's arrow (in orange ink), draft and scheduled capsules on the office rail, the "Not sent yet" chips, and the 2px outline on the chosen capsule or chip. In the calendar: today's dot on the week rail, a changed event's mark on the month grid, the change note's box (at 22%) and its "Changed" pill, and the count capsules in the calendar workbench's column ("Needs details", "Asked to publish").
- **Orange Ink** (orange-ink): orange darkened for text, used in pills ("Later release"), warm row icons, count capsules in tabs and segments, "Time sensitive" on the lock-screen proof, the "Changed" lead in an event's change note, and the widening sentence that warns an edit will reach more dates.
- **On Marker** (on-marker): the near-black text on a solid orange fill (the preview banner, the canvas's size and change capsules, a Marker-toned Home tile).
- **Brand Beige** (beige): the masthead note line, the day rail's event dots, and the admin sidebar's group labels and mark; beige reads as text only on navy.

### Neutral
- **Page White** (white): the page and surface in light scheme, and all text on the masthead.
- **Sand** (sand) and **Strong Sand** (sand-strong): the bulletin's shaded boxes. Callouts, the sermon head, the player, fields and notes, quiet buttons, chips, the segmented track, row-icon tiles; strong sand for image placeholders, progress tracks, the switch off-state and disabled buttons.
- **Ink** (ink), **Ink 2** (ink-2), **Ink 3** (ink-3): headings and titles, then body prose and subtitles, then inactive tabs, hints and trailing metadata. Ink 3 is also a trend chart's comparison series, always dashed.
- **Hairline** (line): row dividers, the section-tab bar's bottom edge, the tab bar's top edge, pressed-row fill.
- **Danger** (danger): the failing status word on the device check; nowhere else.
- **Selected Fill** (selected-fill): the chosen row in a list on the page, a tonal step (sand in light) rather than a shadow. Inside the sand drafts column the step goes the other way, to the segment colour (white in light), so the selected row still stands off its ground.
- **Fill on Surface** (fill-on-surface): fields, quiet buttons and the segmented track inside the white settings panel; sand in light, strong sand in dark, so a sand fill never vanishes into a raised navy panel. Also the quiet pill and the copy field.
- **Device Bezel** (device-bezel): the black ring and cutout of the phone frame; device hardware, never a UI colour.

### Dark scheme
- **Night** (night) is the page; **Night Surface** (night-surface) doubles as sand, so callouts and the player become raised navy panels, with **Night Surface Strong** (night-surface-strong) for tracks and placeholders.
- **Night Masthead** (night-masthead) keeps the masthead a step lighter than the page so it still reads as a masthead.
- **Bright Aqua** (bright-aqua) replaces navy as primary and replaces both aqua roles, with **On Bright Aqua** (on-bright-aqua) as its text. Orange stays the marker; **Night Orange Ink** (night-orange-ink) carries orange text.
- Warm off-white ink (night-ink, night-ink-2, night-ink-3) rather than pure white.
- **The date block in dark** is a raised navy tile (night-surface, the surface role) with its numerals and month in bright aqua (the primary role), its weekday in night-ink-3; it reads as a dark tile on the night masthead, not a white one. Under the yearly palette the same roles give a year surface with sage numerals.
- **Night Selected Fill** (night-selected-fill) is the selected row, the same raised step as a selected segment; Fill on Surface becomes **Night Fill on Surface** (night-fill-on-surface, the strong surface). Under the yearly palette in dark the selected row is **Year Night Selected Fill** (year-night-selected-fill) and fills on the panel use Year Night Surface Strong.

### 2026 yearly palette (staff's choice)
- **Year Green** (year-green) replaces navy as the masthead and live colour; **Year Green Deep** (year-green-deep) is primary; **Year Accent** (year-accent) is the text-on-light action colour.
- **Year Cream** (year-cream) is the masthead note; **Year Sand** and **Year Sand Strong** are the shaded boxes; green-tinted ink (year-ink, year-ink-2, year-ink-3) replaces navy-tinted ink.
- **Year Marker** (year-marker) and **Year Marker Ink** (year-marker-ink) replace orange as the marker.
- In dark, **Year Night**, **Year Night Surface**, **Year Night Masthead** and **Year Bright Sage** (with on-year-bright-sage) mirror the brand dark roles.
- **Year Sage** (year-sage) appears only as a swatch in the Appearance palette picker.

### Named Rules
**The Roles Not Hexes Rule.** Components use the semantic roles (masthead, primary, accent, live, marker, sand, ink, line) and never a palette hex, because every role is redefined by the scheme and the yearly theme. The only literal colours in components are the palette-picker swatches, the black video hero, and white on the play overlay.

**The Marker Rule.** Orange means "here" or "new": the active section, the unread item, how far you've listened, today on the week rail, an event changed since members last opened it, the tile chosen on the Hub canvas. It is never a button fill and never decoration. The one ground it may fill is a Home tile in the Highlight or Pale highlight tone (`marker`, `marker-soft`), which staff choose for a tile they want to stand out; no tile ships in solid Highlight by default. On the canvas it also marks the chosen picture in the media chooser.

**The Aqua Acts Rule.** Aqua is for live state, for things you can act on (links, passages, the active tab), and for one short line of metadata at most (a series or recurrence line, a "Streamed on YouTube" tag). Navy is for the primary action itself. Don't use aqua for headings or running text.

## Typography

**Display Font:** Montserrat (with -apple-system, system-ui, sans-serif); DM Serif Text (with Georgia, serif) for mastheads in the 2026 yearly palette only.
**Body Font:** Montserrat (with -apple-system, system-ui, sans-serif)
**Mono Font:** ui-monospace (with SF Mono, Menlo, monospace), at caption size, for permission nodes and patterns in the admin panel's developer roles tool only.

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
- **Micro** (600, 0.6471rem / 11px): tab-bar labels, the unread badge (700), day-rail weekdays (+0.04em). In the admin panel, uppercase for the sidebar's group labels (+0.06em), the Places directory's group heads (+0.06em, ink-3) and the office week's weekdays (+0.04em).
- **Date numeral** (700, 2rem / 34px, 1.05, -0.02em, tabular): the day of the month in an event's date block, between a micro weekday (700, +0.06em, ink-3) and a pill-size month (700), both uppercase. The compact block in the admin panel sets it at headline size.
- **Pill** (700, 0.7059rem / 12px, 1.3): pills, the label line over a special in the agenda and on an event head, the sidebar's count capsules (tabular), the date line inside an office-week capsule (at the capsule's weight), and the whole capsule on the fortnight rail.

The admin panel keeps the 17px root (there is no iOS body style on a desk) and the same rem steps: its title band uses Headline, its section heads Title, its tables row-title size at 400 with caption-size headers.

### Named Rules
**The Dynamic Type Rule.** Every font size is in rem off the iOS body root. Never set a font size in px; the only px type is the 17px root fallback for platforms without `-apple-system-body`.

**The One Family Rule.** Montserrat is the only face on the page. The serif belongs to the yearly palette's masthead title and nowhere else; the monospace belongs to raw permission nodes in the developer roles tool and nowhere else.

**The Numbers Line Up Rule.** Times, durations, dates in the day rail and positions use tabular figures; so do the agenda's time column, the week rail, the month grid, the week band's range and an event's when line and date block.

## Layout

A single scrolling column per screen inside a fixed shell; the page scroller hides its scrollbar and contains overscroll. Content sits on a 20px side gutter. Sections are separated by 30px (22px for the first after the masthead), section heads by 12px, and stacks inside a section by 14px (22px for looser stacks). The page reserves the tab bar's height plus the bottom safe area plus 32px at its foot.

Every screen is built from the same frame: a 48px top bar (plus the top safe area) laid over the masthead, the masthead itself pulled up under the bar so its navy reaches the top of the screen, then optional sticky section tabs that stop under the top bar, then sections. Tab roots carry the bell in the top bar; pushed detail pages carry a back button labelled with the parent and slide in 20px from the right.

Rows are at least 56px tall, interactive targets at least 44px. The tab bar is a fixed five-column grid with 48px items above the bottom safe area. Section anchors keep a scroll margin of the top safe area plus 100px so a jumped-to heading lands below the sticky bars.

The calendar's Week view replaces section tabs with a sticky week band: the masthead ("This week", the Week and Month switch beside the title) ends 14px early and the band carries on in masthead navy, holding the beige date range and the seven-day week rail, and pins under the top bar while the days scroll beneath (the top bar turns solid after 90px). Under it, the calendar chips (14px down), then each day as a section 20px apart. Month keeps the masthead (the month as title, then a row of the year, the switch and the step buttons) over the chips, a 7-column grid padded 12 by 14px, its key, and the chosen day's agenda. An event page has no masthead of its own: its navy head (date block and words) sits under a solid top bar, then a 16px-gutter body. Prayer, More and the settings pages are short enough to need neither tabs nor a band.

### Home
Home is the Hub: a compact masthead, then one grid of tiles on the 20px gutter, 22px below the masthead. The grid has 2 to 4 equal columns (staff's choice, 2 by default) with a 12px gap; tiles pack in order, each taking the first place its width and height fit. A row's height follows the column width and stays near square: one column's width, held between 88px and 132px. A tile spans 1 to 4 columns and 1 to 4 rows, and its height is its rows plus the gaps between them; a tile as wide as the grid and one row tall sizes to its content instead (an agenda line). A tile narrower than 112px drops its lines and its go arrow, sets its title at label size and tightens to 12px of padding.

### Admin panel
A two-column shell: a 248px navy sidebar, sticky at the full window height and scrolling on its own, beside a work area padded 28px top, 40px at the sides and 64px at the foot. Each page opens on a title band (headline title, an optional caption lead up to 72ch, actions at the end, bottom-aligned and wrapping), then its content, capped at 1440px. Full-bleed views (the workbench, the preview window, the sign-in stage) cancel the work area's padding and run edge to edge.

- **Office week:** the home page is the rail (36px below it), then two columns at 1.4 to 1 with a 48px gap: "Needs you" and "Your areas", both hairline rows at least 64px tall with a 34px leading tile.
- **Announcements desk:** the fortnight rail, the "Not sent yet" chips tucked under it, then the writing column beside a sticky proof aside (280 to 340px, 24px from the top) with a 40px gap, capped at 1180px; "Sent earlier" follows as a table.
- **Places in the app:** the directory beside a sticky 440px phone column (24px from the top) with a 40px gap, capped at 1320px. The phone's holder is as tall as the window allows between 380 and 560px (the window height less 340px), and the phone fits that height.
- **Media:** above 900px the title band keeps its search (180 to 210px), New folder and Upload on the title row. Under it, a wrapping bar (12 by 20px gaps, 16px below): the shelf switch on the left, the type filter chips and the Grid/List switch on the right. Then the path row (at least 36px: breadcrumbs, with the open folder's actions at its end) and, on the app's shelf, its note. The bar, the path row and the note span above the split view, so opening a file's detail never moves the grid at any depth. Inside the list: folder tiles auto-filling at least 210px wide (10px gaps), 16px, then the picture grid auto-filling at least 168px wide (20 by 14px gaps).
- **Split view:** a list beside a detail column of 340 to 440px with a 32px gap, capped at 1600px; the detail is sticky 24px from the top and scrolls within the window height. Between 1201 and 1440px the detail narrows to 300 to 360px with a 24px gap. Under 1200px it drops beneath the list. A table inside the list hides its optional columns once the list is narrower than 760px.
- **Breakpoints allow for the sidebar.** The work area's breakpoints are window media queries offset by the 248px sidebar: the three-pane workbench narrows its outer panes at 1648px and drops the detail under the work area at 1428px; the office week stacks its columns and lets the seven-day rail scroll sideways (days at least 84px) at 1228px, where the announcements desk also drops its proof under the writing column (static, up to 360px); the fortnight rail always keeps days at least 56px and scrolls sideways when they don't fit; the Access page stacks its role column above the place at 1328px; the Places explorer drops its phone column under the directory at 1100px (static, up to 420px, a hairline above, 24px of space).
- **Calendar workbench:** the workbench at full window height: the sand column (24 by 18px padding, 32px at its foot) holding the calendar's items and calendars; the middle pane opening on its own head (a headline title, a caption lead up to 72ch, then the Agenda and Month switch at least 13rem wide beside New event, wrapping) over the agenda as day sections 18px apart, or the month grid capped at 640px; and the detail pane ruled off by a hairline on its left (24px padding, 20px at 1648px; the rule goes at 1428px when the detail drops below), holding the event, a calendar's settings or a break as a lifted panel that clips its navy head to the 14px radius.
- **Narrow (900px and under):** the sidebar becomes a sticky top strip (8 by 12px padding) that scrolls sideways: the mark without its name, the links in one row without group labels, and the marker bar turned into a 3px underline. The work area padding drops to 20px top, 16px sides and 48px foot.

### Named Rules
**The Sidebar Offset Rule.** Admin breakpoints are window media queries offset by the sidebar's 248px. Never make the work area a size container: it becomes the containing block for fixed elements and traps sheets and overlays inside it. A component may still use a container on its own inner list (as the split view does).

**The Masthead First Rule.** Every tab root and every settings page opens on a masthead with a title; long, sectioned pages add sticky section tabs under it. Detail pages with a picture lead with the picture instead (a 16:9 hero under a solid navy top bar). The admin panel has no masthead: its navy is the sidebar, and every page opens on a title band.

**The One Column Rule.** No grids of tiles on a tab root other than Home's Hub, and no uniform icon grid anywhere: the Hub's tiles differ in size, tone and shape. Beyond the Hub, the only multi-column layouts are the two-up series grid on Listen and the day rail. On the desk the admin panel adds its shell, the day rail (a week or a fortnight) and the office week's two columns, split views, the workbench, the announcements desk's writing and proof columns, the Places explorer's directory and phone, and the media library's folder tiles and picture grid (the folders and files themselves, not tools), and still no grid of tool tiles.

## Elevation & Depth

Mostly flat and tonal. Depth comes from colour steps (navy masthead over white page, sand boxes on white, raised navy surfaces in dark) and from frosted bars, not from shadows. The section-tab bar, the event editor's foot bar and the tab bar are 90% opaque page colour with a backdrop blur (18px, 18px and 20px, saturated) so content scrolls visibly beneath them, edged by a hairline rather than a shadow. The calendar's week band is the masthead carried on as a sticky band: solid navy, no blur, no shadow.

### Shadow Vocabulary
- **Lift** (`0 1px 2px rgb(24 35 58 / 0.06), 0 10px 24px -14px rgb(24 35 58 / 0.3)`; in dark `0 1px 0 rgb(255 255 255 / 0.04) inset, 0 14px 28px -18px rgb(0 0 0 / 0.7)`): boxed lists and the event feature card only.
- **Segment** (`0 1px 3px rgb(0 0 0 / 0.14)`): the selected segment of a segmented control.
- **Knob** (`0 2px 6px rgb(0 0 0 / 0.2)`): the switch thumb.
- **Play overlay** (`0 10px 30px rgb(0 0 0 / 0.35)`): the round play button over a video hero.
- **Badge ring** (`0 0 0 2px` masthead colour): separates the unread badge from the bell.
- **Drag lift** (`filter: drop-shadow(0 18px 26px` ink at 30%`)`, with the tile scaled to 1.04 while it moves): a Home tile while it is being moved or resized on the admin canvas, nowhere else.

### Named Rules
**The Flat Page Rule.** Rows, callouts, the sermon head and the player sit flat on the page; only a boxed list, a picture card or a Home tile in the Card tone (`raised`) may lift. If something needs separating, give it sand, not a shadow. In the admin panel the only lifted things are the sign-in card, account panels, the workbench's settings panel and the selection tray (docked over a list that scrolls beneath it); tables, the office week and the split view's detail are flat, separated by hairlines and sand, and today on the rail is an inset 2px primary ring, not a shadow. The chosen capsule on the rail, or the current "Not sent yet" chip, is a 2px marker outline at a 2px offset.

**The Tonal Selection Rule.** A selected row is marked by a fill one tonal step off its ground (Selected Fill, rounded to the field radius and widened 12px into the gutter on each side, its hairline hidden), never by a shadow or a border. A sand leading tile on a selected row takes the page colour (white in light, night in dark) so it still reads as a tile; an accent tile keeps its tint.

## Shapes

Soft, rounded, never sharp. The working radius is 14px for anything box-shaped (callouts, sermon head, player, notes, boxed lists, feature cards, series covers, full-width buttons); 12px for the smaller boxes (status strip, fields, segmented track, now-playing art); 10px for tiles inside rows (row icons and thumbnails); 9px for a selected segment inside its track. Anything sized to a touch target is a capsule: buttons at 44px tall with a 22px radius, chips, passage chips, the pray button, pills, round icon buttons and the play toggle. Sheets round only their top corners at 22px and carry a 38 by 5px grabber. Speaker photos are circles. The calendar's date block is a 78px tile at the 14px radius (62px at 12px when compact); week-rail days and month-grid days round at 12px; month marks are 5px dots, and "not on" is a 6px open ring. Home tiles take the 14px radius and clip what they hold (a picture, the bleeding mark); their icons sit in the 34px, 10px-radius row tile.

The admin panel uses the same scale: sidebar links, sortable header buttons and office-week capsules at 9px; the rail's day columns at 10px inside its 14px sand track; fields, the search field and the chosen-place token at 12px; filter chips and count capsules as capsules; media thumbnails at 8px, like the app's. A dashed line means "you can change this": the upload drop zone (1.5px of ink-3 at 45% on fill-on-surface at the 14px radius, turning deep aqua on hover or focus), and, in the event editor, the outlines round what can be edited in place on the navy head: 2px dashed on-masthead at 50% (3px out) round the date block, at 45% (4px out, 9px radius) round the title and the when line, turning solid marker on focus. Folder tiles and the media grid's thumbnails take the 12px field radius (folder tiles with a hairline border, their icon tile at 9px); breadcrumbs and folder-picker rows round at 9px; the selection tray takes the 14px radius.

The phone frame carries device geometry, not UI shapes: a 46px screen radius inside a 9px bezel ring, a Dynamic Island capsule (124 by 36px) or a notch (160 by 32px, its bottom corners at 20px). The lock-screen proof draws a lock screen the same way: a 3.2rem tabular clock, and a 20px app icon at a 28% radius. These match the hardware being drawn; they are not part of the radius scale and never appear on a UI box.

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
- **Filter chips:** 36px capsules in sand with ink-2 text; selected becomes navy with white text. Scroll horizontally without a scrollbar. The admin's shared filter chips are smaller (34px, 12px padding, caption at 600), wrap with a 6px gap, turn ink on hover and primary with on-primary text when chosen (pressed state for screen readers), and can carry a tabular count at 75% opacity.
- **Toggle chips:** several on at once, each a 36px capsule (14px at the sides, caption at 600): sand with ink-2 when off, primary with on-primary when on, pressed state for screen readers and a haptic on tap. The row scrolls sideways into the gutter without a scrollbar, 8px apart, and can end in a trailing slot (the calendar's deep-aqua "+ Browse" at caption 600); the wrap form wraps in place instead. The calendars in your week use it.
- **Passage chips:** 32px capsules in sand with deep-aqua text and a small open-book icon; tapping opens the passage in the in-app browser.
- **Pill:** a small (0.7059rem, 700) capsule of orange at 28% with orange-ink text, for status such as "Later release".
- **Pray button:** a 36px sand capsule that turns aqua when you are praying.
- **Chosen chips (chip picker):** for choosing several from a long list, such as the roles a tile is shown to. Each choice is a 32px primary capsule with on-primary caption text at 600 and a 28px round remove button (on-primary at 16% on hover, spoken as "Remove" and the name). Under them a 44px fill-on-surface search at the field radius (leading search icon, row-size text, deep-aqua caret, the 2px inset deep-aqua ring on focus) opens a listbox of matches on the surface, ringed by a hairline and lifted, up to 18rem tall: names starting with what's typed come first, each the name at row size and 600 over a caption in ink-2, the active one on sand. Arrows move through it, Enter adds, Backspace on an empty search removes the last chip, Escape clears the search.
- **Pill tones:** Marker (the default, orange as above) for "changed in this draft" and "changed for everyone since"; Accent (aqua at 16% with deep-aqua text) for a calm state, and "Test" on a test notification; Masthead (solid masthead colour with white text) for the event layer; Quiet (fill-on-surface with ink-2 text) for a state that no longer matters, such as an expired or turned-off code, and the "Crash" mark in App health. An optional small icon leads the label.

### Segmented control
A sand track (12px radius, 3px inset) of equal segments; the selected segment is a white tile (9px radius) with the segment shadow. Used for Automatic, Light, Dark, the inbox's Waiting and Everything, and an announcement's Now and Later. A segment can carry a count in the same orange capsule as the tab strip's (shown only above zero).
- **On masthead:** inside a masthead the track is the masthead chip (white at 12%), its segments 32px tall in on-masthead, and the selected one the page-surface tile with primary text. The calendar's Week and Month switch uses it, beside the masthead title.

### Lists and rows
The workhorse. A row is a full-width line at least 56px tall: optional leading tile or thumbnail, a title (row title) with an optional caption subtitle, and trailing metadata with a chevron (or an external-link icon when it leaves the app). Rows are divided by hairlines, start with a hairline above the list, and fill with the hairline colour while pressed.
- **Leading tiles:** 34px, 10px radius; sand with navy icon by default, aqua at 16% with deep-aqua icon for actions, orange at 30% with orange-ink icon for warm items, sand with ink-3 for quiet ones.
- **Thumbnails:** 96px wide at 16:9, 8px radius, because sermon graphics are made for 1920 by 1080 screens; a progress track (4px, strong sand, orange fill) can sit under the subtitle for sermons in progress.
- **Agenda rows:** at least 44px, 10px above and below, top-aligned: a 4.75rem time column (label size, 600, tabular, primary) before the title. A special or one-off carries a label line above its title, pill size at 700 in accent, and its time in primary; a regular row is quiet: no label, its title at 400 in ink-2 and its time at 400 in ink-3. A date that isn't on keeps its row, title and time at 400 in ink-3, with a caption line under the title in ink-3 led by the small circle-off icon saying why ("Kids camp weekend · back on 20 November"), never a suffix on the title. A row missing details says what's missing in a caption in orange ink at 600. Past rows fade to 55%; a selected row takes the Tonal Selection. Within a day section the hairline sits above each row.
- **Unread and read rows:** an unread row has a 9px orange dot in a 17px gutter before its title, and the title at 700; a read row has no dot and its title at 500. Rows that are neither keep 600.
- **Inbox:** the Notifications page's masthead note counts what needs you ("2 waiting for you · 3 new"). While something waits, a Waiting and Everything switch sits above the list, its Waiting segment carrying the count. Waiting rows are reading rows on the warm tile with an action line below in deep aqua at label size and 600 ("Preview it" and a chevron, or "Do this in the admin panel"), and a hint under the list. Everything shows every item (warm tiles for those still waiting, quiet tiles for the rest), an Accent "Test" pill on test notifications, and a small quiet "Mark all as read" under the list while anything is unread.
- **Hint:** a caption line in ink-2, up to 65ch, 14px under a list, for one sentence about how the list behaves.
- **Toggle rows:** a title and caption beside a 51 by 31px switch (strong sand off, aqua on, white knob).
- **Boxed list:** the same rows inside a white, lifted, 14px panel with 16px side padding.
- **Reading rows:** for entries you read rather than tap. Top-aligned, 22px above and below, the leading tile nudged up to sit with the title, and the subtitle at body size in ink, up to 65ch. Anything more (facts, a text link) goes below the subtitle.
- **Title extra:** a slot just after the title, for a pill such as "Staff only".
- **Fact list:** labelled lines on hairlines: a 600 caption label in ink-3 over row-size text in ink. With an icon, each fact has a 34px sand tile with a navy icon, as a row does, and every fact starts with a hairline. Plain facts set their text in ink-2 and drop the first hairline, so they read as a row's detail. A fact with no text is not shown.
- **Text link:** a deep-aqua 600 label-size link with a trailing chevron and a 44px target, for a "Show me" inside a row.

### Cards / Containers
- **Callout:** sand, 14px radius, 15 by 16px padding, a bold lead line then text. The bulletin's shaded box (Your challenge this week).
- **Sermon head:** a sand band (14px radius, pulled 12px into the gutter) holding a 112px 16:9 thumbnail, the sermon title and "speaker · watch or listen", linking to the sermon.
- **Feature card:** the only picture card, for upcoming events: a 16:7 cover over a white body, 14px radius, lifted.
- **Covers:** images fill their box; sermon artwork boxes are always 16:9 so the graphic's text is never cropped. With no image, a navy (or aqua-tinted, or orange-tinted) block with the mark at 10% and an optional title.
- **Series cards:** a 16:9 cover, then the series name and message count, two to a row.

### Inputs / Fields
- **Style:** borderless sand fill, 12px radius, 13 by 14px padding, body size; a 600 caption label above in ink-2. Notes fields use the 14px radius and a 180px minimum height.
- **Focus:** no outline; a 2px inset ring in deep aqua. Elsewhere focus-visible is a 2px deep-aqua outline at 2px offset.
- **Rich text:** a wrapping toolbar of 36px fill-on-surface buttons 4px apart at the 9px radius (caption at 600, ink-2, ink on hover, primary with on-primary when pressed), over the text as a field: fill-on-surface at the 12px radius, 13 by 14px padding, body size at 1.5, deep-aqua caret, the 2px inset deep-aqua ring on focus, resizing vertically. Under it the preview, "How it looks", is a plain caption at 600 in ink-2 over the rendered text.
- **Search field (admin):** a 40px fill-on-surface field at the 12px radius, up to 420px wide, with a leading search icon in ink-3, row-size text, an ink-2 placeholder and a deep-aqua caret; focus is the 2px inset deep-aqua ring on the whole field. It settles 250ms after typing stops before it searches.
- **Copy field:** a labelled, read-only 40px field in fill-on-surface at the 12px radius (row-size tabular text, a little tracking), with a small quiet Copy button beside it that turns to "Copied" with a check, and a polite caption status line beneath (a hint, or how to copy by hand if copying fails). Used for sign-in codes and recovery links; selecting the field selects the whole value.
- **Copy button:** the copy field without its field, where the value is already shown beside it: a small quiet capsule with a link icon ("Copy link") that turns to "Copied" with a check, or "Couldn't copy: select the address" with an alert icon, spoken politely. In Places in the app it sits between the primary action and the quiet ones.
- **Who sees it (audience picker):** a segmented Everyone, Signed in and Roles (a fourth "Set up specially" appears only for an audience the three can't express), and under Roles the chip picker of roles ("Find a role") with a hint line; one audience can name several roles.
- **Media chooser:** choosing a picture or file opens a wide sheet titled "Choose a picture" with Media's own picker. It opens on the last folder used. On top, a search ("Find a picture in every folder", or "Find a file in every folder") and a quiet Upload; then the folder breadcrumbs (Library first) and the open folder's subfolders as compact folder tiles (at least 180px wide, 8px gaps, 50px tall with a 32px icon tile); then an auto-filling grid of items at least 150px wide (14 by 12px gaps, up to 52vh or 560px tall, scrolling on its own), each a 4:3 thumbnail on strong sand at the field radius over its name in caption at 600 (one line, ellipsis) and where it's used in caption ink-3 ("Not used yet", "Used in 3 places"). Searching looks in every folder: the breadcrumbs and tiles give way to "Results for “…” in every folder" at 600 in ink-2, and each item says which folder it's in ("In Sermons › 2026 series"). An empty folder is an empty state ("No pictures in this folder"). Hover rings the thumbnail 2px in marker at 55%; the chosen one is ringed 3px in marker with a 26px marker tick disc in its top-right corner; focus is the deep-aqua outline 4px out. A hairline-ruled foot holds the chosen name (or "Nothing chosen yet") in caption ink-2, Cancel (quiet) and Choose (primary, disabled until something is chosen). Double-clicking an item chooses it. Opening it moves focus into the sheet without scrolling the page behind.
- **Place field:** how staff choose what something opens. With nothing chosen it is a quiet button with a map-pin icon ("Choose a place in the app"). The chosen place is a token: a fill-on-surface box at the 12px radius, at least 56px tall, holding the row's 34px tile (sand for pages, accent for things in them), the name at row size and 600 over where it is (or its date) in caption ink-2, both cut off with an ellipsis, then a small quiet Change and a small quiet icon-only clear ("Open nothing"). A place that can no longer be opened takes the warm tile (marker at 30%, marker-ink alert icon) and says why. Change opens a wide sheet carrying the Places directory, listing only what the audience can open.

### Navigation
- **Top bar:** 48px plus the top safe area, three-column (back, centred title, actions). Transparent over the masthead with white icons; once the page scrolls past the masthead (a per-page threshold, 120px by default and 44px on Home's compact masthead) it turns solid navy and the title fades up into view. Detail pages with a hero use it solid from the start. The back button carries the parent's name, and is spoken as "Back to" that name, or just "Back" when that is its label; a page can give it its own action instead of a parent (the event editor's "Cancel"). A word action at the end ("Edit") is on-masthead text at body size and 600, 44px tall.
- **Tab bar:** fixed, up to five equal items: up to four chosen by staff (Home and Sermons until they publish a change), then More, always last; each a 24px icon over an 11px 600 label in ink-3; the active tab is deep aqua. Tapping the active tab scrolls to the top. While audio plays, three small orange bars animate beside the Listen icon on every screen. Frosted page colour with a hairline top edge.
- **Notifications bell:** a 44px icon button in the top bar of every tab root, with an 18px orange count badge ringed in masthead navy; it opens the inbox, a pushed page whose top bar carries a sliders icon button to Notification settings.

### Masthead (signature)
The cover of every tab root: navy ground, the mark at 7% opacity, 210px wide, bleeding off the right; the display title (balanced, up to 13 characters a line); a note line in beige at 600 (verse and date, a date range, a one-line purpose); then an optional slot for the screen's live element. The masthead sits under the transparent top bar, padded down by the safe area plus 56px.
- **Status strip:** inside the Sunday masthead, a 12px-radius strip of translucent white (12%) with a clock icon or a pulsing aqua live dot, the next service or "Live now", and a ghost or live button.
- **Now playing:** inside the Listen masthead, 136px 16:9 artwork beside the title. With nothing playing it shows the latest sermon with Listen and Watch buttons; once audio is playing it pins that sermon instead, with a 4px orange progress line and a transport row (elapsed, 15s back, a 56px white play/pause disc, 30s forward, remaining).
- **Beside:** a slot beside the title; the title and it share a row that wraps when tight (10px gap, bottom-aligned), the slot nudged 4px up to sit on the title's baseline. The calendar's Week and Month switch sits there.
- **Week rail:** in the calendar's sticky week band, seven equal day buttons 4px apart, each at least 44px at the 12px radius in on-masthead: an uppercase micro weekday (600, +0.04em, at 75%), the date at lead size and 700 (tabular), and a 5px beige dot when something is on. Days before today fade to 60%; the day in view takes the masthead chip; today is a white (surface) tile with primary text and an orange dot. Pressing fills with the masthead chip; focus is a 2px marker outline 2px out. Tapping a day scrolls the agenda to it.

### Section tabs (signature)
A sticky, horizontally scrolling row of 600 labels in ink-3 (18px apart) that pins directly under the top bar on sectioned pages. As you scroll, the tab for the section in view turns ink and a 3px orange underline slides and resizes to sit under it (240ms ease-out); the row scrolls itself to keep the active tab visible. Tapping a tab smooth-scrolls to its section with a selection haptic. All of Sunday's five tabs (Welcome, Sermons, This week, Next, Give) fit a 390pt width.

### Sermon outline and player
- **Outline:** point headings at 700, items in ink-2 prose with a 6px orange dot bullet, passage chips beneath; a "Main text" line leads with its chip.
- **Player:** a sand panel (14px radius) with a 48px navy round play toggle, label and tabular position, 15s back and 30s forward skip buttons, and an orange-accented scrubber.

### Sheet
A bottom sheet over a 38% black scrim: page-coloured, top corners 22px, a grabber, a title-large heading and ink-2 lead, rising 320ms with the ease-out curve. Used for My notes, the first-launch topics welcome, the notifications welcome, confirmations and What's new. On wide screens (720px and up) the sheet is centred (560px), and one without a cover hides its grabber. Sections inside a sheet drop the page gutter and their top margin, and take the sheet's.
- **Wide:** for a sheet that carries a whole list to choose from (the place picker): 760px on wide screens, with 28px side padding.
- **Cover:** an optional navy band across the top of the sheet, edge to edge, in the masthead's colours. The grabber sits on it in the masthead chip colour, and the mark bleeds off the right at 7% (180px wide). The cover says what the sheet is about, so a covered sheet is usually named by its label rather than a title.
- **Paged sheet:** steps through a few items in one sheet, each with its own cover and body. Under the body, orange page dots mark where you are: 8px, the current one widening to a 20px capsule, each in a 36 by 44px target. Swipe left or right on the cover or the body to turn the page. The actions are an optional per-page action (primary) over Next, which reads Done on the last page; with no action, Next is the primary. Only a page change slides: the next page comes in 14px from the right, the previous from the left (260ms). Opening shows only the sheet's rise. One item shows no dots.
- **Sheet heading:** a covered sheet's head: a 44px icon tile on the masthead chip (12px radius) beside the title in headline type at 700 (balanced), with an optional beige note line at label size and 600 under the title, aligned with it. What's new and the notifications welcome both use it.
- **What's new:** the first paged sheet, shown once after an update. Its cover is the sheet heading: the highlight's icon and title, with the note line ("New in 1.1"). The body is the summary at lead size, an optional 16:9 picture and the icon fact list. More → What's new lists every version as a section of reading rows, highlights on the warm tile. The model is in [docs/architecture/changelog.md](docs/architecture/changelog.md).
- **Notifications welcome:** one sheet that asks before iOS does, shown once on the first open while the phone's notification permission is undecided; it comes before What's new, and only one opening sheet shows per open. Step one: a bell sheet heading, "Hear from the church" with the note "Notifications", one lead-size line saying what the church sends, then Turn on notifications (primary, block) over Not now (quiet). Turn on raises the system dialog; on Allow the same sheet becomes step two, "Notifications are on" with the note "Change these any time in More", over the notification settings' toggle rows and Done. Step two's cover and body slide in like a paged sheet's next page (14px, 260ms); there are no page dots. Not now, or closing on step one, is remembered and the sheet doesn't come back.

### Empty state
Centred: a 40px deep-aqua icon, a 1.0588rem ink title, an ink-2 line, and an optional quiet button.
- **A link that can't open (app):** a link into the app that leads nowhere it can show is a pushed page back to Home, with a masthead that names the state ("Not available", "Not found") over an empty state that says why and what to do. Signed out and the place is for signed-in people: a primary Sign in. Not for this person, taken down or unknown: a quiet Go home. A file that opened in the phone's browser: "Opened", with a quiet "Open it again".

### Calendar (signature)
The church calendar as one running diary from today. The ordinary stays quiet and only what's different speaks.
- **Week:** the sticky week band (see Layout) with the range in beige at label size and 600 (tabular), 12px above the week rail. Under it the toggle chips of your calendars, then a day section per day: a heading at row size and 700 ("Today", "Wednesday") followed by the date in caption ink-3 at 400, then its agenda rows. The list ends on a quiet "Carry on past …" button, or, at the end of what the phone holds, a hint saying that is everything.
- **Month grid:** micro weekdays (600, +0.04em, ink-3), then day buttons at least 46px at the 12px radius: the date at row size and 600 (tabular), and under it up to three marks: pale (strong sand) for a regular event, accent for a special, marker for a change, and an open 1.2px ink-3 ring for a date that isn't on. Days before today in ink-3; days outside the month at 45%; hover fills sand. Today is filled primary with on-primary (marks in on-primary, regular at 50%); the chosen day is an inset 2px primary ring. A caption key in ink-3 lists the four marks.
- **Event head:** a navy band like the masthead, the mark at 7% bleeding off its top-right, holding the date block beside the words (16px apart): the label line (pill size, 700, beige), the title (headline, 700, -0.02em, balanced) and the when line (beige, label size, 600, tabular: "8:30–11:30am · every second Saturday"). The label line carries the event's own label or its calendar's name, never words the page invents. Compact in the admin panel's panels: 16px padding, a 12px gap, the title at title size.
- **Date block:** a white (surface) tile, 78px wide at the 14px radius, the weekday, the date numeral and the month stacked in primary (see Date numeral). Flat: no shadow. In dark it is a raised navy tile with aqua numerals (see Colors). It is a picture of the date, spoken as the full date.
- **Event body:** 16px under the head, items 14px apart: an optional 16:9 picture edge to edge with square corners above the body; the actions wrapping 10px apart (Add to calendar as the primary with its icon, Register when there is a link and Share quiet); a change note in marker at 22% at the 12px radius (11 by 14px, caption in ink, "Changed" leading in orange ink at 700); a date that isn't on as a sand note at the 12px radius with its icon in ink-2 at 600; then the fact list with icons (where, opening Maps; contacts, calling or messaging), About in prose, and Other dates as rows with the time trailing in ink-3 (a date not on in ink-3).
- **Edit where you read it:** Edit turns the event page into its editor without leaving it. The top bar reads Cancel and "Editing"; the head keeps its place and its date block, title and when line take dashed outlines (see Shapes) and open small sheets. Below, fields at their 12px radius, add-rows (a full-width 44px line under a hairline, a plus and deep-aqua label at 600, ink on hover), contacts as rows (the sand row tile, name and number, a pencil) that open into a sand box at the 12px radius with surface fields; then the foot bar.
- **Foot bar:** sticky at the foot, docked on the tab bar: frosted bar colour (18px blur) with a hairline above, 12px by the gutter, 10px between its lines: a segmented "This date only | Every …" for a regular event, then Save (primary, block) beside Not on this date (quiet), and a small danger text link to delete. In the admin panel it is the same bar on the panel's surface (no blur), sticky to the panel's bottom with 14 by 16px padding.
- **Widening sentence:** when an edit would reach more dates than the one in view, a caption at 600 in orange ink with a leading icon says so, 6px under the field; when it needs an answer it becomes a box in marker at 18% at the 12px radius (11 by 14px) with its choices under the words.
- **Calendar workbench (admin):** the sand column opens on "Calendar" at title-large size, then items (All your calendars, Needs details, Asked to publish) as 40px buttons at the 10px radius, label size at 600 in ink, the current one on the segment colour (white in light), hover at 60% of it, a count in a 20px orange capsule (pill type, on-marker, tabular) only above zero. Below a hairline, the calendars as 44px rows: the name at label size and 600 with an optional pill-size ink-3 line ("Needs a publisher"), a 34px settings icon button in ink-3 (primary while its panel is open) and a small switch (36 by 22px). The chosen calendar row takes the segment colour. "+ New calendar" follows in deep aqua at label size; Breaks sits in a foot group under a hairline with a quiet tabular count in ink-3. Day heads in the middle pane are label size at 700 with the date in ink-3 at 400.

**The Quiet Regular Rule.** In the calendar the ordinary stays quiet and only what's different speaks: a regular event is ink-2 at 400 with its time in ink-3 and no label; a special or one-off carries its label in accent and its time in primary; a change is marked in orange until it has been opened; a date that isn't on keeps its place in ink-3 with the circle-off line under its title, never a suffix and never dropped from the list.

### Admin workbench and drafts
- **Workbench:** three panes in one view: a sand list column (280 to 340px, hairline right edge), a white work area (24 by 32px padding), and a detail pane (320 to 512px) whose content is a lifted white settings panel at the 14px radius. Under 1200px the detail drops beneath the work area; under 760px, or whenever it is asked to be narrow (the app's form), it becomes one column that shows a single pane at a time.
- **Tab strip:** a wrapping row of label-weight tabs in ink-3, 22px apart and 44px tall, over a hairline; the selected tab turns ink with the 3px orange underline. A tab can carry a count in an orange capsule (tabular figures), and a trailing slot holds an add action in deep aqua. It is the static sibling of the section tabs: it switches what is shown, it does not scroll the page.
- **Save status:** one caption line at 600 that is polite to screen readers: a small spinner and "Saving…", a deep-aqua check and "Saved", or, on failure, a cloud-off icon and "Couldn't save. Trying again by itself." in orange ink with an underlined "Try now". An optional detail follows in ink-3 after a middle dot.
- **Selected row:** see the Tonal Selection Rule; the same treatment marks the chosen draft in the list and the chosen row in the work area. In the workbench's sand list column a list-style table hides its header and rounds each cell to the field radius, so the selected row is a white segment tile.
- **Admin overrides:** in the admin panel the workbench fills the window height, narrows its outer panes to 260 to 300px and 320 to 400px at 1648px, and drops the detail under the work area (24 by 32px padding, a hairline above) at 1428px.

### Admin sidebar (signature)
The admin panel's masthead, turned on its side. A 248px navy column, 22px between its parts: the mark in beige at 30px beside "Good Shepherd" at 700, then the navigation in groups (Content, People, The app, and You always last at the foot, set off by a 1px rule of the masthead tint and pushed to the bottom). Each group has an uppercase micro label in beige. Links are 38px, label type at 600 with a small icon, in white at 76%; hover fills them with the masthead tint at 60%, and the current link takes the full tint and white text while a 3px orange bar, rounded on its outer end, scales in from its centre at the sidebar's left edge (240ms, ease-out). A count of things waiting sits at the end of a link in an orange capsule (20px, pill type, tabular, on-marker text), only when the count is above zero, and refreshes as you move between pages. Focus rings in the sidebar are orange.

### Title band
The page head in the admin panel: a headline-size title (balanced, focus target for navigation), an optional caption lead in ink-2 up to 72ch, or a lead slot, an optional back link in deep aqua above the title, and actions at the end. It also sets the browser title as "Page · Good Shepherd admin".

### Office week (signature)
- **Day rail:** one shared rail, a week on the home page and a fortnight on Announcements. A sand track (14px radius, 10px padding, 8px column gap, at least 196px tall) of white day columns at the 10px radius that run the rail's full height. Each column carries an uppercase micro weekday in ink-3 ("Today" on today, in orange ink) over a tabular title-large date. Today is outlined with an inset 2px primary ring; days before today keep their column but fade their contents to 55%.
- **Fortnight:** fourteen days from this week's Monday, each at least 56px wide with a 6px gap and 8 by 8px padding, the rail as tall as its contents and scrolling sideways when the days don't fit; its capsules are tighter (5 by 7px) and set entirely in pill type. A span covering the whole rail reads "The whole fortnight" instead of "All week".
- **Capsules:** each dated item is a capsule (9px radius, 6 by 10px padding, caption) laid across the days it covers: a 600 title cut off with an ellipsis over a tabular pill-size date range at 80%. Capsules pack into bands: each takes the highest band where it doesn't touch another, so items on different days share a line. Three kinds: **Everyone** (what everyone sees) in a primary fill with on-primary text; **Event layer** in accent at 16% on the surface with accent text; **Draft** (not out yet) in marker at 32% on the surface with marker-ink text. A capsule presses to 0.98; the chosen one has a 2px marker outline at a 2px offset.
- **Moments:** an item at one time rather than across days (an announcement's send time) reserves its own day and the next two (fewer at the rail's end), so its title has room for two lines; another item that close moves down a band. Its second line is the time alone ("7am"); the full day and time are in its tooltip.
- **Legend and quiet state:** a caption legend in ink-3 with 14 by 10px swatches appears only when something is dated, and lists only the kinds on the rail, each in the page's own words (the home page's Draft reads "Not out yet: a draft or a scheduled announcement"). With nothing dated, one centred caption line in ink-3 sits across the rail.
- **Needs you:** hairline rows with a marker-tinted tile (orange at 30%, orange-ink pencil), a 600 title link, a caption detail with a relative date, and a small quiet button for the action. With nothing waiting, a single hairline-ruled line with a deep-aqua check.
- **Your areas:** hairline rows linking to each area: a sand tile with a primary icon, a 600 title, a one-line caption purpose in ink-2, and an ink-3 chevron; hover fills the row with sand at 60%.

### Announcements desk
The page where staff write, check and send an announcement, set as the fortnight it will land in.
- **Fortnight rail:** this week and next, with sent announcements as primary capsules and scheduled ones as marker capsules, each a moment at its send time. While a time is being chosen, the announcement being written shows as a live "this one" capsule at that time, outlined as the chosen one.
- **Not sent yet:** a row of 34px marker capsules (marker at 32% on the surface, marker-ink, caption) after a 600 caption label in ink-2: each a 600 title cut off at 28ch beside its state in pill type. The one open on the desk has the 2px marker outline; they press to 0.98.
- **Writing column:** a title-size heading with Save status beside it, then the title and message fields, then a checklist: hairline rows (14px above and below) with a 120px label column at label size and 600 in ink-2, the field's own label hidden. The rows are Who it's for (with how many phones it would reach as its hint), When (a Now and Later segmented control; Later adds a day field beside a list of times written the church's way, up to 160px, and a caption line saying when it goes), Opens, and Urgent (a toggle). Actions close the column: a small Danger Delete on the left, then Send to my phone (quiet) and a primary button that names its reach ("Send to 42 phones", or "Schedule it").
- **Proof aside:** the lock-screen proof, captioned with what tapping opens, then "In the app's notifications" (label size, 600, ink-2) over the unread row it will become, its message clamped to two lines.
- **Confirm sheet:** sending opens a sheet with the lock-screen proof and a hairline summary (a 110px label column: Who it's for, When, and Urgent when it is), a warning notice when no phone would get it, and Back to writing beside the primary send.
- **Sent earlier:** a data table of what went out before this week, newest first: when it went, how many phones it reached and how many have read it.
- **Sent or scheduled:** the writing column becomes the state line and the message in prose (65ch); a scheduled one offers a quiet "Don't send it: back to writing".

### Data table
The admin's working list. A toolbar row (search, filter chips) wraps above a full-width table at row-title size. Headers are caption-size 600 in ink-3 over a hairline; sortable headers are buttons whose arrow is hidden until hover and shows in orange ink on the sorted column (fading 160ms). Rows are divided by hairlines with 12px cells; numbers and dates align right in tabular figures, secondary cells are caption in ink-2, long text is cut off at 30ch. A selectable row shows a pointer, fills with selected-fill at 55% on hover and the full selected-fill when chosen; focus is an inset 2px deep-aqua ring, and the arrow keys move the selection. An empty table shows an empty state beneath it.

### Charts
- **Stat tile:** a hairline above, the value in display size at 700, a row-size 600 label in ink, then an optional caption change in ink-2 and explanation in ink-3. A value too small to show (Fewer than 5) is the quiet tile: title size in ink-2.
- **Ranked list:** a row-size title at 700 and a caption explanation in ink-3, over hairline rows (at least 40px, caption size): the rank in ink-3 tabular figures, the label in ink, the value in ink-2 tabular figures at the end. Unnumbered, it drops the rank, for counts that aren't a ranking (Insights' "How phones are set up").
- **Trend line:** the main series is a 2px deep-aqua line ending in a labelled dot, the comparison series a 2px dashed ink-3 line (its legend swatch dashed too), over hairline gridlines with pill-size tabular ticks, the plot capped at 760px. A hover crosshair shows a lifted tooltip. Columns are not focusable: the table of every number under the chart, behind a deep-aqua 600 summary, is the accessible view. With fewer than two points the chart is a sand panel at the 14px radius with one caption line in ink-2.

### Split view
The admin's table-and-detail layout. The detail is a hairline-ruled column, not a card: 28px from a 1px line on its left, flat on the white page. When it drops beneath the list, the line moves above it. Without a selection the list takes the full width.

### Places in the app
The app laid out on the desk: every place in the app, grouped by area, beside a phone showing the chosen one. Reached from the sidebar's The app group (map-pin).
- **Directory:** the shared search field and filter chips (All and each area) wrapping above the list, then a group per area: an uppercase micro head in ink-3 at 600 (+0.06em) over hairline rows, each with its icon on a sand tile for pages or an accent tile for things in them (events, files), the name, where it is ("More → What's new") or its date, and who can open it as trailing text. The chosen row takes the Tonal Selection. A group cut short ends in a deep-aqua 600 "Show all …" at label size with a chevron (44px target). Nothing matching is an empty state. The same directory is the place picker in a wide sheet.
- **Phone column:** centred under the phone: the name in title type (balanced), where it is in caption ink-2, then who can open it as a 600 caption line in ink-2 with a people icon. Then the actions, centred and wrapping 8px apart: the place's own change action as a small primary ("Change it in a draft"), the copy button, then small quiet actions ("Announce it"). Under them the link itself (host and path, caption ink-2, tabular, breaking anywhere) and one note in ink-3 caption up to 40ch on what the link does on a phone. A place that can't be shown is an empty state in the column.
- **The phone:** a feature can register a live view of its place (Home draws through HomeScreen); any other place is the phone frame with the app's masthead carrying the place's name and where it is as its note.

### Breadcrumbs
Where you are inside something nested, as a row of crumbs (a navigation landmark named "Where you are"). Crumbs sit 2px apart with a small chevron in ink-3 between them; each is 34px tall, 8px at the sides, at the 9px radius, row size at 600, with an optional small leading icon (an image for the Library, a folder, a lock for what the app owns). Earlier crumbs are deep-aqua buttons that fill with sand on hover and take the 2px deep-aqua outline on focus; the last is the current place in ink. A crumb can take a drop: only the crumb under the drag lights, with a 12% deep-aqua tint and a 2px inset deep-aqua ring. Placed at the start of a row, the crumbs pull 8px left so their text lines up with the content below.

### Selection tray
A navy bar that docks at the foot of the work area while several things are chosen, carrying what can be done to all of them. It is sticky 20px from the bottom of the window, at least 58px tall, padded 8px 10px 8px 22px at the 14px radius, in primary with on-primary text at 600 and the Lift shadow, 24px below the list. It reads the count with tabular figures ("3 files chosen", spoken politely), then Clear (quiet, on-primary at 14%) and the actions; a primary action inside it turns white with ink text. It rises 10px from 40% over 220ms (not with reduced motion). Media shows it only for two or more files: one chosen file is the split view's detail.

### Media library
Staff's library as two shelves: the Library, which staff organise in folders, and Made by the app, which the app fills. Reached from the sidebar's Content group.
- **Shelf switch:** a segmented control sized to its labels (14px at the sides): Library (an image icon) and Made by the app (a lock). The type filter chips (All, Pictures, PDFs and files, Not used, each with a count) and a Grid/List segmented control share its row.
- **Path row:** breadcrumbs from the shelf down to the open folder, each folder crumb a drop target. An open staff folder puts its actions at the row's end: small quiet Rename… and Move to…, and a small danger Delete folder…. Searching replaces the row with "Results for “…” in every folder" at 600 and a small quiet Clear search.
- **Folder tiles:** a white tile at the 12px field radius with a hairline border, at least 64px tall, 10 by 14px padding: a 38px sand icon tile at 9px with the folder in primary, then the name at 600 (one line, ellipsis) over a caption count in ink-2 ("3 files · 1 folder", "Empty", or "Shared with others" for one the viewer can't open). Hover drops the border and fills the tile with sand, its icon tile turning white. An app folder shows a lock in ink-2. A tile under a drag takes a deep-aqua border, a 1px inset deep-aqua ring and a 10% deep-aqua tint; what is being dragged fades to 45%. A folder holding only folders says so in a caption under the tiles ("No files here, only the folders above…") rather than calling itself empty.
- **Picture grid:** each file a 4:3 thumbnail on strong sand at the field radius over its name in caption at 600 and how it's used in caption ink-3, one line each. Hover rings the thumbnail 2px in marker at 55%; a chosen file is ringed 3px in marker. A 28px round tick sits in the top-left corner (ink at 40% inside a 2px page-coloured border), shown on hover, on focus, when chosen (a marker disc with an on-marker tick) and on every card while two or more are chosen. Focus is the deep-aqua outline 4px out. Files drag onto folder tiles and crumbs. The List view is the data table, a 64px 16:10 thumbnail leading each row.
- **Detail:** one chosen file opens in the split view's detail; its Folder fact is a deep-aqua 600 link that opens that folder, after a lock when the app owns it, and Move to… leads its actions.
- **Move notice:** after a move, an info notice above the grid says what moved, where to, and that nothing broke: "3 files moved to Sermons › 2026 series. Everything that uses them still shows them." A failed move is a warning notice in the same place.
- **Already in the library:** choosing a file the library already has replaces the upload form with a sand box at the card radius, 16px in: "Already in the library" at body size 700, a caption ink-2 lead (up to 60ch) saying the file is exactly the same and one copy is easier to look after, then the existing file as a row (an 88px 16:10 thumbnail, its name at 600 with an ellipsis, "In <folder> · Used in N places" in caption). Use the existing one is the only primary (navy, a check icon); Add it again anyway is quiet on the surface colour (white in light) so it stands off the sand. The box takes focus when it appears, without a ring, scrolling into view (smooth unless reduced motion) and announcing its heading and lead. Using the existing one opens that file in its folder, chosen, under an info notice: "“Romans series.jpg” was already in the library, so nothing new was added. Here it is." Adding it again brings back the form with Add a second copy.
- **Folder picker (Move to…):** a sheet titled for what is moving ("Move 3 files", "Move “Sermons”") with the lead "Things that use them keep working: moving only changes where you find them here." Under it, a list of folders (up to 50vh or 420px, scrolling on its own): the Library with an image icon, then every staff folder with a small folder icon, indented 22px per level. Rows are 42px at 9px with the name at 600 (ellipsis); the folder something is in now says "Here now" in caption ink-3. Hover fills Selected Fill at 55%; the chosen row takes Selected Fill (the Tonal Selection Rule); a folder it can't go into (itself or one inside it) is ink-3 and can't be chosen. It starts with nothing chosen, and Move here (primary, a folder-input icon) stays disabled until a different folder is chosen, beside Cancel (quiet). Deleting a folder with things in it uses the same sheet ("Delete “Sermons”?", Move them and delete); an empty one is a plain confirm with a danger Delete folder.
- **New folder, Rename:** a sheet with one Name field (up to 80 characters) and Make folder or Rename.
- **Made by the app:** see the App's Own Rule. Its crumbs and folder tiles carry the lock; under the path row a caption note in ink-2 (up to 72ch) leads with a small lock and says what the folder holds and that its files stay where the app put them. With nothing in it, the shelf is a lock empty state ("Nothing made by the app yet").

**The App's Own Rule.** Wherever the app owns something rather than staff (the Made by the app shelf, its folders and crumbs, a file's folder, the shelf's empty state), it carries the lock glyph and one plain caption saying what it is and what staff can't do to it. The lock is ink-2 or the surrounding text colour; ownership takes no colour or badge of its own.

### Sign-in and gates
- **Sign-in card:** a white card at the 14px radius, lifted, up to 460px wide (28px padding, 20px at the foot), centred on a full-window sand stage, with the mark and name in primary at 700 at its head; fields and quiet buttons inside it use fill-on-surface. A small quiet "App reviewer? Sign in with a code" switches it to the code form.
- **Open in the app:** a shared link opened without the app is the sign-in card on the sand stage, filling the window: the brand head, then the place as its row tile beside its name in title-large type (balanced) and where it is in caption ink-2, then two short ink-2 paragraphs (up to 52ch) on what the link does and how to get the app. A place for signed-in people is named only "A place in the church app"; a place that's gone says "This isn't in the app any more". Signed-in staff also get a quiet "See it in Places in the app".
- **Gate:** when a page needs signing in or a permission the person lacks, the title band stays and an empty state sits in a sand box at the 14px radius beneath it. A page that doesn't exist and a page that fails show the empty state alone, with a quiet button home or to try again.

### Previews
- **Preview banner:** an orange marker strip docked just above the tab bar on every screen while a preview is on: an eye icon, the bold name of what is shown with an optional caption line, an underlined Hide, and a dark capsule Exit in on-marker colour. The page reserves room for it at its foot.
- **Phone frame:** a phone at its true point size (each supported iPhone's width, height, safe areas and cutout), with a status bar (9:41, signal and battery), the Dynamic Island or notch, and the tab bar; the frame lifts and is scaled to fit the space it is given: the width of its holder and the window height below it, or the holder's own height when the holder sets one (as the Places phone column does). It can carry its own palette, so a preview shows another palette without changing the page around it. Rows and tabs inside it do not respond to taps.
- **Lock-screen proof:** what a notification will look like on a locked phone. A masthead-coloured panel at the 14px radius with the date in caption at 600 (on-masthead at 76%) over a 9:41 clock, then a page-coloured notification at the sheet's 22px radius: a row with a 20px masthead app icon, the app name in uppercase pill type, "Time sensitive" in marker ink when it is urgent, and "now" in ink-3, then the title at row size and 600 and the message clamped to four lines. Empty fields show placeholder words in ink-3. An optional caption sits under the panel.
- **Home screen and surface views:** the Home masthead and its Hub tiles are drawn by one component, and each surface registers one view; the app's Home and the admin preview both draw through them, so a preview is the real screen, not a copy.

### Home tiles and their layouts (signature)
Home is a grid of tiles in theme tones (docs/features/home.md). A tile is never refused for its size: each kind
declares **layouts** (`TileVariant`: a key, a label, the sizes it suits, when it's the default), and the tile
draws the layout chosen in its settings, or **Best for this size**, which picks the default for its size and
picture. A layout that doesn't suit the size, or needs a picture the tile hasn't got (`TileVariant.NeedsPicture`),
falls back to the default. Sizes are read against the grid (a 3-wide tile on 2 columns is
2 × 1). Every kind brings its own catalogue the same way; add new layouts to this table as they come up
(owner, 2026-11-04: "options as they come up").

The frame draws six shapes (`TileShape`): **square** (icon top, words at the foot), **row** (words, icon at the
end), **poster** (tall: icon top, a larger title, a go line; the mark bleeds off dark tones), **picture**
(picture or cover above the words), **behind** (picture fills the tile, words over a masthead-deep shade, text in
`on-masthead`) and **beside** (a square picture at the start, needs two columns). The picture layouts all need a
picture.

**Picture layouts on every kind** (owner, 2026-11-04, note 22): each kind that brings its own catalogue also offers
the three picture layouts (`TileVariants.Pictures`), so any tile can carry a picture "just in case". In a picture
layout a kind's live content shrinks to a title and one line (This week: the next thing and when; Notifications: the
count and the newest title; the bulletin: the theme with the date and verse) so it stays readable over the shade,
plus its go line from two cells up. **Best for this size** keeps a kind's designed layout until a picture is
chosen, then picks the picture layout for the size, as a Link tile does. A kind none of whose layouts show a
picture hides the Picture field (`HubTileKind.UsesPicture`); today every kind uses one.

| Kind | Layout | Suits | Draws | Default |
|---|---|---|---|---|
| Link tile (Give, Bible, Prayer, Life Groups, Ministries, Visit us, campaigns) | Words and icon | any | square / row / poster by size; no picture | without a picture |
| | Picture behind | any, with a picture | behind | 1 × 1 with a picture |
| | Picture above | 2 rows or more, with a picture | picture | two rows or more, with a picture |
| | Picture beside | 2 columns or more, with a picture | beside | one row, wider, with a picture |
| Latest sermon | Cover first | any | 1 × 1: cover and play only; one row: title beside the cover; taller: cover above the title, 2 × 2 adds the length; the tile's picture replaces the cover | always |
| | Where you're up to | any | play, title and how long (progress once Sermons plays), no cover | never |
| | Picture behind | more than one cell, with a picture | behind: title, date and play over the shade | never |
| Sunday bulletin | Poster | any | 1 × 1: "Sunday 8 Nov" and the open words; one row: theme with date · verse; taller: the bulletin cover | always |
| | Theme first | any | the year's theme as the title at every size | never |
| | Verse | more than one cell | the week's verse in large type, the reference small | never |
| | Picture behind / above / beside | as for Link tiles | the theme, the date and verse, "Read the bulletin" from two cells | with a picture, as for Link tiles |
| This week (Coming up: one-off events, specials and dates not on) | Agenda | any | 1 × 1: the next thing as the title over when ("Tomorrow 7pm", "Not on Saturday"); one row: up to three; taller: as many as fit, up to *How many to show*; with nothing coming, "Nothing special coming up" | always |
| | Just a link | any | icon, title and "3 things coming up" | never |
| | Picture behind / above / beside | as for Link tiles | the title, the next thing and when, "Calendar" from two cells | with a picture, as for Link tiles |
| Notifications | Count and newest | any | bell, "3 unread", the newest title | 1 × 1 |
| | Newest in full | any | the newest notice and its first lines, the count above | one row, wider |
| | Unread dots | any | the newest titles each with an unread dot; taller shows more | two rows or more |
| | Picture behind / above / beside | as for Link tiles | the count with an unread dot and the newest title (or the staff's words when nothing is unread) | with a picture, as for Link tiles |
| | (nothing unread) | | the staff's words, any layout | |
| Outreach day | Invitation | any | 1 × 1: icon and title; one row: a row with the line; taller: a poster with "Find a route" | always |
| | Just a link | any | icon and title | never |
| | Picture behind / above / beside | as for Link tiles | the invitation and its line, "Find a route" when taller | with a picture, as for Link tiles |

In the editor a layout that doesn't suit the tile stays listed but greyed, and its caption says why instead of
what it draws: "Needs a wider tile", "Needs a taller tile", "Needs a bigger tile", or "Needs a picture: choose
one under Picture". The automatic choice reads "Best for this size (now Picture beside)", naming what it picks.

Recorded on the decision pages but not built: This week as a day strip, the sermon series card, Outreach's map
card (needs real routes). The pages are in `.impeccable/mocks/decision/hub-variants/<kind>/`.

### Home masthead
Home's masthead is the compact one: the safe area plus 40px above (not 56px), 18px below, the title "Good Shepherd" kept on one line at the display size or 10.5% of the screen width, whichever is smaller, and under it one line, 8px down, holding today's date as the beige note beside the sync indicator on the masthead. Home has no section tabs. The top bar is clear over the masthead, so the status bar sits on navy, and turns solid with the title after 44px of scroll; the admin canvas's phone does the same at 44px.

### Hub tiles
- **Tones:** eight theme role pairs, never a hex, so every tone follows the scheme and the yearly palette. Staff see plain names; the token stays the same: **Soft** (`sand`: sand with ink, the default), **Card** (`raised`: the surface with ink, lifted with `--tile-lift`: in light a soft ambient halo on every side plus the lift below, never a drawn border, so a white card reads on the white page; in dark the usual lift; with a picture, only the lift), **Top of Home** (`masthead`: masthead with on-masthead text and the beige note for lines), **Buttons** (`primary`: primary with on-primary), **Pale accent** (`live-soft`: live at 13% on the page, ink, deep-aqua go line), **Accent** (`live`: live with on-live), **Pale highlight** (`marker-soft`: marker at 24% on the page, ink, marker-ink go line) and **Highlight** (`marker`: marker with on-marker). On the four solid tones the secondary line is the text colour at 82 to 84% over the ground, and icons and agenda rules switch to the text colour at 14 to 18%. On the two pale tones the secondary line is the tile's ink at 78% over the ground, never ink-2.
- **Pale highlight in dark:** in dark it mixes 40% marker into on-marker (`--tile-soft-mix`, `--tile-warm-base`), its secondary line is the ink at 92% (`--tile-soft-ink`) and its go line is ink (`--tile-warm-go`). Pale accent keeps its light-scheme mix and 78% line in dark.
- **Frame:** 14px radius, 14 by 14 by 16 by 16px padding (16px all round on poster and row), the title at lead size and 700 (-0.01em, balanced, breaking only between words, never hyphenated), up to two caption lines in the secondary ink (three on a poster, at 600), then a 600 label-size go line with a chevron in the tone's go colour. A tile can carry a live dot or a 9px orange unread dot beside its title.
- **Corner mark on picture tiles** (owner, 2026-11-04, note 21): a tile with a picture behind or above shows the same › (opens in the app) or ↗ (a web page) as a square tile, in a 28px round chip in its top-right corner: `on-masthead` over `masthead-deep` at 58% with an 8px backdrop blur and a hairline ring of on-masthead at 14%, so it reads on pale and busy pictures alike (24px, 8px in, on the smallest tiles). A picture beside the words shows the plain mark at the words' top-right, as square tiles do. A tile with its own go line or a play button gets no mark.
- **Fitting the words:** each tile is its own size container. At 112px wide or less the lines and go icon drop, the title goes to label size and the padding to 12px. At 200px or less the play disc moves to the top-right corner (12px in, 40px across) so the words get the full width, the trailing icon is hidden on picture and beside tiles, an agenda's time stacks above its event with the day inline, a verse title drops to row size, and unread-dot titles may take two lines. A poster 240px wide or more carries a 52px icon at the field radius (26px glyph).
- **Icon:** the 34px row tile on the text colour at 7% (14% on solid tones). Its colour, for staff: Goes with the tile, Buttons, Accent, Highlight, Grey, or Its own colours (offered only for an uploaded drawing).
- **Mark:** a solid-toned poster two or more rows tall carries the church mark at 7%, 168px, bleeding off its bottom-right corner, like the masthead.
- **Picture:** above the words filling the space the cell leaves them, beside them as a square up to 132px or 40% of the tile (at least 88px tall), or behind them under a shade from masthead-deep at 45% to 90% by 70% of the height, with on-masthead text. With no picture, a sermon cover is the masthead with the mark at 20%.
- **Press and focus:** the whole tile is one target; it scales to 0.97 over 160ms on press, and focus is the 2px deep-aqua outline at 2px offset. A tile's own action (the 48px primary play disc) sits 16px from its bottom-right corner above the target; over a picture or cover it carries a faint 2px ring of on-masthead at 34%.
- **Agenda inside a tile:** hairline-ruled entries, a 5.4rem tabular time column at row size and 700 in primary (the text colour on solid tones), the day under it in caption. A date that isn't on reads "Not on" in the time column, its title at 400 in the tile's secondary ink.
- **Unread dots inside a tile:** the newest titles at row size and 600, 8px apart, each one line with an ellipsis after its 9px dot.

### Hub canvas (signature)
Home arranged on the desk, inside the drafts workbench: the work area is the phone, and the settings panel beside it edits the chosen tile.
- **Controls rail:** a column 196 to 240px wide beside the phone, 16px between controls: Add a tile as a full-width primary button, then Columns as a segmented control, "Show Home for" (whose Home it is) as a 38px sand select at the field radius, Preview as a Light and Dark segmented control, and "Preview as on" as a date field in the same style, set to today; once another day is chosen a "Back to today" link sits under it. Each control has a caption label at 600 in ink-2. Hidden tiles stay behind a 36px sand capsule toggle in ink-2, then list as rows.
- **Stage:** the phone frame alone, scaled with a transform (never CSS zoom, whose boxes WebKit misreports) to fit the height left under the draft head, never below 0.6; the caption saying how to move and resize with the mouse and the keyboard sits at the foot of the controls rail. The draft head and tabs stay put; only the area under them scrolls, and the page itself never does. Nothing on the stage can be text-selected.
- **Draft head in canvas mode:** the draft's title at title-large size, clamped to two lines and breaking at words, and the area's lead line hidden, so the head stays one line tall beside its actions.
- **On a tile:** hovering draws an inset 2px marker ring at 60%; the chosen tile is outlined by a 2px marker ring, and carries its size as a marker capsule in words ("2 wide, 1 tall", pill type at 700, tabular, on-marker) and a corner grip, an 18px marker square at the inner radius ringed 3px in the page colour, set 8px out from its bottom-right corner. Capsules sit across the tile's top edge, 10px in from the right; the size capsule shows only on the chosen tile, and on the others what changed in the draft shows as a marker capsule in the same place.
- **Moving a tile:** after 5px of travel the tile lifts (a copy in a drag layer over the phone's screen, above the masthead and the tab bar, scaled to 1.05, the drag lift) and follows the pointer 1:1 while its own cell goes invisible. Nothing marks the landing place but the gap itself, as on an iPhone's home screen: no slot box, no column guides. The grid repacks live in the page by the same packing as `HubGrid`: once the tile's top-left cell has rested 130ms over a new cell the order changes (always planned from the arrangement the drag started with, so the picture depends only on where the tile is held), and the neighbours glide to their new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)`. Only the tiles that must move do: a small tile over a big one makes it give way downwards and the row above fills from below; nothing ever jumps. Holding the tile near the phone's top or bottom scrolls the screen every frame, faster the nearer the edge, never past the end, and the grid stays still until the scroll stops. Arrow keys move the chosen tile one cell in that direction through the same plan. Letting go settles the tile into its gap over 220ms on a gentle spring (`linear()` with a 3% overshoot) while its shadow fades; Escape, or letting go off the phone, glides everything back and scrolls the phone back smoothly. Resizing from the grip keeps the drag lift without the scale and repacks the others live as the size changes. With the keyboard, arrows move the chosen tile and Shift with arrows resizes it, and the others glide the same way. Reduced motion drops the glide and the lift's scale. Edge scrolling arms only once the pointer has left the edge zone or moved half a row. However a drag ends (drop, Escape, a lost pointer, the tab hiding), the grid is left clean: no hidden or shifted tile, and the dropped picture is exactly the order saved.
- **Settings panel:** the tile's name in title type over a caption, then fieldsets with legends at label size and 700: size on the grid (36px round steppers with a tabular value), layout at this size, colour from the theme, icon, words, picture, opens, and who sees it and when, with the actions under a hairline, wrapping at their natural widths (a danger action on its own line). Beside the canvas the panel is sticky at the top of the window (above the sticky stage) and scrolls on its own.
- **Colour swatches:** four to a row (10 by 8px gaps), each a 40px "Aa" chip at the field radius drawn in its own tone with a hairline inset (the Card chip lifted), over its staff name in caption ink-2, centred; the chosen chip has a 2px marker outline at a 2px offset and its name turns ink at 600. A caption under them says the words change colour by themselves to stay easy to read.
- **Layout picker:** the kind's layouts stacked 6px apart, each a fill-on-surface box at the field radius (10 by 12px padding) with the name at row size over a caption saying what it draws; the chosen one turns the page colour with an inset 2px marker ring, and a layout that doesn't suit the tile is greyed to 55% with a caption saying why.
- **Contrast:** there is no light and dark proof on show. The tile is drawn twice off screen, in light and in dark, and the contrast of its text measured; only when a scheme falls under 4.5 : 1 does a warning notice appear at the top of the panel, in plain words ("Words on this colour are hard to read in dark mode. Try another colour.").
- **Icon picker:** the chosen icon in a 38px fill-on-surface cell at the tile radius beside its name at row size and a small quiet "Change icon" (Done while open); Change opens the grid of 38px cells, auto-filling, 6px apart, at the tile radius on fill-on-surface in ink-2, the chosen icon primary with on-primary, with the icon upload under it. The icon colour select follows when there is an icon.
- **Add a tile:** a sheet of kinds grouped under label-size 700 heads in ink-2 (Your library, Your own, From the app), each a fill-on-surface card at the 14px radius (14px padding) in an auto-filling grid of at least 17rem: its row tile (warm for the church's own and saved templates, accent for the app's), the name at row size, a caption line, its starting size in tabular ink-3; hover steps to strong sand.

### Tab bar canvas
The tab bar arranged on the desk, in the same world as the Hub canvas: the draft's Home on the phone, with the draft's tab bar.
- **On the phone:** each tab is a button; hover draws an inset 2px marker ring at 60%, and the chosen tab the full 2px marker ring at the field radius. More is drawn at 60% and can't be chosen.
- **Rail:** Add a tab as the full-width primary button; *Show the tabs for* as the 38px sand select; then *On the bar, in order*: 44px sand rows at the field radius, each numbered in tabular ink-3, the tab's icon and word at caption size 600, an 8px marker dot when the draft changed it, and quiet *Move left* and *Move right*; the chosen row turns the surface colour with the inset 2px marker ring. More closes the list as an outlined row with a lock and "Always last" in ink-3. Tabs not on the bar for that person follow as quiet rows saying why.
- **Settings panel:** the tile settings panel's shape: Opens (the place field), On the tab (the word, with its 12-letter limit as a hint, and the icon picker without *No icon* or uploads), Who sees it and when, and *Take it off the bar* as the danger action.

### Motion
One curve, `cubic-bezier(0.22, 1, 0.36, 1)`, for presses, sheets, the section-tab underline and page entry. Tab roots fade in from 40% (220ms); pushed pages fade and slide 20px from the right. Presses scale (0.94 to 0.98). The live dot pulses on a 1.6s cycle. A paged sheet slides a new page 14px in from the side it comes from (260ms), and the current page dot widens over 240ms. In the admin panel the sidebar's marker bar scales in over 240ms, hovers and fills ease over 160ms (120ms for table rows), and sort arrows fade over 160ms. Week-rail days fill over 160ms and month-grid days over 140ms; the rich text toolbar's buttons ease their fill over 140ms. Going back fades rather than slides, and a swipe from the left edge of a pushed page drags it away and goes back. On the Hub canvas tiles glide to new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)` (a curve of their own), and a dropped tile settles on a gentle `linear()` spring. Reduced motion collapses every animation and transition to 1ms, in the app and the admin panel alike; on the Hub canvas the drag still works but tiles move without gliding or lifting.

## Implementation

The system ships in `src/Platform/GSBC.ChurchApp.Platform.Ui`, and both hosts link it: the app in full,
the admin panel for tokens, type and the shared components. The admin shell, the office week's columns and admin-only page styles live in the admin host's `wwwroot/css/admin.css`; the day rail is shared and lives in `ui.css`. A feature's own screens are styled in its Client's `wwwroot/css` (the announcements desk and the inbox in `notifications.css`, the calendar in Events Client's `events.css`). The agenda row, the week rail, the toggle chips and the masthead's Beside row are shared and live at the end of `ui.css`; the rich text editor is styled in `rich-text.css`.

- **Tokens:** `wwwroot/css/tokens.css` holds every literal colour: the semantic roles, redefined per
  `data-theme` and `data-scheme`, plus the palette-picker swatches. `ui.css` styles the components from
  the roles only. `Architecture.Tests` (`DesignTokenRules`) fails on a literal colour in any other
  stylesheet and on a px font size (the root fallback aside), which are the Roles Not Hexes and
  Dynamic Type rules.
- **Theme:** `wwwroot/js/theme.js` runs in `<head>` and sets `data-scheme` and `data-theme` from what
  the phone last knew, so nothing flashes. `AppTheme` (in the app's layout) reads Configuration's
  `app.appearance` area through the store, applies staff's palette at start-up and whenever the feed
  changes, and remembers it. `Appearance` holds the person's Light, Dark or Automatic and a local
  preview palette.
- **Shell:** `wwwroot/js/shell.js` and the `Shell` service: haptics, back with a fallback, the top bar
  turning solid past the masthead, edge-swipe back, scroll to top, pull to refresh, the status bar.

| Design | Component | Notes |
|---|---|---|
| Screen frame, top bar, back | `Page` | `BackHref` makes it a pushed page (slide in, back button, edge swipe); `OnBack` gives the back button its own action; `Actions` fills the top bar's end |
| Masthead | `Masthead` | Title, note, a slot for the live element, `Beside` for a control on the title's row, and a `Class` |
| Section and section head | `Section` | Title, lead, a "see all" link, a trailing slot |
| Callout, picture card, navy card | `Card` | `CardTone.Callout`, `Raised` (only where a picture leads, via `Media`), `Masthead` |
| Rows and boxed lists | `ListGroup`, `ListRow` | Leading tile tones, trailing text or content, chevron or external-link icon, disabled; `Reading` for entries to read, `TitleExtra` for a pill after the title, `Below` for more; `Unread` true or false sets the dot and the title weight |
| Pull to refresh | `PullToRefresh` | Wraps the whole page body, masthead included; the indicator sits under the top bar. Touch only, so offer a button too |
| Sheet | `Sheet` | Grabber, title, lead, body, actions; Escape and the scrim close it; centred on wide screens; `Cover` adds the navy band (then name it with `Label`); `Wide` for a list to choose from; opening moves focus into it without scrolling the page; it opens in a native `<dialog>` with `showModal()` (the top layer), so no scrolling, sticky or transformed ancestor can clip or hide it (Safari did), falling back to the fixed overlay where `showModal` is missing |
| Sheet heading | `SheetHeading` | `Icon`, `Title` and an optional `Note`; goes in a sheet's `Cover` |
| Paged sheet | `PagedSheet` | `Count`, a `Cover` and `Page` per index, an optional `Action`; page dots, swipe, Next and Done |
| Facts | `FactList`, `Fact` | Label and text, with an optional icon tile; a fact with no text is skipped |
| Link in a row | `TextLink` | Deep-aqua label with a chevron and a haptic |
| Empty state | `EmptyState` | Icon, title, line, an action slot |
| Dates and times | `DateLine` | Church time, `Long`/`Short`/`Relative`/`TimeOnly`, ranges, "10:30am" and "6pm" as the bulletin writes them |
| Live/sync | `SyncIndicator` | Up to date, catching up, offline; plain or on the masthead |
| Buttons | `Button` | Primary, Quiet, On masthead, Ghost on masthead, Live, Danger; block, small, icon-only, busy |
| Fields | `TextField`, `SelectField`, `ToggleRow`, `Segmented` | Labels tied to inputs; hint and problem lines are described-by; a `SegmentOption` can carry a `Count` |
| Copy a value | `CopyField` | Label, read-only value, Copy that turns to Copied, a polite status line; `ButtonOnly` is the copy button alone |
| Loaded data | `LiveData` | Loading, failure and stale states; reloads itself when what it depends on changes |
| Loading | `Skeleton` | Rows, lines or a block, with a status for screen readers |
| Tab bar and More | `AppTabBar`, `MoreList`, `PhoneTabs` | The phone's tab bar draws the first four published tabs the person sees (`TabBar.For`), then More; More lists every module entry that isn't a tab now. Tabs are placements in the Tab bar area, chosen by staff in a draft |
| Admin sidebar | `ModuleNav` | Grouped by `NavEntry.Group` (`NavGroups`: Content, People, The app, You, with You always last), with `Counts` for the orange capsules |
| Icon picker | `IconPicker` (Configuration.Client) | The chosen icon and *Change icon*, opening the grid; uploaded icons and their upload for tiles, the app's own only for tabs |
| Admin title band | `TitleBand` | Title, lead or lead slot, back link, actions; sets the browser title |
| Admin tables | `DataTable`, `TableColumn` | Columns with a cell, an optional sort key and a class (`num`, `optional`, `quiet`, `truncate`); a toolbar slot, an empty slot, selection with the arrow keys |
| Table and detail | `SplitView` | A list and an optional detail column, hairline-ruled |
| Search | `SearchField` | Labelled, settles before it reports the value |
| Filters | `FilterChips`, `FilterOption` | One choice from a set, with optional counts |
| Office week | `IOfficeWeekSource`, `OfficeWeek` | Features contribute tasks and dated spans (`Everyone`, `Layer`, `Draft`); `NavEntry.Summary` is an area's one-line purpose on the home page |
| Day rail | `DayRail` | `First`, `Today`, `Days` (7, or 14 for a fortnight), the spans, a hidden heading, the quiet line, a `Legend` of kinds and words, and the `Selected` href; a span whose end equals its start is a moment |
| Admin workbench | `Workbench` | Aside, main and detail panes; `Narrow` makes it the one-column form; `Showing` picks the pane in one column |
| Pill | `Pill` | `PillTone.Marker`, `Accent`, `Masthead`, `Quiet`, with an optional icon |
| Charts | `StatTile`, `TrendLine` | `Quiet` for a value too small to show; a main and an optional comparison series, a table of every number, an `Empty` line under two points |
| Ranked list | `RankedList`, `RankedItem` | Title, explanation, label and value items, an `Empty` line; `Numbered` (default true) shows the rank |
| Save state | `SaveStatus` | Saved, waiting, saving, failed with a retry; an optional detail |
| Tabs with counts | `TabStrip` | `TabOption` value, label and count; orange underline on the selected tab; a trailing slot |
| Preview banner | `PreviewBanner` | Docked above the tab bar; title, detail, Hide, Exit |
| Phone preview | `PhoneFrame`, `PhoneDevice` | A true-size device with status bar, cutout and tab bar, scaled to fit its space; an optional palette |
| Lock-screen proof | `LockScreenProof` | Title, body, `TimeSensitive`, the date line, and a caption slot |
| Home and surfaces | `HomeScreen`, `SurfaceViews` | The one way Home and the admin preview draw surfaces |
| Hub grid and tiles | `HubTiles`, `HubTile`, `HubIcon`, `HubTileViews` | The grid packs tiles by `HubGrid` (2 to 4 columns, up to 4 rows); `HubTile` draws a `TileShape` in a `TileTones` tone; each kind's view picks its `TileVariant` |
| Hub canvas | `HubCanvas`, `TileSettings`, `AddTileSheet`, `HubLibrary` (Configuration Client) | Arranging, sizing and recolouring tiles in a draft; the controls rail; the settings panel with swatches, the layout picker and the contrast warning. `hub-canvas.js` carries the live drag and a port of `HubGrid`'s packing |
| Choose several | `ChipPicker` | `Items`, `Chosen`, `Text`, an optional `Detail`, `Placeholder`, `Empty`; matches starting with the search come first |
| Who sees it | `AudiencePicker` (Content) | Everyone, Signed in, or several roles through `ChipPicker`; an audience it can't express shows as "Set up specially" |
| Choose a picture or file | `MediaChooser` (Content) | A wide `Sheet` around the registered `MediaPicker` (Media's `MediaChoice`); `ImagesOnly`; opens on the last folder used |
| Breadcrumbs | `Breadcrumbs`, `Crumb` | `Label`, a `Trail` of crumbs (label, key, icon), `OnOpen`; `CanDrop` and `OnDrop` make crumbs drop targets |
| Act on several | `SelectionTray` | `Label`, `Count` (hidden at 0), `One` and `Many` nouns, `OnClear`, the actions as child content |
| Media library | `MediaLibrary`, `FolderTiles`, `MoveToSheet` (Media Client) | The shelves, path row, tiles, grid or list, tray and folder sheets; `FolderTiles` has a `Compact` form for the chooser; `MoveToSheet` takes `Current`, `Suggested`, `Blocked` and an `Action` label |
| Places in the app | `PlaceDirectory`, `PlaceExplorer`, `PlacePhone`, `PlaceField` | Search, area chips and grouped rows; the directory beside the phone column with `PlaceAction`s (one `Leads`); a `PlacePreview` a feature registers, else the masthead summary; the picker field in a wide sheet. A holder with `data-fit-height` fits the phone frame to its height |
| Agenda row | `AgendaRow` | `Time`, `Title`, `Label`, `Subtitle`, `NotOn` (the circle-off line), `Quiet`, `Past`, `Warn`, `Selected`; a link, a button or plain |
| Week rail | `WeekRail` | `Days` (`RailDay`: date and whether anything is on), `Today`, `Here`, `Picked` |
| Choose several on and off | `ToggleChips` | `Label`, `Chips` (`ToggleChip`), `Toggled`, a `Trailing` slot; `toggle-chips--wrap` wraps instead of scrolling |
| Calendar | `DaySection`, `MonthGrid`, `EventHead`, `DateBlock`, `EventDetails`, `EventEditor`, `ComingUpTile` (Events Client) | The week, the month, an event and editing it on the phone and in the admin panel (`InPanel`); styled in `events.css`. The admin page is `CalendarWorkbench` with `CalendarSettingsPanel` and `BreakPanel` |
| Icons, mark | `Icon`, `BrandMark` | Lucide paths (ISC); the mark is the logo pack's file as a mask, never redrawn |

A development build has More → Design gallery, which shows every block in the current palette.

## Do's and Don'ts

### Do:
- **Do** open every tab root on a masthead with the title in display type, a beige note line and the mark at 7% opacity bleeding off the right edge.
- **Do** add sticky section tabs with the sliding orange underline to any long page divided into named sections, and keep every tab label visible at 390pt width.
- **Do** put the notifications bell in the top bar of every tab root.
- **Do** build content as hairline-divided rows on the white page, and use sand (callouts, sermon head, player, fields) for anything the bulletin would shade.
- **Do** size type in rem off the iOS body root and keep Montserrat at 400, 600 and 700.
- **Do** read colours through the semantic roles so light, dark and the yearly palette all work without component overrides.
- **Do** keep touch targets at 44px or more and give presses a scale and a haptic.
- **Do** mark a selected row with Selected Fill, a tonal step, and nothing else.
- **Do** open every admin page on a title band, and work an area as a data table with its detail in a split view beside it.
- **Do** offset admin breakpoints by the 248px sidebar.
- **Do** let staff choose what something opens with the place field and its directory, never by typing an address.
- **Do** give a Home tile one of the eight theme tones, and draw it in a layout its kind declares for its size.
- **Do** mark the chosen tile on the Hub canvas with the 2px marker ring, its size capsule in words and the corner grip, and warn in plain words only when a tone's text is hard to read in light or dark.
- **Do** grey out a layout that doesn't suit a tile and say why, rather than hiding it.
- **Do** keep a regular event quiet in the agenda and let only a special, a change or a date that isn't on speak, with a date not on said in a line under its title.
- **Do** edit an event where it is read: the same head with dashed outlines round what can change, and the save and scope choices in a foot bar.
- **Do** mark whatever the app owns with the lock glyph and one plain caption, and keep a page's bars and path row above its split view so choosing an item never moves the list.

### Don't:
- **Don't** lay a tab root other than Home out as a grid of tiles, and don't let the Hub become a uniform grid of identical icon squares or a feed of identical cards.
- **Don't** float buttons over the reading column; actions belong in the top bar, in a section, or in a sheet.
- **Don't** use orange as a button fill or decoration; it marks position and newness only.
- **Don't** add shadows to rows, callouts or the player; only boxed lists and picture cards lift.
- **Don't** use DM Serif Text anywhere but the yearly palette's masthead title.
- **Don't** redraw or restyle the church mark beyond recolouring the supplied artwork.
- **Don't** set font sizes in px.
- **Don't** put a phone masthead in the admin panel, or open it on a grid of tool tiles.
- **Don't** box the split view's detail as a card; it is a hairline-ruled column.
- **Don't** make the admin work area a size container.
- **Don't** mark a date that isn't on with a suffix on its title or by dropping it from the list.
