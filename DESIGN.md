---
name: GSBC Church App
description: Good Shepherd Baptist Church's own app, set like its Sunday bulletin.
colors:
  navy: "#2a3d5e"
  navy-deep: "#22334f"
  aqua: "#009c8f"
  aqua-deep: "#00776d"
  live: "#008176"
  orange: "#e7ae74"
  orange-ink: "#8a5422"
  on-marker: "#2a1a08"
  beige: "#d5c9b7"
  white: "#ffffff"
  sand: "#f4efe7"
  sand-strong: "#e9e1d4"
  ink: "#18233a"
  ink-2: "#545e70"
  ink-3: "#626a79"
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
  night-ink-3: "#9ca2b1"
  night-orange-ink: "#f0c493"
  night-line: "rgb(255 255 255 / 0.09)"
  selected-fill: "#f4efe7"
  fill-on-surface: "#f4efe7"
  field: "#f4efe7"
  field-edge: "rgb(98 106 121 / 0.7)"
  night-selected-fill: "#34446a"
  night-fill-on-surface: "#26334f"
  night-field: "#2d374d"
  night-field-edge: "rgb(156 162 177 / 0.7)"
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
  viewer-backdrop: "rgb(8 12 22 / 0.97)"
  viewer-control: "rgb(255 255 255 / 0.12)"
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
  space-3xs: "2px"
  space-2xs: "4px"
  space-xs: "6px"
  space-sm: "8px"
  space-md: "12px"
  space-lg: "16px"
  space-xl: "20px"
  space-2xl: "24px"
  space-3xl: "32px"
  space-4xl: "48px"
  stack: "14px"
  gutter: "20px"
  after-tabs: "20px"
  section-first: "22px"
  section: "30px"
  tile-gap: "12px"
  tile-row-min: "88px"
  tile-row-max: "132px"
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
    backgroundColor: "{colors.field}"
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
    backgroundColor: "{colors.live}"
    textColor: "{colors.white}"
    rounded: "{rounded.touch}"
    padding: "0 14px"
    height: "36px"
  button-disabled:
    textColor: "{colors.ink-2}"
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
    backgroundColor: "{colors.fill-on-surface}"
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
  pill-danger:
    textColor: "{colors.danger}"
    rounded: "{rounded.full}"
    padding: "3px 9px"
  segmented:
    backgroundColor: "{colors.field}"
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
    backgroundColor: "{colors.field}"
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
    backgroundColor: "{colors.fill-on-surface}"
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
  sidebar-jump:
    backgroundColor: "{colors.masthead-chip}"
    textColor: "{colors.on-masthead-2}"
    typography: "{typography.label}"
    rounded: "{rounded.inner}"
    height: "38px"
    padding: "0 10px"
  sidebar-area-head:
    textColor: "{colors.white}"
    typography: "{typography.label}"
    rounded: "{rounded.inner}"
    height: "36px"
    padding: "0 10px"
  sidebar-pin:
    textColor: "{colors.on-masthead-2}"
    rounded: "{rounded.inner}"
    size: "30px"
  sidebar-pin-on:
    textColor: "{colors.orange}"
  jump-option:
    textColor: "{colors.ink}"
    typography: "{typography.row-title}"
    rounded: "{rounded.inner}"
    height: "52px"
    padding: "6px 10px"
  jump-option-active:
    backgroundColor: "{colors.sand}"
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
    backgroundColor: "{colors.field}"
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
    backgroundColor: "{colors.field}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    height: "40px"
    padding: "0 12px"
  filter-chip:
    backgroundColor: "{colors.field}"
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
    backgroundColor: "{colors.field}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.inner}"
    height: "36px"
    padding: "0 10px"
    typography: "{typography.caption}"
  rich-text-tool-pressed:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  rich-text-input:
    backgroundColor: "{colors.field}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "13px 14px"
    typography: "{typography.body}"
  rich-text-field-tool:
    textColor: "{colors.ink-2}"
    rounded: "{rounded.inner}"
    size: "36px"
  rich-text-field-tool-pressed:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  rich-text-field-input:
    backgroundColor: "{colors.field}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "12px 16px"
    typography: "{typography.body}"
  rich-text-field-bar:
    backgroundColor: "{colors.white}"
    padding: "4px 0 6px"
  rich-text-field-insert:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.inner}"
  post-view-day:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.field}"
    padding: "8px 0 6px"
    width: "62px"
  post-view-end:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink-2}"
    typography: "{typography.caption}"
    padding: "16px"
  writing-desk-page:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "6px 24px 48px"
    width: "800px"
  writing-desk-side:
    backgroundColor: "{colors.white}"
    padding: "20px"
    width: "360px"
  blue-card:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "16px 18px 14px"
  blue-card-kind:
    textColor: "{colors.beige}"
    typography: "{typography.pill}"
  blue-card-number:
    textColor: "{colors.white}"
    typography: "{typography.title-lg}"
  blue-card-status-current:
    backgroundColor: "{colors.aqua}"
    textColor: "{colors.white}"
    rounded: "{rounded.full}"
    height: "24px"
    padding: "0 10px"
    typography: "{typography.pill}"
  blue-card-status-soon:
    backgroundColor: "{colors.orange}"
    textColor: "{colors.on-marker}"
    rounded: "{rounded.full}"
    height: "24px"
    padding: "0 10px"
    typography: "{typography.pill}"
  blue-card-status-run-out:
    backgroundColor: "{colors.white}"
    textColor: "{colors.danger}"
    rounded: "{rounded.full}"
    height: "24px"
    padding: "0 10px"
    typography: "{typography.pill}"
  sign-in-code:
    textColor: "{colors.ink}"
    typography: "{typography.headline}"
  person-tab-panel:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.card}"
    padding: "20px 22px"
    typography: "{typography.caption}"
  person-tab-head:
    textColor: "{colors.ink-3}"
    typography: "{typography.pill}"
  safeguarding-flag:
    textColor: "{colors.orange-ink}"
    rounded: "{rounded.card}"
    padding: "13px 16px"
    typography: "{typography.row-title}"
  safeguarding-flag-run-out:
    textColor: "{colors.danger}"
  person-finder-row:
    textColor: "{colors.ink}"
    height: "52px"
    typography: "{typography.row-title}"
  feedback-empty-frame:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.card}"
    padding: "18px 14px"
    width: "9.5rem"
  feedback-strip-thumb:
    rounded: "{rounded.tile}"
    width: "46px"
  feedback-column:
    backgroundColor: "{colors.sand}"
    textColor: "{colors.ink}"
    rounded: "{rounded.card}"
    padding: "10px"
    typography: "{typography.label}"
  feedback-card:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.tile}"
    padding: "10px 12px 12px"
    typography: "{typography.caption}"
  feedback-card-title:
    textColor: "{colors.ink}"
    typography: "{typography.row-title}"
  feedback-label:
    backgroundColor: "{colors.fill-on-surface}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.inner}"
    padding: "1px 8px"
    typography: "{typography.pill}"
  feedback-label-accent:
    textColor: "{colors.aqua-deep}"
  workbench-rail:
    backgroundColor: "{colors.sand}"
    width: "248px"
    padding: "24px 16px 32px"
  workbench-record:
    backgroundColor: "{colors.white}"
    width: "440px"
    padding: "24px 24px 0"
  pane-head-title:
    textColor: "{colors.ink}"
    typography: "{typography.title-lg}"
  rail-filter:
    textColor: "{colors.ink-2}"
    typography: "{typography.label}"
    rounded: "{rounded.inner}"
    height: "38px"
    padding: "0 8px"
  rail-filter-on:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
  settings-group-admin:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "16px 20px 20px"
    width: "820px"
  settings-group-phone:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "12px 16px"
  menu-panel:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.field}"
    padding: "6px"
    width: "15rem"
  menu-item:
    textColor: "{colors.ink}"
    typography: "{typography.row-title}"
    rounded: "{rounded.inner}"
    height: "44px"
    padding: "8px"
  menu-item-danger:
    textColor: "{colors.danger}"
  account-menu:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "12px"
    width: "312px"
  date-popover:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.card}"
    padding: "12px"
    width: "344px"
  date-popover-day:
    textColor: "{colors.ink}"
    rounded: "{rounded.inner}"
    height: "36px"
    typography: "{typography.caption}"
  date-popover-day-chosen:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
  board-column:
    backgroundColor: "{colors.sand}"
    rounded: "{rounded.card}"
    padding: "12px"
    width: "180px"
  board-card:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.tile}"
    padding: "12px 16px 16px"
    typography: "{typography.caption}"
  picture-viewer:
    backgroundColor: "{colors.viewer-backdrop}"
    textColor: "{colors.white}"
  picture-viewer-button:
    backgroundColor: "{colors.viewer-control}"
    textColor: "{colors.white}"
    rounded: "{rounded.full}"
    size: "44px"
