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

How to read this guide: the **Pattern map** first, then the rules for the area you touch. The detail for each
component and screen is in [docs/design/](docs/design/) (see *Components*), and every shared component is live in the
gallery (`/gallery` on both sides). The tokens above are the source impeccable reads.

## Pattern map: one way to do each job

Read this before building any screen. Each job has one shared component and one placement (owner, 2026-11-12;
`.impeccable/mocks/design-system/`). The admin panel is designed for a desk and the app for a phone: they need not
look alike, only do the same job with the same data. A small difference between pages is a parameter on the shared
component, never a copy.

| Job | Use | Where things go |
|---|---|---|
| A list and the record chosen from it (admin) | `Workbench` | Filters in a sand rail on the left only when the page has filters; the list in the middle; the record ruled off on the right, beside the list at every desktop width. At 1360px and under the rail moves above the list, or, with `FoldAside`, steps aside while a record is open. A page without a rail uses `SplitView` (the same workbench). The page's one add action sits in the `TitleBand`, top right. |
| Chatting at a desk (admin) | `ChatsList` + `ChatView InPanel` + `ChatPeople` (`ChatFrame`) | Three panes: your chats (the open one `ListRow Current`), the conversation with the `Composer` at its foot, and *Chat settings* ruled off on the right, shown and hidden by the panel arrow at the conversation's head (`AppIcons.ShowSidePane` / `HideSidePane`; owner, 2026-11-13, #77). At 1200px and under the list steps aside while settings show; at 900px and under, one pane at a time. The phone keeps the cog to the chat's settings page. Starting a chat stays in a ministry's Chats tab. |
| A record's heading | `PaneHead` | Title and a one-line lead; its ⋯ menu (`Actions`) holds Delete and every other action on the item; Open as a page and Close at the right. |
| Saving | `SaveBar` (+ `UnsavedGuard`) | **Cancel bottom left, Save bottom right** of anything that saves, on both sides. The status line or the reason it can't save sits on its own line above, at the right (centred over a full-width save), never between the buttons. Other actions sit after Cancel, moving to their own line above when the bar is 520px or narrower. A create form (`New`) never says it has unsaved changes. See *Saving*. |
| The keyboard (phone) | `Composer`, and `RichTextField`'s format bar | Only a composer (typing and sending are one act) and the format bar of the text you're writing in dock on the keyboard. Both hide iOS's Done strip, so the format bar ends with a pinned *Hide the keyboard* and a tap outside a composer lowers it (#99). A form's save bar stays at the foot of the page and the keyboard covers it (owner, 2026-11-12, #65). `DesignDriftRules` fails on anything else docked at the foot. |
| Deleting | the item's ⋯ menu, then a `Sheet` | Delete is an action on the item, never a red button in a foot or on the page. Exception: a thing with a settings page or sheet (a cog) keeps Delete at the foot of its settings and has no ⋯ beside the cog (owner, 2026-11-12, #64). In a `Sheet` that edits one item, its ⋯ sits in the heading (`Tools`). It asks once, saying what goes with it; the red action is listed first (admin: at the right, Keep it at the far left; phone: stacked on top). |
| Adding | the record pane | New opens a blank record, already chosen, in the record pane; Cancel takes it away. On the phone it is a page with the same fields. |
| A group of settings | `Section Grouped` (+ `Foot`) | Admin: a card the full width of its pane, short fields two to a line (`ui-pair--wide`; owner, 2026-11-12, #71), title and lead inside, a sand foot for its save bar or "These change straight away". Phone: an inset group. A Save-button form and switches that change at once never share a group. |
| Switching views | `TabStrip`, `Segmented`, `FilterChips` | Tabs (the underline) for different things; Segmented for the same list two ways; chips only to narrow down. Chips never switch views; Segmented never picks what tabs should show. |
| Tools for a list | `Workbench` rail with `FilterChips Rail` (admin); main button + `Menu` (phone) | Search and filter groups live in the rail, one `FilterChips Rail` per group, with counts. On the phone the main action is one wide button and the rest go in its ⋯ `Menu`. |
| More actions | `Menu` | One ⋯ button; its panel is always whole on screen (#69); items made with `this.Item(...)` (`MenuItems`) say what they do; Delete last and red, after a rule (`startsGroup`). A hand-built panel uses `MenuButton` rows. |
| Actions on a row in a list | `ListRow` with `Actions` | The row opens the thing; its ⋯ sits beside the row, never inside its link. Delete or Unlink last and red, asking once in a `Sheet` (#50). |
| Putting things in order | `ReorderList` | Drag by the handle (touch or mouse); ⋯ has Move up and Move down; each move applies at once and shows the moment it's dropped (#58, #62). `Quiet` (a short list in a narrow column): handle on hover or focus, Alt ↑ ↓, no ⋯, a caption saying how. An optional preview follows the drag (owner, 2026-11-12). |
| A small panel from a button | `Popover` | On the menu's shadow; Escape or a click outside closes it. The Account menu is one. |
| One role of several | `RoleMenu` | A button showing the role; its menu says what each allows, Remove in red last. Applies at once. Several roles at once stay ticks (`RoleTick`). |
| Steps something moves through | `StagePipeline` | Done steps ticked, the current one solid, the next waiting; press a step to move there. `ReadOnly` for someone who can see but not move it. |
| Boards | `Board` + `BoardCard` | Sand columns with a count, a dashed sentence when empty, optional "Add one here", a folded column for what's put aside. |
| Taking a file | `DropBox` (`FilePick Box`) | Every place that takes a file, on both sides: a dashed box saying what it takes over how ("Or click to choose one from this computer"); drop, click, or paste where the page allows. On the phone it opens the photo library or files. Never a link-like row (owner, 2026-11-12, #66). |
| Writing a message | `Composer` | Docked at the foot of a full-height page: attach (`Attach` slot, `AppIcons.Attach`) left, the field, a round Send right; what's attached in `Above`. Send keeps the focus in the field, so the phone's keyboard stays up (owner, 2026-11-13, #82). Admin chats take `EnterSends`. Chats and Send feedback use it. |
| Days marked on a month | `MonthDays` | Day tiles, Monday first, ‹ ›, a caption key; marks for all day, mornings, evenings, some hours, skipped. Days away use it. |
| One rule of a few, each needing a sentence | `Segmented Rows` | Radio rows in a settings group, each with a detail line; a sentence after says what follows. |
| Dates and times | `DateField`, `TimeField`, `DateTimeField` | Admin: type it ("15 Nov", "7:30pm") or open the calendar or times (every 15 minutes); what was understood reads back with a check; month and year headings jump far (#70). Today and Sundays only where they make sense (never on a birth date, `Shortcuts="false"`). Phone: the native wheel. Never `Type="date"` or `Type="time"` in a feature. |
| A picture, bigger | `PictureOpen` + `PictureViewer` | The whole window on near-black, arrows, a filmstrip, Download. Never squeeze a picture into a pane. |
| A message and its reactions | `MessageBubble` + `BubbleReactions` | Counted chips under the bubble (yours ringed in deep aqua); photos open in the picture viewer; a quote scrolls to the original. Admin: actions from a ⋯ beside the bubble as a `Popover` (#76). Typed Markdown shows formatted (the chat scope); the composer has no format bar (owner, 2026-11-14). Detail: [chats.md](docs/design/chats.md). |
| An action inside a line of text | `Button Variant="Link"` | Deep-aqua words at 600, no fill, in the text's size ("Try again · Delete"). An action that stands alone is a real button. |
| A page's top-bar actions (phone) | `Button Variant="Quiet"` icon, `Menu` | Icon buttons take the bar's colour. A cog opens the settings of what you're looking at, with Delete at their foot; a ⋯ `Menu` only for something with no settings, never beside a cog (#64). |
| Icons | `AppIcons` (Platform.Contracts) | One icon per meaning, named for the thing; a cog always means "the settings of what you're looking at"; the eye-off (`AppIcons.Hidden`) means hidden from everyone else, never a preview (that is the eye, `AppIcons.Previews`). |
| A page's count in the admin sidebar | `INavCounts` (Platform.Client) | A feature counts what waits for you and names the feeds that change it; the sidebar shows the total in the link's orange capsule. |
| Light or dark | `SchemeChoice` | Phone: Appearance. Admin: the Account menu. |
| A page of news | `NewsList` (+ `NewsStory`) | Today's urgent first on a marker band, then the newest post leading with its 16:9 picture, then rows with the picture at the side (owner, 2026-11-13). Church news and *Your ministries* use it. |
| Reading one post | `PostView` (+ `MediaStrip`), under `TopBarStyle.SolidTitleOnScroll` | The bulletin item, the same for every post: navy rule, date block beside the title, the pictures it opens with, the first paragraph at lead size, splits between cards on another navy rule, and a sand foot that ends it. Detail: [posts.md](docs/design/posts.md). |
| Writing a post | `PostEditor` (+ `WritingDesk` in the admin panel) | A title and one `RichTextField` holding the whole post, pictures and all in place (owner, 2026-11-14). Phone: a full-screen page. Admin: a page of its own (`WritingDesk`), never the record pane (owner, 2026-11-13, #83). Files upload with the save. |
| Formatted text | `RichTextField` | Live Markdown with one bar of quiet icons and a navy **Insert** at the right; each place says what it allows with `RichTextMedia`. Media and cards in place, each with a pill of tools above it (owner, 2026-11-13, 2026-11-14, #92, #93). `RichTextEditor` is the older field until places move (events moved, 2026-11-14). Detail: [posts.md](docs/design/posts.md), *Formatted text*. |
| Something in the app inside a post | `AppCardFrame` (+ a feature's card via `AddAppCardView`) | An event, sermon, ministry or Life Group as a card in the writer's pick of *Side rule* (default), *Timetable line* or *Box* (owner, 2026-11-14), filled from the synced store; inserted from Insert → *From the app* (`AppCardPicker`). |
| Writing on a page (admin) | `WritingDesk` | The work as a white page on sand (up to 800px) with Write and Preview on a phone tabs; settings in a 360px column on the right that folds; the save bar at the column's foot (owner, 2026-11-13). |
| Spacing | `--space-*`, `--section`, `--section-first`, `--stack`, `--gutter`, `--after-tabs` | No new raw pixel spacing. |

**Rules for the workflow** (every station):

- Start from this map and the gallery. A job that isn't here is a new pattern: build it once in Platform.Ui, add it to
  the gallery and this map in the same change, and run it past the owner on the decision page first.
- `DesignDriftRules` and `IconRules` hold the map: every class in markup exists in a stylesheet; raw spacing, raw
  `<button>`, native date fields, quiet-first dialogs, `SplitView` and red delete buttons outside a confirm only go
  down (`design-ratchets.json`; lower an entry when its count drops, never raise one); every public component appears in
  the gallery; one icon per meaning.
- A screen still built the old way moves onto its component the next time anyone touches it.

## Overview

**Creative North Star: "The Sunday Bulletin"**

The app reads like the church's printed bulletin on a phone: each tab
root opens on a navy masthead (a large title, a beige note line, the church mark's ring at 7% bleeding off the right),
then a white page as one readable column, with sand where the bulletin would shade a box. Long sectioned pages (Sunday,
Listen) carry sticky section tabs with a sliding orange underline. Calm density: a 20px gutter, hairline-divided rows
rather than card stacks, type that follows the phone's text size. It rejects the generic church-app template: no
uniform grid of icon squares, no feed of identical cards. Home is the one tile layout (the Hub, owner, 2026-10-27): tiles
the church arranges, differing in size, tone and shape, like the bulletin's front page.

Light is the default; a dark scheme follows the system or Appearance. Every palette, the brand and the 2026 yearly theme
(the website's greens, a serif masthead) included, is a theme staff choose and change on App styling, so what everyone
sees is data, not a release.

The admin panel is the same bulletin at the office desk: a navy sidebar in the masthead's place, a white work area that
opens with a title band, and the office week as its home. Areas are worked in one workbench. It follows the computer's
light or dark until someone picks one in the Account menu.

**Key Characteristics:**
- A navy masthead on every tab root, the mark's ring at 7%; a navy top bar, transparent over it, solid once past.
- Sticky section tabs with a sliding orange underline on long sectioned pages.
- A white page, sand callouts, hairline-divided rows; shadows only on boxed lists and picture cards.
- Montserrat at 400, 600 and 700, sized in rem off the phone's text size.
- Home as the Hub: a 2 to 4 column grid of tiles in theme tones, sized and arranged by the church.
- The calendar as one running diary: regular events quiet, only what's different speaks.
- In the admin panel, a 248px navy sidebar with an orange bar on the current page and orange counts, over a white work
  area with a title band.
- One way per job (the pattern map), the same on the phone and the desk.

## Colors

Detail of where each role is used: [foundations.md](docs/design/foundations.md), *Colours*.

- **Navy** (masthead, primary): the masthead, the stuck top bar, primary buttons, selected chips. **Bright aqua** takes
  primary in dark.
- **Aqua** (live) for live state; **deep aqua** (accent) for things you can act on, on white and sand: links, the
  active tab, focus rings, a series line.
- **Orange** (marker) for "here" and "new"; **orange ink** for orange text.
- **Sand** and **strong sand**: the bulletin's shaded boxes; **Field** (sand fill with a 1px field edge; owner,
  2026-11-11) for what you type into or set; **Fill on Surface** for a shaded box inside a panel; **Selected Fill** for a
  chosen row.
- **Ink**, **ink 2**, **ink 3**: headings, then prose, then hints and metadata. **Hairline** (line) divides rows.
- **Danger**: only what has failed or run out (a run-out safeguarding item, a failing check, danger actions, a field's
  problem line). Due soon stays the marker.
- **Beige**: text on navy only.

### Named Rules
**The Roles Not Hexes Rule.** Components use the semantic roles, never a palette hex: every role is redefined by
`data-scheme` (light, dark) and `data-theme` (a staff theme, or `brand` before one arrives), and a preview frame resets
them. The only literal colours in components are the palette-picker swatches, the black video hero and white on the play
overlay. `DesignTokenRules` fails on a literal colour outside `tokens.css`.

**The Marker Rule.** Orange means "here" or "new": the active section, the unread item, how far you've listened, today,
an event changed since you last looked, the chosen tile on the Hub canvas. It is never a button fill or decoration. The
grounds it may fill: a Home tile staff set to Highlight or Pale highlight (none ships so), and a calendar row that
stands out, tinted at 14% in the app's lists (owner, 2026-11-09).

**The Aqua Acts Rule.** Aqua is for live state, things you can act on, and one short line of metadata at most. Navy is
the primary action itself. No aqua headings or running text.

**The Contrast Rule.** Text and placeholders hold 4.5:1 on what they sit on; icons and control edges 3:1, in every
theme and both schemes. Nothing is faded with opacity to look quiet, past or disabled: it changes colour (disabled is an
outline; owner, 2026-11-11). Opacity is for images, covers, drag ghosts, the mark at 7% and brief press feedback. App
styling refuses a theme that breaks it.

## Typography

Montserrat throughout at 400, 600 and 700: Bold titles, Regular reading, 600 for labels and row titles; tight negative
tracking on big sizes. DM Serif Text only for the yearly theme's masthead title. Monospace (`--font-code`) only for code.
The root is the iOS body text style (17px fallback) and every size is a rem step of it, so Dynamic Type scales the whole
app; the admin panel keeps the 17px root. Steps (frontmatter `typography`): Display (masthead only), Headline (detail
titles, the admin title band), Title Large (sheets), Title (section heads), Body (prose at 1.62, 65ch), Row title,
Label, Caption, Micro, Date numeral, Pill. Detail: [foundations.md](docs/design/foundations.md), *Typography*.

### Named Rules
**The Dynamic Type Rule.** Every font size is in rem off the iOS body root; never px (the root fallback aside).
`DesignTokenRules` enforces it.

**The One Family Rule.** Montserrat is the only face on the page; the serif is the yearly masthead's, the monospace is
code's.

**The Numbers Line Up Rule.** Times, durations, dates, positions and counts use tabular figures.

## Layout

The phone: one scrolling column in a fixed shell, a 20px gutter, sections 30px apart (22px after the masthead), rows at
least 56px, targets at least 44px. Every screen is the same frame: a 48px top bar over the masthead (navy to the top of
the screen), optional sticky section tabs, then sections; tab roots carry the bell, pushed pages a back button named for
the parent. A page appears at once, with no fade or slide. Spacing comes from one scale in `tokens.css` (`--space-*` and
the named rhythms); raw pixel spacing only goes down.

With the keyboard up the screen ends where the keyboard begins and nothing moves by itself: the bars and masthead stay
put, the tab bar steps aside, the focused field shows above the keyboard and any foot bar, and a sheet sits on it
(owner, 2026-11-11/12). Detail: [app-shell.md](docs/design/app-shell.md) and [foundations.md](docs/design/foundations.md).

The admin panel: a 248px navy sidebar beside a white work area that opens on a title band, capped at 1440px; full-bleed
views (the workbench, previews, sign-in) run edge to edge. At 900px and under the sidebar becomes a top strip with a
Menu button. Page layouts and breakpoints: [admin-shell.md](docs/design/admin-shell.md).

### Named Rules
**The Sidebar Offset Rule.** Admin breakpoints are window media queries offset by the 248px sidebar; the workbench's own
steps are 1360px and 760px of window. Never make the work area a size container (it traps sheets and overlays); a
component may use one on its own inner list.

**The Masthead First Rule.** Every tab root and settings page opens on a masthead with a title; long sectioned pages add
sticky section tabs. A detail page with a picture leads with it (16:9 under a solid top bar). The admin panel has no
masthead: every page opens on a title band.

**The Still Frame Rule.** A page that shows one thing at a time keeps its frame while what it shows changes: App
styling's title band, buttons, phone and panel hold their places and sizes; the media library keeps its bars above the
split view.

**The One Column Rule.** No grid of tiles on a tab root but Home's Hub, and no uniform icon grid anywhere. Other
multi-column layouts: the series grid on Listen, the day rail, and on the desk the shell, the office week, the
workbench, the announcements desk, Places, App styling, boards and the media grid; never a grid of tool tiles.

## Elevation & Depth

Mostly flat and tonal: depth comes from colour steps (navy over white, sand on white, raised navy in dark) and frosted
bars (90% page colour with a backdrop blur, edged by a hairline), not shadows. The shadows, in the frontmatter and
[foundations.md](docs/design/foundations.md): Lift, Segment, Knob, Play overlay, Badge ring, Pop (menus and popovers),
Drag lift (a Home tile on the canvas only).

### Named Rules
**The Flat Page Rule.** Rows, callouts, the sermon head and the player sit flat; only a boxed list, a picture card and a
Card-toned Home tile lift. Need separation? Use sand, not a shadow. In the admin panel only the sign-in card, account
panels, settings groups, the selection tray, menus and popovers, and the floating Feedback ticket lift; tables, the
office week, board columns and the record pane are flat.

**The Tonal Selection Rule.** A selected row is a fill one tonal step off its ground (Selected Fill, at the field
radius, widened 12px each side, its hairline hidden), never a shadow or border.

## Shapes

Soft, never sharp. The radius scale (frontmatter `rounded`): 14px for boxes (callouts, cards, boxed lists, full-width
buttons, Home tiles, sheets' content), 12px for smaller boxes and fields, 10px for tiles inside rows, 9px for items
inside a track or menu; anything sized to a touch target is a capsule. Sheets round their top corners at 22px. A dashed
line means "you can drop or add something here" (the drop box). The phone frame's device geometry is never a UI shape.
The church mark appears only as a mask of the supplied artwork, recoloured, never redrawn. Detail:
[foundations.md](docs/design/foundations.md), *Shapes*.

## Components

The detail for each component and feature screen is in [docs/design/](docs/design/), one file per area. Read the
file for what you touch:

| File | Covers |
|---|---|
| [foundations.md](docs/design/foundations.md) | Where each colour role is used, the type steps, layout measures, shadows, shapes, motion |
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

### Saving
A form with unsaved changes always asks before they're lost (owner, 2026-11-10). There are two kinds, and a person can
always tell which they're on:

- **Saves by itself** (long work with its own publish or send step: App styling drafts, an announcement, the digest):
  `SaveStatus` near its title ("Saving…", "Saved", "Couldn't save. Trying again by itself." with *Try now*, or why it
  can't save yet). No Save button.
- **Saves with a button** (everything else): one `SaveBar`, as the pattern map places it, its button named for what it
  does ("Save", "Add the ministry"). Can't save yet: the button is disabled and the reason is written above it, never
  only in a tooltip.
- **Instant changes** (a switch, a role tick, linking a person) need no Save; they sit in their own group, whose foot
  says "These change straight away", never beside a Save button.
- **Leaving:** `UnsavedGuard` asks (*Save and go on*, *Discard the changes*, *Stay here*); a screen that swaps what's open
  without navigating asks `UnsavedChanges.MayLeaveAsync()` first. `SaveRules` enforces it.

Where the save goes on each surface: [saving.md](docs/design/saving.md).

### Examples in copy
**Copy never names a real ministry, group, room or person as an example** (owner, 2026-11-10). Placeholders, hints,
option details and empty states stay generic ("Documents and links for its people"), or use the ministry's own name
and data when the words are about it. A field with no generic example has no placeholder. Sample data (Development
only) may use real-sounding names; nothing on screen depends on it.

### Motion
One curve, `cubic-bezier(0.22, 1, 0.36, 1)`, for presses, sheets and the section-tab underline. Pages appear at once,
and a screen opened again draws what it last showed at once (`LiveData`'s `Remember`), refreshing in place. Presses
scale (0.94 to 0.98) with a haptic. Reduced motion collapses every animation to 1ms, in the app and the admin panel; on
the Hub canvas the drag still works without gliding. Timings: [foundations.md](docs/design/foundations.md), *Motion*.

## Do's and Don'ts

### Do:
- **Do** open every tab root on a masthead, and every admin page on a title band.
- **Do** build content as hairline-divided rows on the white page, with sand for what the bulletin would shade.
- **Do** read colours through the roles, size type in rem, and keep targets at 44px with a press scale and a haptic.
- **Do** work an admin area in the workbench, with Cancel bottom left and Save bottom right, and Delete in the item's ⋯
  menu, asking once and saying what goes with it.
- **Do** let staff choose what something opens with the place field and its directory, never by typing an address.
- **Do** give a Home tile one of the eight theme tones and a layout its kind declares; grey out a layout that doesn't
  suit and say why.
- **Do** keep a regular event quiet in the agenda and let only a special, a change or a date that isn't on speak, a date
  not on said in a line under its title.
- **Do** edit an event where it is read: the same head as a live preview, fields below, the save in a foot bar.
- **Do** mark whatever the app owns with the lock glyph and one plain caption.
- **Do** show a safeguarding item that has run out in danger and one due soon in the marker, and never let either block
  anyone.
- **Do** open every post on the navy rule and close it on the sand foot; a split between cards repeats the navy rule,
  never sand, because sand at the end means it is over.

### Don't:
- **Don't** float buttons over the reading column; actions belong in the top bar, a section or a sheet.
- **Don't** use orange as a button fill or decoration, or add shadows to rows, callouts or the player.
- **Don't** use DM Serif Text beyond the yearly masthead, set font sizes in px, or redraw the church mark.
- **Don't** put a phone masthead in the admin panel, open it on tool tiles, box the record pane as a card, or make the
  work area a size container.
- **Don't** put a red Delete button in a foot, a save bar or on the page.
- **Don't** use the browser's own date or time input, or build a second copy of a pattern the map already has.
- **Don't** mark a date that isn't on with a suffix on its title or by dropping it from the list.