---

# Design System: GSBC Church App

## Pattern map: one way to do each job

Read this before building any screen. Each job has one shared component and one placement, chosen by the owner on
2026-11-12 (`.impeccable/mocks/design-system/`, brief in `.impeccable/surfaces/churchapp-platform-ui-wwwroot-css-ui-css-0c623f32.md`).
The admin panel is designed for a desk and the app for a phone: they need not look alike, only do the same job with
the same data. A small difference between pages is a parameter on the shared component, never a copy. Every
component is shown, live, in the gallery (`/gallery` on both).

| Job | Use | Where things go |
|---|---|---|
| A list and the record chosen from it (admin) | `Workbench` | Filters in a sand rail on the left only when the page has filters; the list in the middle; the record ruled off on the right, staying beside the list at every desktop width. At 1360px and under the rail moves above the list, or, with `FoldAside`, steps aside while a record is open. A page without a rail uses `SplitView`, which is the same workbench. The page's one add action sits in the `TitleBand`, top right. |
| Chatting at a desk (admin) | the chat desk: `ChatsList` + `ChatView InPanel` + `ChatPeople` (`ChatFrame`) | Three panes side by side under one head line: your chats on the left (title, search, rows with `ListRow Current` marking the open one on Selected Fill), the conversation in the middle with the shared `Composer` at its foot, and *Chat settings* ruled off on the right, shown and hidden by a panel arrow at the conversation's head (`AppIcons.ShowSidePane` while it's hidden, `AppIcons.HideSidePane` while it's shown, labelled *Show* or *Hide chat settings*; owner, 2026-11-13, #77). At 1200px and under the chat list steps aside while *Chat settings* is shown. The phone keeps the cog, which opens the chat's settings page. Starting a chat stays in a ministry's Chats tab. At 900px and under it is one pane at a time with a back button. |
| A record's heading | `PaneHead` | Title and a one-line lead; its ⋯ menu (`Actions`) holds Delete and every other action on the item; Open as a page and Close at the right. |
| Saving | `SaveBar` (+ `UnsavedGuard`) | **Cancel bottom left, Save bottom right** of anything that saves, on both sides. The status line ("Saved", "You have changes that aren't saved") or the reason it can't save sits on its own line above, at the right (centred over a full-width save), never between the buttons. Other actions (the bar's child content) sit after Cancel; when the bar is 520px or narrower they move to their own line above it, at the right. A create form never says it has unsaved changes (`New`). |
| The keyboard (phone) | `Composer`, and `RichTextField`'s format bar | Only a composer, where typing and sending are one act (a chat's message box, Send feedback), docks on the keyboard; so does the format bar of the text you're writing in (`RichTextField`), because formatting is part of typing (2026-11-13). A form's save bar stays at the foot of the page and the keyboard covers it; it is there when you scroll to the end (owner, 2026-11-12, #65: "Makes no sense for cancel and save this date to follow it"). `DesignDriftRules` fails on anything else docked at the foot that the keyboard block doesn't send back into the page. |
| Deleting | the item's ⋯ menu, then a `Sheet` | Delete is always an action on the item, never a red button in a foot or on the page, with one exception: a thing that has a settings page or sheet (a cog) keeps Delete at the foot of its settings, and has no ⋯ beside the cog (owner, 2026-11-12, #64). In a `Sheet` that edits one item, its ⋯ sits in the sheet's heading at the right (`Tools`). It asks once, saying what goes with it. Actions list the main (red) one first: the admin panel shows it at the right with Keep it at the far left; the phone stacks it on top. |
| Adding | the record pane | New opens a blank record, already chosen, in the record pane beside the list; Cancel takes it away. On the phone it is a page with the same fields. |
| A group of settings | `Section Grouped` (+ `Foot`) | A card on the admin panel running the full width of its pane, short fields two to a line in a `ui-pair--wide` (owner's pick on the decision page, 2026-11-12, #71; `.impeccable/mocks/settings-width/`), with its title and lead inside and a sand foot for its save bar or "These change straight away"; an inset group on the phone. A form that saves with a button and switches that change at once never share a group. |
| Switching views | `TabStrip`, `Segmented`, `FilterChips` | Tabs (the underline) for different things; Segmented for the same list two ways; chips only to narrow down. Chips never switch views, and Segmented never picks a value that tabs should show. |
| Tools for a list | the `Workbench` rail with `FilterChips Rail` (admin); main button + `Menu` (phone) | Search and filter groups live in the rail (`FilterChips Rail`, one per group, with counts). On the phone the main action is one wide button and the rest go in its ⋯ `Menu`, so nothing wraps alone. |
| More actions | `Menu` | One ⋯ button; its panel is always whole on screen, inside a short sheet too, scrolling within itself only when the window is too short (#69); items are made with `this.Item(...)` (`MenuItems`), so each action belongs to the page that made it and the page redraws after it; they say what they do, with a line under when it helps; Delete last and red, after a rule (`startsGroup`). A hand-built panel uses `MenuButton` rows. |
| Actions on a row in a list | `ListRow` with `Actions` | The row opens the thing; its ⋯ menu sits beside the row, never inside its link, so pressing ⋯ never also opens it. Delete or Unlink last and red, asking once in a `Sheet` (2026-11-12, #50). |
| Putting things in order | `ReorderList` | Drag a row by its handle (touch or mouse); its ⋯ menu has Move up and Move down for a keyboard or screen reader; an optional preview above follows the drag (a ministry's tabs, chosen by the owner on the decision page, 2026-11-12). Each move applies at once. `Quiet` is for a short list in a narrow column (the admin calendars): the handle shows only on hover or focus (always on touch screens), Alt and ↑ or ↓ on it moves the row, and there is no ⋯ menu; a caption under the list says how ("Drag a calendar by its handle to change the order everyone sees. With the keyboard: Alt ↑ or Alt ↓ on its handle."). The handle sits inside the row, so a chosen row's fill takes it in (#61). The new order shows the moment it's dropped, never waiting for the server (#58, #62). |
| A small panel that opens from a button | `Popover` | The surface on the menu's shadow; Escape or a click outside closes it. The Account menu is one. |
| One role of several | `RoleMenu` | A button showing the role; its menu says what each role allows, with Remove in red last. Applies at once. Several roles at once stay a tick list (`RoleTick`). |
| Steps something moves through | `StagePipeline` | Done steps ticked, the current one solid, the next waiting; press a step to move there. `ReadOnly` shows the same steps to someone who can see where it is but not move it. |
| Boards | `Board` + `BoardCard` | Sand columns with a count, a dashed sentence when empty, optional "Add one here", a folded column for what's put aside. |
| Taking a file | `DropBox` (`FilePick Box`) | Every place that takes a file, on both sides: a dashed box saying what it takes ("Drop a document or picture here") over how ("Or click to choose one from this computer"); drop on it, click it, or paste where the page allows (Send feedback). It turns deep aqua while files are over it. On the phone the same box opens the photo library or files. Never a link-like row (owner, 2026-11-12, #66). |
| Writing a message | `Composer` (Platform.Ui Content) | Docked at the foot of a full-height page: an attach button (a `FilePick` in its `Attach` slot, the paperclip `AppIcons.Attach`) at the left, the field, a round Send at the right; what's attached sits in `Above`. While its field has focus the phone hides the keyboard's ^ v ✓ bar. Chats' message box and Send feedback on the phone both use it (Send feedback without an attach button: its pictures are chosen at the top of the page); the chat puts its reply or edit line, *Writing for* chips, mentions and picked files in `Above`, and Send turns to a check while editing. Pressing Send, by mouse or touch, leaves the focus in the field, so the phone's keyboard stays up between messages (owner, 2026-11-13, #82). In the admin panel the chat's composer takes `EnterSends`: Enter sends, Shift+Enter starts a new line, Escape stops a reply or edit, and a small hint under it says so (hidden on touch screens); pictures can be pasted or dropped anywhere on the conversation (`DropOn`). |
| Days marked on a month | `MonthDays` | A month of day tiles (Monday first) with ‹ › and a caption key under it: shaded for all day, the top half for mornings, the bottom half for evenings, lightly shaded for some hours, struck through for a day skipped; choosing a day opens what's on it. Days away use it. |
| One rule of a few, each needing a sentence | `Segmented Rows` | Radio rows inside a settings group, each with a detail line when its label needs one; a sentence after the choices says what follows. The roster's swap rule and what the team sees use it. |
| Dates and times | `DateField`, `TimeField`, `DateTimeField` | Admin: type it ("15 Nov", "7:30pm"), or open the calendar or the list of times (every 15 minutes) from the button at the field's right end; what was understood reads back under it with a check. The calendar's month heading opens the months, and their year opens twenty years, to jump far (#70). Today and the Sundays show only where they make sense: never on a birth date (`Shortcuts="false"`). Phone: the native wheel. Never `Type="date"` or `Type="time"` in a feature. |
| A picture, bigger | `PictureOpen` + `PictureViewer` | The whole window on near-black, arrows and a filmstrip, Download. Never squeeze a picture into a pane. |
| A message and its reactions | `MessageBubble` + `BubbleReactions` | Reactions sit under the bubble as small counted chips (yours ringed in deep aqua); the same chips in a row, without counts, are the picker at the top of a message's sheet. A message's photos open in the picture viewer. A reply's quote is a button (`OnQuote`): it reads older messages until the original is there, scrolls it into the middle and rings it in marker for a moment (`Shell.PointOutAsync`); if the phone doesn't have it, the quote says so ("It isn't on your phone, so it can't be shown."). In the admin panel a message's actions open from a ⋯ beside the bubble (`MoreLabel`), shown on hover or focus, or Enter on the focused bubble, as a `Popover` anchored above it instead of the phone's sheet: the reaction row, a rule, then `MenuButton` rows ending on Delete after a rule, with no line or gap under it (#76). Typed Markdown shows formatted in the bubble (the chat scope: bold, italic, strikethrough, highlight, code, links, lists and quotes; headings, tables, splits and pictures stay as typed), and the composer has no format bar (owner, 2026-11-14). |
| An action inside a line of text | `Button Variant="Link"` | Deep-aqua words at 600 with no fill or padding, in the text's own size ("Couldn't send · Try again · Delete", "Seen by 4"). An action that stands alone is a real button. |
| A page's top-bar actions (phone) | `Button Variant="Quiet"` icon, `Menu` | Icon buttons in the bar take the bar's colour. A cog opens the settings of what you're looking at, and Delete sits at the foot of those settings; a ⋯ `Menu` is only for something with no settings, never beside a cog (#64). |
| Icons | `AppIcons` (Platform.Contracts) | One icon per meaning, named for the thing (`AppIcons.Prayer`, `AppIcons.Settings`); a cog always means "the settings of what you're looking at"; the eye-off (`AppIcons.Hidden`) means hidden from everyone else, never a preview (that is the eye, `AppIcons.Previews`). |
| A page's count in the admin sidebar | `INavCounts` (Platform.Client) | A feature counts what waits for you on its page and names the feeds that change it; the sidebar adds it to the office week's counts and shows the total in the link's orange capsule. Chats counts unread chats and those waiting for your yes. |
| Light or dark | `SchemeChoice` | Phone: Appearance. Admin: the Account menu, which opens from the sidebar's Account row. |
| A page of news | `NewsList` (+ `NewsStory`) | What's urgent today first on a marker band, then the newest post leading with its 16:9 picture, then rows with the picture at the side (owner's pick on the decision page, 2026-11-13). Church news and *Your ministries* both use it. |
| Reading one post | `PostView` (+ `MediaStrip`), under `TopBarStyle.SolidTitleOnScroll` | The bulletin item, the same for every post: a navy rule, the date block beside the title with who it's from under it, then the pictures it opens with (a swipeable strip when two or more, "1 of 3", dots), the text with its first paragraph at lead size, the next card (`===`) under another navy rule, its link as a wide button, and a sand foot that ends it ("Posted today by … for everyone."), filling the screen on the phone (`FillsPage`). Church news, ministry posts and *Preview on a phone* all use it. |
| Writing a post | `PostEditor` (+ `WritingDesk` in the admin panel) | A title and one `RichTextField` holding the whole post: text, headings, pictures, galleries, video, files and links all in place in the text (owner, 2026-11-14: "just a title and proper markdown that lets you embed images/etc into it"). On the phone a full-screen page, with one row for who it's for, telling people and when that opens their settings (inline inside a sheet). In the admin panel it's a page of its own (`WritingDesk`), never the record pane (owner, 2026-11-13, #83). Files wait and upload with the save, on both sides. |
| Formatted text | `RichTextField` (Platform.Ui Content) | Live Markdown: it formats as you type, and the marks show faintly only where the cursor is. One bar of 36px quiet icons above the text: a *Text style* select (Text, Heading 1 to 3, where `Headings` allows), Bold, Italic, Strikethrough, Highlight, then Bulleted and Numbered list, Quote, Link, Code and ⋯ (Superscript, Subscript), groups split by hairline dividers, and a navy primary **Insert** at the right. Insert's groups: *Pictures and media* (Picture, Video, Video link, File, through Media's chooser with Upload), *Layout* (Split into cards, Line across, Table, Code block), *In the text* (Footnote, Emoji), *From the app* (Event, Sermon, Ministry, Life Group; see App cards). Cards and tables are drawn in place. Each place says what it allows with `RichTextMedia` (`Posts` everything; `Layout` the layout extras only; none, formatting only, and no Insert). Media shows in place, with a caption line under it (one quiet line for every block, underlined in aqua while typing), its library path (`~/Folder/file.jpg`) and a pill of controls (drag, add a picture beside it, replace, take out). Every block drags by the pill's handle with a mouse or a finger onto an aqua line between blocks (the block dims, Escape cancels, Alt ↑ ↓ on the handle with the keyboard), and each picture in a gallery has a ⋯ at its bottom right (on hover at a desk, always on the phone) with Move earlier, Move later, Replace and Take this picture out, a sheet on the phone (owner's pick on the decision page, 2026-11-14, #92, #93). On the phone the bar is one sliding row with Insert first and pinned, and Insert, the style and ⋯ open sheets; while the keyboard is up the bar sits on it with 44px-wide buttons. `RichTextEditor` (the Markdown box with a preview) is what older places still use until they move (owner's picks on the decision page, 2026-11-13 and 2026-11-14). |
| Something in the app inside a post | `AppCardFrame` (+ each feature's card, registered with `AddAppCardView`) | An event, sermon, ministry or Life Group sits in the text where it was put, as a card in one of three looks the writer picks per card: *Side rule* (the default), *Timetable line* or *Box* (owner, 2026-11-14). The frame draws; the feature fills it from the synced store. In the editor its pill of tools sits above it, never over its title. It goes in from Insert → *From the app*, through `AppCardPicker`. |
| Writing on a page (admin) | `WritingDesk` | The work as a white page on sand, up to 800px, with Write and Preview on a phone tabs; its settings in a 360px column on the right, in grouped cards under a caption head with a fold arrow (`AppIcons.HideSidePane`, `ShowSidePane`); folded, they sum up in one accent line beside the tabs that unfolds them. The save bar at the foot of the column, or across the page when folded; under 1228px the settings sit under the page and the save bar spans it. A title band above with Back and, once the item exists, its ⋯ menu (a new post has nothing in it to do) (owner's pick on the decision page, 2026-11-13). The page holds the title and one editor. |
| Spacing | `--space-*`, `--section`, `--section-first`, `--stack`, `--gutter`, `--after-tabs` | No new raw pixel spacing. |

**Rules for the workflow** (they apply to every station):

- Start from this map and the gallery. If a job isn't here, it is a new pattern: build it once in Platform.Ui, add it
  to the gallery and to this map in the same change, and run it past the owner on the decision page first.
- The drift tests in `tests/Architecture/…/DesignDriftRules.cs` and `IconRules.cs` hold the map: every class used in
  markup exists in a stylesheet; raw spacing, raw `<button>`, native date fields, quiet-first dialogs, `SplitView`
  and red delete buttons outside a confirm can only go down (`design-ratchets.json`); every public component appears
  in the gallery; one icon per meaning. When a count drops, lower its entry in the same change. Never raise one.
- A screen still built the old way moves onto its component the next time anyone touches it.

## Overview

**Creative North Star: "The Sunday Bulletin"**

The app reads like the church's own printed bulletin brought onto a phone. Every top-level tab opens on a navy masthead carrying a large title, a short line of context in beige, and the faint ring of the church's mark bleeding off the right edge; below it the page turns white and continues as a single readable column, with sand-coloured callouts where the bulletin would use a shaded box. On the long, sectioned pages (Sunday and Listen) a sticky row of section tabs sits under the masthead with an orange underline that slides to the section in view, so the page navigates like a folded bulletin rather than a dashboard.

Density is calm and generous: one column, a 20px gutter, list rows with hairline dividers instead of card stacks, and type sized off the iOS body text setting so the congregation's own Dynamic Type choice carries through. The system rejects the generic church-app template, a uniform grid of identical icon squares, and the feed of identical cards; content sits on the page, and cards appear only where a picture leads (an event, a sermon series). Home is the one tab root laid out as tiles (the Hub, chosen 2026-10-27): a grid the church arranges, where tiles differ in size, tone and content shape, so it reads as the bulletin's front page rather than an app launcher.

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
- A tab bar built from what features register, ending in More; with the Hub home (favoured 2026-10-27) it grows to Home, Listen, Prayer, More. A notifications bell with an orange unread badge joins every tab root with the inbox.

## Colors

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
- **Field** (field) and **Field Edge** (field-edge): what you type into or set (owner, 2026-11-11, chosen on the decision page: "Fill with an edge"). Fields are the field fill with a 1px field edge (ink-3 at 70%), as Inputs describe; quiet buttons, filter chips, the segmented track and steppers take the fill without the edge, and the switch's off track takes the edge over a fill of its own (ink-3 at 24% over the surface, 2026-11-13). In light the fill is sand; in dark it is the panel lifted 7% towards white (#2d374d in the brand), which every theme works out the same way, so a field still shows on a raised navy panel.
- **Ink** (ink), **Ink 2** (ink-2), **Ink 3** (ink-3): headings and titles, then body prose and subtitles, then inactive tabs, hints and trailing metadata. Ink 3 is also a trend chart's comparison series, always dashed.
- **Hairline** (line): row dividers, the section-tab bar's bottom edge, the tab bar's top edge, pressed-row fill.
- **Danger** (danger): something that has failed or run out. The failing status word on the device check, and a safeguarding item that has run out (2026-11-10, Claude's default while the owner slept): the Danger pill ("Run out" on the Safeguarding list, "Overdue" on the phone's training row), the flag notice on a person's Safeguarding tab (danger at 12% with danger text), and the blue card's status capsule (danger text on white). It is also the text of danger actions and a field's problem line, as Components describe. Due soon stays the marker (orange, or orange-ink text), so only what has already run out is red.
- **Selected Fill** (selected-fill): the chosen row in a list on the page, a tonal step (sand in light) rather than a shadow. Inside the workbench's sand list column the step goes the other way, to the segment colour (white in light), so the selected row still stands off its ground.
- **Fill on Surface** (fill-on-surface): a shaded box inside a panel; sand in light, strong sand in dark, so it never vanishes into a raised navy panel. The quiet pill, the chosen place, the layout picker's choices, the icon picker's cells, the Add a tile cards, and (2026-11-13, so they show in dark) avatars, row-icon tiles and fact icons. Fields and quiet buttons take Field instead.
- **Device Bezel** (device-bezel): the black ring and cutout of the phone frame; device hardware, never a UI colour.

### Dark scheme
- **Night** (night) is the page; **Night Surface** (night-surface) doubles as sand, so callouts and the player become raised navy panels, with **Night Surface Strong** (night-surface-strong) for tracks and placeholders.
- **Night Masthead** (night-masthead) keeps the masthead a step lighter than the page so it still reads as a masthead.
- **Bright Aqua** (bright-aqua) replaces navy as primary and replaces both aqua roles, with **On Bright Aqua** (on-bright-aqua) as its text. Orange stays the marker; **Night Orange Ink** (night-orange-ink) carries orange text.
- Warm off-white ink (night-ink, night-ink-2, night-ink-3) rather than pure white.
- **The date block in dark** is a raised navy tile (night-surface, the surface role) with its numerals and month in bright aqua (the primary role), its weekday in night-ink-3; it reads as a dark tile on the night masthead, not a white one. Under the yearly palette the same roles give a year surface with sage numerals.
- **Night Field** (night-field, #2d374d) and **Night Field Edge** (night-field-edge, night-ink-3 at 70%) are Field in the dark scheme.
- **Night Selected Fill** (night-selected-fill) is the selected row, the same raised step as a selected segment; Fill on Surface becomes **Night Fill on Surface** (night-fill-on-surface, the strong surface). Under the yearly palette in dark the selected row is **Year Night Selected Fill** (year-night-selected-fill) and fills on the panel use Year Night Surface Strong.

### 2026 yearly palette (seeded as a theme staff can change, 2026-11-09)
- **Year Green** (year-green) replaces navy as the masthead and live colour; **Year Green Deep** (year-green-deep) is primary; **Year Accent** (year-accent) is the text-on-light action colour.
- **Year Cream** (year-cream) is the masthead note; **Year Sand** and **Year Sand Strong** are the shaded boxes; green-tinted ink (year-ink, year-ink-2, year-ink-3) replaces navy-tinted ink.
- **Year Marker** (year-marker) and **Year Marker Ink** (year-marker-ink) replace orange as the marker.
- In dark, **Year Night**, **Year Night Surface**, **Year Night Masthead** and **Year Bright Sage** (with on-year-bright-sage) mirror the brand dark roles.

### Named Rules
**The Roles Not Hexes Rule.** Components use the semantic roles (masthead, primary, accent, live, marker, sand, ink, line) and never a palette hex, because every role is redefined by the scheme and the yearly theme. The only literal colours in components are the palette-picker swatches, the black video hero, and white on the play overlay.

**The Marker Rule.** Orange means "here" or "new": the active section, the unread item, how far you've listened, today on the week rail, an event changed since members last opened it, the tile chosen on the Hub canvas, the next set of Life Groups to fill. It is never a button fill and never decoration. The one ground it may fill is a Home tile in the Highlight or Pale highlight tone (`marker`, `marker-soft`), which staff choose for a tile they want to stand out; no tile ships in solid Highlight by default. The other is a calendar row that stands out (a one-off or a changed date), tinted marker at 14% in the app's week and month lists (owner, 2026-11-09), so it follows the theme as the marker does. On the canvas it also marks the chosen picture in the media chooser.

**The Aqua Acts Rule.** Aqua is for live state, for things you can act on (links, passages, the active tab), and for one short line of metadata at most (a series or recurrence line, a "Streamed on YouTube" tag). Navy is for the primary action itself. Don't use aqua for headings or running text.

**The Contrast Rule.** Text and placeholders hold 4.5:1 against what they sit on; icons and the edges of controls hold 3:1. The quiet inks (ink-2, ink-3) are worked out to read on the page, on shaded boxes and on fields, in every theme and both schemes. Nothing is faded with opacity to look quiet, past or disabled: it changes colour instead (Buttons, *Disabled*). Opacity is for images and covers, drag ghosts, the mark at 7%, and brief press and busy feedback, and never takes text below these numbers. App styling refuses a theme that breaks it: the theme check measures each text colour against what it sits on, in light and in dark, quiet and secondary text in shaded boxes and in fields, danger text in shaded boxes, the masthead's quieter text and the text on its buttons included.

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

### Named Rules
**The Dynamic Type Rule.** Every font size is in rem off the iOS body root. Never set a font size in px; the only px type is the 17px root fallback for platforms without `-apple-system-body`.

**The One Family Rule.** Montserrat is the only face on the page. The serif belongs to the yearly palette's masthead title and nowhere else; the monospace belongs to code (raw permission nodes in the developer roles tool, code a writer marks as code in formatted text, and the editor's library paths) and nowhere else.

**The Numbers Line Up Rule.** Times, durations, dates in the day rail and positions use tabular figures; so do the agenda's time column, the week rail, the month grid, the week band's range and an event's when line and date block.

## Layout

A single scrolling column per screen inside a fixed shell; the page scroller hides its scrollbar and contains overscroll. Content sits on a 20px side gutter. Sections are separated by 30px (22px for the first after the masthead), section heads by 12px, and stacks inside a section by 14px (22px for looser stacks). The page reserves the tab bar's height plus the bottom safe area plus 32px at its foot.

Every screen is built from the same frame: a 48px top bar (plus the top safe area) laid over the masthead, the masthead itself pulled up under the bar so its navy reaches the top of the screen, then optional sticky section tabs that stop under the top bar, then sections. Tab roots carry the bell in the top bar; pushed detail pages carry a back button labelled with the parent. A page appears at once, with no fade or slide: fading a page in let the masthead's navy, which sits behind every page for the overscroll, show through its content as a grey veil, and a page replaced by its loaded self played it twice.

Spacing comes from one scale in `tokens.css`: 2, 4, 6, 8, 12, 16, 20, 24, 32 and 48px (`--space-3xs` to `--space-4xl`), with named rhythms on top: the gutter (20px), the stack (14px), the section gap (30px, 22px for the first) and the gap under a tab strip (20px). New spacing uses them; the drift ratchet only lets raw pixel spacing go down.

Rows are at least 56px tall, interactive targets at least 44px. The tab bar is a fixed five-column grid with 48px items above the bottom safe area. Section anchors keep a scroll margin of the top safe area plus 100px so a jumped-to heading lands below the sticky bars.

**With the keyboard up** (owner's reports, 2026-11-11/12) the screen ends where the keyboard begins, and nothing moves on its own. The app follows the keyboard itself (Capacitor Keyboard with `resize: none`): it sets `keyboard-open` on the root and `--kb` to the keyboard's height, accessory bar included, and the shell shrinks to sit on it, so the status bar, the top bar and the masthead stay put and nothing slides under the clock. The tab bar and the preview banner step aside, the page's foot reserve drops to 32px, and the space behind the keyboard is page colour, never the masthead's navy. The focused field is always shown above the keyboard and above any foot bar. A sheet sits on the keyboard and is never taller than the space above it; it slides in first and only then focuses its search field. A foot bar (Send, Save) stays docked just above the keyboard with only its actions and any warning; its explanatory lines and quiet links come back when the keyboard goes. The chat composer docks on the keyboard the same way. On Android the window itself resizes, so `--kb` stays 0 and only the class applies. The rules live in their own block at the end of `ui.css`.

The calendar's Week view replaces section tabs with a sticky week band: the masthead ("This week", the Week and Month switch beside the title) ends 14px early and the band carries on in masthead navy, holding the beige date range and the seven-day week rail, and pins under the top bar while the days scroll beneath (the top bar turns solid after 90px). Under it, the calendar chips (14px down), then each day as a section 20px apart. Month keeps the masthead (the month as title, then a row of the year, the switch and the step buttons) over the chips, a 7-column grid padded 12 by 14px, its key, and the chosen day's agenda. An event page has no masthead of its own: its navy head (date block and words) sits under a solid top bar, then a 16px-gutter body. Prayer, More and the settings pages are short enough to need neither tabs nor a band.

### Named Rules
**The Sidebar Offset Rule.** Admin breakpoints are window media queries offset by the sidebar's 248px; the workbench's own steps are 1360px and 760px of window. Never make the work area a size container: it becomes the containing block for fixed elements and traps sheets and overlays inside it. A component may still use a container on its own inner list (as the workbench's list pane and the save bar do).

**The Masthead First Rule.** Every tab root and every settings page opens on a masthead with a title; long, sectioned pages add sticky section tabs under it. Detail pages with a picture lead with the picture instead (a 16:9 hero under a solid navy top bar). The admin panel has no masthead: its navy is the sidebar, and every page opens on a title band.

**The Still Frame Rule.** A page that shows one thing at a time keeps its frame while what it shows changes. App styling's title band keeps its 72px whether it carries the sentence or a draft's save line, its buttons hold their places with the Drafts menu always last at the far right, and the phone and the 400px panel keep their widths whether you look at what everyone sees, the colours, Home or the tab bar; choosing a part changes only what is in the panel. The media library keeps its bars above the split view for the same reason.

**The One Column Rule.** No grids of tiles on a tab root other than Home's Hub, and no uniform icon grid anywhere: the Hub's tiles differ in size, tone and shape. Beyond the Hub, the only multi-column layouts are the two-up series grid on Listen and the day rail. On the desk the admin panel adds its shell, the day rail (a week or a fortnight) and the office week's two columns, the workbench (the split view included), the announcements desk's writing and proof columns, the Places explorer's directory and phone, App styling's phone and panel, a board's columns and its docked ticket (and a ticket page's main column and side), and the media library's folder tiles and picture grid (the folders and files themselves, not tools), and still no grid of tool tiles.

## Elevation & Depth

Mostly flat and tonal. Depth comes from colour steps (navy masthead over white page, sand boxes on white, raised navy surfaces in dark) and from frosted bars, not from shadows. The section-tab bar, the event editor's foot bar and the tab bar are 90% opaque page colour with a backdrop blur (18px, 18px and 20px, saturated) so content scrolls visibly beneath them, edged by a hairline rather than a shadow. The calendar's week band is the masthead carried on as a sticky band: solid navy, no blur, no shadow.

### Shadow Vocabulary
- **Lift** (`0 1px 2px rgb(24 35 58 / 0.06), 0 10px 24px -14px rgb(24 35 58 / 0.3)`; in dark `0 1px 0 rgb(255 255 255 / 0.04) inset, 0 14px 28px -18px rgb(0 0 0 / 0.7)`): boxed lists and the event feature card only.
- **Segment** (`0 1px 3px rgb(0 0 0 / 0.14)`): the selected segment of a segmented control.
- **Knob** (`0 2px 6px rgb(0 0 0 / 0.2)`): the switch thumb.
- **Play overlay** (`0 10px 30px rgb(0 0 0 / 0.35)`): the round play button over a video hero.
- **Badge ring** (`0 0 0 2px` masthead colour): separates the unread badge from the bell.
- **Pop** (`0 0 0 1px` hairline, `0 18px 36px -16px` scrim; `0 18px 40px -12px` for the Account menu and the date and time popovers): menus, popovers and the chip picker's matches, anything that opens over the page from a button.
- **Drag lift** (`filter: drop-shadow(0 18px 26px` ink at 30%`)`, with the tile scaled to 1.04 while it moves): a Home tile while it is being moved or resized on the admin canvas, nowhere else.

### Named Rules
**The Flat Page Rule.** Rows, callouts, the sermon head and the player sit flat on the page; only a boxed list, a picture card, a Home tile in the Card tone (`raised`) may lift. If something needs separating, give it sand, not a shadow. In the admin panel the only lifted things are the sign-in card, account panels, settings groups (a hairline ring and the lift), the selection tray (docked over a list that scrolls beneath it), menus and popovers (the Pop shadow) and the Feedback board's ticket while it floats over the board under 1228px; tables, the office week, board columns and the workbench's record pane are flat, separated by hairlines and sand, and today on the rail is an inset 2px primary ring, not a shadow. The chosen capsule on the rail, or the current "Not sent yet" chip, is a 2px marker outline at a 2px offset.

**The Tonal Selection Rule.** A selected row is marked by a fill one tonal step off its ground (Selected Fill, rounded to the field radius and widened 12px into the gutter on each side, its hairline hidden), never by a shadow or a border. A sand leading tile on a selected row takes the page colour (white in light, night in dark) so it still reads as a tile; an accent tile keeps its tint.

## Shapes

Soft, rounded, never sharp. The working radius is 14px for anything box-shaped (callouts, sermon head, player, notes, boxed lists, feature cards, series covers, full-width buttons); 12px for the smaller boxes (status strip, fields, segmented track, now-playing art); 10px for tiles inside rows (row icons and thumbnails); 9px for a selected segment inside its track. Anything sized to a touch target is a capsule: buttons at 44px tall with a 22px radius, chips, passage chips, the pray button, pills, round icon buttons and the play toggle. Sheets round only their top corners at 22px and carry a 38 by 5px grabber. Speaker photos are circles. The calendar's date block is a 78px tile at the 14px radius (62px at 12px when compact); week-rail days and month-grid days round at 12px; month marks are 5px dots, and "not on" is a 6px open ring. Home tiles take the 14px radius and clip what they hold (a picture, the bleeding mark); their icons sit in the 34px, 10px-radius row tile. The blue card takes the 14px radius and clips a ring at its top right: a 150px circle drawn as an 18px stroke of the masthead chip colour, set 44px out past the top and right edges; its status is a 24px capsule.

The admin panel uses the same scale: sidebar links, area heads, pin buttons and the Jump to field, Jump to's rows, sortable header buttons and office-week capsules at 9px; the rail's day columns at 10px inside its 14px sand track; fields, the search field and the chosen-place token at 12px; filter chips and count capsules as capsules; media thumbnails at 8px, like the app's. A dashed line means "you can change this": the upload drop zone (1.5px of ink-3 at 45% on fill-on-surface at the 14px radius, turning deep aqua on hover or focus), and the same frame on the phone when Send feedback has nothing chosen yet. The event head is never outlined: it previews, and fields edit (owner, 2026-11-08). Folder tiles and the media grid's thumbnails take the 12px field radius (folder tiles with a hairline border, their icon tile at 9px); breadcrumbs and folder-picker rows round at 9px; the selection tray takes the 14px radius. Menus and their items round at 12px and 9px, the Account menu and the date popover at 14px, its days and times at 9px. Board columns take 14px and their cards 10px; a filter in the rail is a 9px row, not a capsule. In the picture viewer the controls are 44px circles and the filmstrip's 56px thumbnails round at 9px.

The phone frame carries device geometry, not UI shapes: a 46px screen radius inside a 9px bezel ring, a Dynamic Island capsule (124 by 36px) or a notch (160 by 32px, its bottom corners at 20px). The lock-screen proof draws a lock screen the same way: a 3.2rem tabular clock, and a 20px app icon at a 28% radius. These match the hardware being drawn; they are not part of the radius scale and never appear on a UI box.

The church mark appears only as a mask of the supplied artwork, recoloured with the current text colour: at 7% opacity bleeding off the masthead's right edge, at 10% inside fallback covers, and in the footer and boot screen.

## Components

The detail for each component and feature screen is in [docs/design/](docs/design/), one file per area. Read the
file for what you touch:

| File | Covers |
|---|---|
| [controls.md](docs/design/controls.md) | Buttons, chips, segmented controls, stages, fields, menus, ordering, settings groups, dates, the picture viewer, boards, tables, charts, breadcrumbs, the selection tray |
| [lists-and-cards.md](docs/design/lists-and-cards.md) | Rows of every kind, boxed lists, facts, callouts, covers and cards |
| [saving.md](docs/design/saving.md) | The two kinds of form, where the save goes, instant changes, leaving with changes |
| [app-shell.md](docs/design/app-shell.md) | The phone's top bar, tab bar, bell, masthead, section tabs, sheets, empty states, sign-in, previews, the sermon player |
| [admin-shell.md](docs/design/admin-shell.md) | The admin panel's layout and breakpoints, the workbench, sidebar, Jump to, title band, office week |
| [home.md](docs/design/home.md) | The Hub, its tiles and their layouts, the Hub and Tab bar canvases |
| [calendar.md](docs/design/calendar.md) | The week, the month, an event and editing it |
| [posts.md](docs/design/posts.md) | The news page, a post's page, writing a post, formatted text, the news desk, the writing page, the digest |
| [ministries.md](docs/design/ministries.md) | A ministry's page, leading it, its roster, the Ministries desk, Life Groups |
| [people.md](docs/design/people.md) | A person's page and roles, safeguarding, finding someone, families, the blue card |
| [prayer.md](docs/design/prayer.md) | The Prayer desk, its settings, a prayer request's page |
| [chats.md](docs/design/chats.md) | The chats desk and checking a chat |
| [feedback.md](docs/design/feedback.md) | Send feedback on both sides, the Feedback board |
| [media.md](docs/design/media.md) | The media library |
| [places-and-styling.md](docs/design/places-and-styling.md) | Places in the app, App styling |
| [components.md](docs/design/components.md) | Which component draws each part, with its main parameters, and where the styles live |

### Examples in copy
**Copy never names a real ministry, group, room or person as an example** (owner, 2026-11-10: "i don't really want direct mentions of it in the frontend as being as/for kids life. there's lots of references and such of stuff like that"). Placeholders, hints, option details and empty states stay generic ("Documents and links for its people"), or use the ministry's own name and data when the words are about that ministry ("{Name}'s leaders lead these too"). A field with no generic example has no placeholder. Sample data (Development only) may use real-sounding names; nothing on screen depends on it.

### Motion
One curve, `cubic-bezier(0.22, 1, 0.36, 1)`, for presses, sheets and the section-tab underline. Pages appear at once (no entry fade or slide), and a screen opened again draws the data it last showed at once (`LiveData`'s `Remember`), refreshing it in place. Presses scale (0.94 to 0.98). The live dot pulses on a 1.6s cycle. A paged sheet slides a new page 14px in from the side it comes from (260ms), and the current page dot widens over 240ms. In the admin panel the sidebar's marker bar scales in over 240ms, an area's chevron turns over 200ms while its pages fade 4px into place over 200ms, hovers and fills ease over 160ms (120ms for table rows), and sort arrows fade over 160ms. Week-rail days fill over 160ms and month-grid days over 140ms; the rich text toolbar's buttons ease their fill over 140ms. A swipe from the left edge of a pushed page drags it away and goes back. On the Hub canvas tiles glide to new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)` (a curve of their own), and a dropped tile settles on a gentle `linear()` spring. Reduced motion collapses every animation and transition to 1ms, in the app and the admin panel alike; on the Hub canvas the drag still works but tiles move without gliding or lifting.

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
- **Do** open every admin page on a title band, and work an area in the workbench: filters in the sand rail, the list, the record ruled off beside it.
- **Do** put Cancel bottom left and Save bottom right of anything that saves, with its status on its own line above.
- **Do** put Delete and every other action on an item in its ⋯ menu, and ask once before deleting, saying what goes with it.
- **Do** offset admin breakpoints by the 248px sidebar.
- **Do** let staff choose what something opens with the place field and its directory, never by typing an address.
- **Do** give a Home tile one of the eight theme tones, and draw it in a layout its kind declares for its size.
- **Do** mark the chosen tile on the Hub canvas with the 2px marker ring, its size capsule in words and the corner grip, and warn in plain words only when a tone's text is hard to read in light or dark.
- **Do** grey out a layout that doesn't suit a tile and say why, rather than hiding it.
- **Do** keep a regular event quiet in the agenda and let only a special, a change or a date that isn't on speak, with a date not on said in a line under its title.
- **Do** edit an event where it is read: the same head as a live preview, every change in a field below it, and the save in a foot bar; edit a regular event on its own page, from its run of dates.
- **Do** mark whatever the app owns with the lock glyph and one plain caption, and keep a page's bars and path row above its list so choosing an item never moves the list.
- **Do** show a safeguarding item that has run out in danger and one due soon in the marker, and never let either block anyone.
- **Do** keep a page that shows one thing at a time in one frame (the Still Frame Rule), and put a phone preview's choices under it as one sentence of sand chips that change only the phone.
- **Do** open every post on the 3px navy rule with its date block and close it on the sand foot; a split between cards repeats the navy rule, never sand, because sand at the end of a post means it is over.

### Don't:
- **Don't** lay a tab root other than Home out as a grid of tiles, and don't let the Hub become a uniform grid of identical icon squares or a feed of identical cards.
- **Don't** float buttons over the reading column; actions belong in the top bar, in a section, or in a sheet.
- **Don't** use orange as a button fill or decoration; it marks position and newness only.
- **Don't** add shadows to rows, callouts or the player; only boxed lists and picture cards lift.
- **Don't** use DM Serif Text anywhere but the yearly palette's masthead title.
- **Don't** redraw or restyle the church mark beyond recolouring the supplied artwork.
- **Don't** set font sizes in px.
- **Don't** put a phone masthead in the admin panel, or open it on a grid of tool tiles.
- **Don't** box the workbench's record pane as a card; it is a hairline-ruled column on the page.
- **Don't** put a red Delete button in a foot, a save bar or on the page.
- **Don't** use the browser's own date or time input in the admin panel, or build a second copy of a pattern the map already has.
- **Don't** make the admin work area a size container.
- **Don't** mark a date that isn't on with a suffix on its title or by dropping it from the list.
