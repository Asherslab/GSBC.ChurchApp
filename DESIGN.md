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
  post-view-band:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.white}"
    padding: "20px 20px 24px"
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
| Reading one post | `PostView` (+ `MediaStrip`) | With two or more pictures or videos, a swipeable strip leads flush under the top bar ("1 of 3", dots). With no strip and no picture opening the post, the title sits on a navy band that continues the top bar (`Band`; *default, owner to review*, #88); a post that opens with a picture has no band. Then who it's from and when, the text with real paragraph spacing and type scale, a sand bar across the page between cards (`===`), footnotes gathered at the end, and its link as a wide button. Church news, ministry posts and *Preview on a phone* all use it. |
| Writing a post | `PostEditor` (+ `WritingDesk` in the admin panel) | A title and one `RichTextField` holding the whole post: text, headings, pictures, galleries, video, files and links all in place in the text (owner, 2026-11-14: "just a title and proper markdown that lets you embed images/etc into it"). On the phone a full-screen page, with one row for who it's for, telling people and when that opens their settings (inline inside a sheet). In the admin panel it's a page of its own (`WritingDesk`), never the record pane (owner, 2026-11-13, #83). Files wait and upload with the save, on both sides. |
| Formatted text | `RichTextField` (Platform.Ui Content) | Live Markdown: it formats as you type, and the marks show faintly only where the cursor is. One bar of 36px quiet icons above the text: a *Text style* select (Text, Heading 1 to 3, where `Headings` allows), Bold, Italic, Strikethrough, Highlight, then Bulleted and Numbered list, Quote, Link, Code and ⋯ (Superscript, Subscript), groups split by hairline dividers, and a navy primary **Insert** at the right. Insert's groups: *Pictures and media* (Picture, Video, Video link, File, through Media's chooser with Upload), *Layout* (Split into cards, Line across, Table, Code block), *In the text* (Footnote, Emoji), *From the app* (Event, Sermon, Ministry, Life Group; see App cards). Cards and tables are drawn in place. Each place says what it allows with `RichTextMedia` (`Posts` everything; `Layout` the layout extras only; none, formatting only, and no Insert). Media shows in place, with a caption line under it, its library path (`~/Folder/file.jpg`) and a pill of controls (drag, add a picture beside it, replace, take out). On the phone the bar is one sliding row with Insert first and pinned, and Insert, the style and ⋯ open sheets; while the keyboard is up the bar sits on it with 44px-wide buttons. `RichTextEditor` (the Markdown box with a preview) is what older places still use until they move (owner's picks on the decision page, 2026-11-13 and 2026-11-14). |
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
- **Pill** (700, 0.7059rem / 12px, 1.3): pills, the label line over a special in the agenda and on an event head, the sidebar's count capsules (tabular) and an area's page count (600, tabular, white at 76%), the date line inside an office-week capsule (at the capsule's weight), and the whole capsule on the fortnight rail.

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

### Home
Home is the Hub: a compact masthead, then one grid of tiles on the 20px gutter, 22px below the masthead. The grid has 2 to 4 equal columns (staff's choice, 2 by default) with a 12px gap; tiles pack in order, each taking the first place its width and height fit. A row's height follows the column width and stays near square: one column's width, held between 88px and 132px. A tile spans 1 to 4 columns and 1 to 4 rows, and its height is its rows plus the gaps between them; a tile as wide as the grid and one row tall sizes to its content instead (an agenda line). A tile narrower than 112px drops its lines and its go arrow, sets its title at label size and tightens to 12px of padding.

### Admin panel
A two-column shell: a 248px navy sidebar, sticky at the full window height and scrolling on its own, beside a work area padded 28px top, 40px at the sides and 64px at the foot. Each page opens on a title band (headline title, an optional caption lead up to 72ch, actions at the end, bottom-aligned and wrapping), then its content, capped at 1440px. Full-bleed views (the workbench, the preview window, the sign-in stage) cancel the work area's padding and run edge to edge.

- **Office week:** the home page is the rail (36px below it), then two columns at 1.4 to 1 with a 48px gap: "Needs you" and "Your areas", both hairline rows at least 64px tall with a 34px leading tile.
- **Announcements desk:** the fortnight rail, the "Not sent yet" chips tucked under it, then the writing column beside a sticky proof aside (280 to 340px, 24px from the top) with a 40px gap, capped at 1180px; "Sent earlier" follows as a table.
- **Places in the app:** the directory beside a sticky 440px phone column (24px from the top) with a 40px gap, capped at 1320px. The phone's holder is as tall as the window allows between 380 and 560px (the window height less 340px), and the phone fits that height.
- **App styling:** capped at 1440px. A title band held at 72px whatever it shows: the title on one line, cut off with an ellipsis, over one lead line 1.6rem tall; its actions never wrap. Under it two columns with a 48px gap: the phone column, sticky 16px from the top and as tall as the window less 150px (at least 520px), holding the phone with its preview chips 14px below; and the 400px panel, its parts 14px apart and 32px clear at its foot, scrolling with the page.
- **Media:** above 900px the title band keeps its search (180 to 210px), New folder and Upload on the title row. Under it, a wrapping bar (12 by 20px gaps, 16px below): the shelf switch on the left, the type filter chips and the Grid/List switch on the right. Then the path row (at least 36px: breadcrumbs, with the open folder's actions at its end) and, on the app's shelf, its note. The bar, the path row and the note span above the split view, so opening a file's detail never moves the grid at any depth. Inside the list: folder tiles auto-filling at least 210px wide (10px gaps), 16px, then the picture grid auto-filling at least 168px wide (20 by 14px gaps).
- **Workbench (a list and its record):** three panes edge to edge at the window's full height: the sand rail (248px; 24px in at the top, 16px at the sides, 32px at its foot), the list (24px top, 32px sides, 48px foot) and the record pane (360 to 440px, 24px in, ruled off by a hairline on its left, sticky, as tall as the window below the workbench's top and scrolling on its own, so its foot and save bar are in reach before the page scrolls (2026-11-13)). A page without filters drops the rail; with nothing chosen the list takes the room. At 1360px and under the record narrows to 320 to 380px and the rail sits above the list (16 by 32px padding), or, with `FoldAside`, steps aside while a record is open. At 760px and under, or when asked to be narrow (the app's form), one pane shows at a time: the chosen record in place of the list, its Close going back to it (owner, 2026-11-13), unless the page says which (`Showing`, for a record pane that is always there); the split view's form stacks the record under the list instead, with a hairline above it. A workbench under other content on a page is ruled off by a hairline above it, with 16px under the rule at 760px and under. A table hides its optional columns once it is narrower than 760px, and under 420px tightens its cells to 8px and lets its headings wrap.
- **Breakpoints allow for the sidebar.** The work area's breakpoints are window media queries offset by the 248px sidebar: the office week stacks its columns and lets the seven-day rail scroll sideways (days at least 84px) at 1228px, where the announcements desk also drops its proof under the writing column (static, up to 360px) and the Feedback board's docked ticket becomes an overlay; the fortnight rail always keeps days at least 56px and scrolls sideways when they don't fit; a person's Roles tab drops its explanation panel under the ticks (no longer sticky) at 1328px; a person's Family and Safeguarding tabs narrow their panel to 260 to 340px (28px gap) at 1328px and drop it under the rows (static, the record's fields one column) at 928px; the Places explorer drops its phone column under the directory at 1100px (static, up to 420px, a hairline above, 24px of space), and so does App styling its panel under the phone (28px of space, the phone column static and 640px tall); at 760px App styling's title band wraps and grows to fit.
- **Calendar workbench:** the shared workbench, its rail folding while a record is open at 1360px and under: the sand rail holding the calendar's items and calendars; the middle pane opening on its own head (a headline title, a caption lead up to 72ch, then the Agenda and Month switch at least 13rem wide beside New event, wrapping) over the agenda as day sections 18px apart, or the month grid capped at 640px; and the record pane, holding the event, a calendar's settings or a break flat on the page.
- **Feedback board:** the title band (its search up to 320px), the Board and Blocked phones tab strip, then the status columns 16px below in one row 10px apart, scrolling sideways when they don't fit. Clicking a card docks its ticket beside the board: the board and a 360 to 440px dock, capped together at 1600px; the dock is sticky at the top, as tall as the window and scrolling on its own, runs to the work area's top, right and foot edges, and is ruled off by a hairline on its left (28px in, 48px at its foot). Under 1228px the dock leaves the grid and floats over the board at the window's right edge (up to 440px, full height, a -18px 0 32px shadow in the scrim colour). A ticket opened as its own page (`/feedback/{number}`) is two columns, its main column beside a 280 to 360px side 40px apart, capped at 1320px; one column under 1100px.
- **Settings pages:** settings in grouped sections, each a card up to 820px wide, 20px apart (see Components, *Settings groups*).
- **Narrow (900px and under):** the sidebar becomes a sticky top strip (8 by 12px padding) with the mark and "Good Shepherd" at one end and a Menu button at the other: a 38px capsule on the masthead tint, label size at 600 in white, with a menu icon that turns to an x and "Close" while the menu is open. The menu opens the same sidebar under the strip as a navy panel up to 320px wide (12 by 14px padding, 20px at the foot, scrolling on its own, a 0 18px 32px shadow in the scrim colour) over the scrim. Escape, a click outside or opening a page closes it; opening moves focus to Jump to, and closing returns it to Menu. The work area padding drops to 20px top, 16px sides and 48px foot.

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

### Buttons
Capsules with a firm press: they scale to 0.96 on press with an ease-out curve, and fire a selection or light haptic.
- **Shape:** capsule (22px radius at 44px tall); full-width block buttons are 52px tall at the 14px card radius.
- **Primary:** navy fill, white label, 600 weight, 18px side padding. Used for the main action of a section (Give online, Send to the pastoral team).
- **Quiet:** the field fill (Field, no edge), ink label. Secondary actions and "Not now"/"Done".
- **On masthead:** white fill with navy label, the main action inside a masthead (Listen). **Ghost on masthead:** translucent white fill (12%) with white label, the secondary masthead action (Watch, Remind me).
- **Live:** aqua fill, the Watch button while a service is streaming. In the light scheme the fill is a deeper aqua (#008176) so white text holds 4.5:1 (4.8:1).
- **Tinted:** deep aqua at 12% with a deep-aqua label (20% on hover), for an action that does something for you beside a quiet one, without competing with the section's primary: *Capture this page* in the admin panel's Send feedback.
- **Disabled** (owner, 2026-11-11, chosen on the decision page: "Outline"): no fill, a 1.5px inset ring of ink-3 and an ink-2 label, not interactive and not pressing. On the masthead the ring and label are on-masthead-2; in the selection tray, on-primary (the ring at 60%). The same outline marks every disabled control: icon buttons (a ring in their own colour), filter chips, steppers, the chat send button, switches (the ring around an ink-3 knob) and a chat message still waiting to send (its time and files in ink-3). Disabled rows, toggle rows, choices, the admin panel's plain buttons and text links turn their words ink-3 instead; a field that can't be changed keeps its fill with ink-3 text.
- **Icon buttons:** 44px round, transparent, with a translucent fill and a 0.94 scale on press; disabled, the outline above.
- **Pressed** (`Pressed`): a button that turns something on and off says so to screen readers (`aria-pressed`); a small icon button turns navy while on. The format bar's buttons use it (see Formatted text).
- **Link** (`ButtonVariant.Link`): an action inside a line of text ("Try again · Delete" under a message that couldn't send, "Seen by 4", the check page's "2 more people"). Deep-aqua words at 600 with no fill, no padding and no minimum height, at the size of the text around it; disabled, ink-3 with no outline. An action that stands alone is a real button.

### Chips
- **Filter chips:** 36px capsules in sand with ink-2 text; selected becomes navy with white text. Scroll horizontally without a scrollbar. The admin's shared filter chips are smaller (34px, 12px padding, caption at 600), wrap with a 6px gap, take the field fill, turn ink on hover and primary with on-primary text when chosen (pressed state for screen readers), and can carry a tabular count, in ink-3 until the chip is chosen. **Rail** (`FilterChips Rail`, one group per filter in the workbench's rail): a caption-size 700 label in ink-3, then each choice as a full-width 38px row at the 9px radius, label size, left-aligned with its count in ink-3 at the end; hover tints it with the segment colour at 60%, and the chosen one is the segment tile (white in light) with the segment shadow. Groups sit 16px apart.
- **Toggle chips:** several on at once, each a 36px capsule (14px at the sides, caption at 600): sand with ink-2 when off, primary with on-primary when on, pressed state for screen readers and a haptic on tap. The row scrolls sideways into the gutter without a scrollbar, 8px apart, and can end in a trailing slot (the calendar's deep-aqua "+ Browse" at caption 600); the wrap form wraps in place instead. The calendars in your week use it. The **quiet** form is for a set that is usually all on, where solid capsules would shout (the weekly digest's calendars, owner 2026-11-09): it wraps, 6px apart, 32px capsules at 12px sides with no fill, ink-2 text and a 1px ring of ink-3 at 45% when off (ring ink-3 and text ink on hover), and sand with ink text led by a small check in accent when on (9px at the leading side).
- **Passage chips:** 32px capsules in sand with deep-aqua text and a small open-book icon; tapping opens the passage in the in-app browser.
- **Pill:** a small (0.7059rem, 700) capsule of orange at 28% with orange-ink text, for status such as "Later release".
- **Reaction chips** (`BubbleReactions`): under a chat message, wrapping 4px apart, each a 26px capsule on the page colour ringed 1px in the hairline, the emoji then its count in tabular ink-2 at caption size; your own reaction is deep aqua at 12% on the page with a 1px deep-aqua ring. Tapping toggles it with a light haptic; where you can't react (a chat you may only read, the admin check page) they are disabled. **Picker:** the same chips in a sand capsule row spread across the top of a message's sheet, each a 44px round target with a bigger emoji (1.4rem), no fill, ring or count; yours is a surface tile with a 2px inset deep-aqua ring.
- **Pray button:** a 36px sand capsule that turns aqua when you are praying.
- **Chosen chips (chip picker):** for choosing several from a long list, such as the roles a tile is shown to. Each choice is a 32px primary capsule with on-primary caption text at 600 and a 28px round remove button (on-primary at 16% on hover, spoken as "Remove" and the name). Under them a 44px search in the field fill with the field edge, at the field radius (leading search icon, row-size text, deep-aqua caret, the 2px inset deep-aqua ring on focus) opens a listbox of matches on the surface, ringed by a hairline and lifted, up to 18rem tall: names starting with what's typed come first, each the name at row size and 600 over a caption in ink-2, the active one on sand. Arrows move through it, Enter adds, Backspace on an empty search removes the last chip, Escape clears the search.
- **Pill tones:** Marker (the default, orange as above) for "changed in this draft" and "changed for everyone since"; Accent (aqua at 12% with deep-aqua text) for a calm state, and "Test" on a test notification; Masthead (solid masthead colour with white text) for the event layer; Quiet (fill-on-surface with ink-2 text) for a state that no longer matters, such as an expired or turned-off code, the "Crash" mark in App health, and "Not recorded" on the Safeguarding list; Danger (danger at 12% with danger text) for a safeguarding item that has run out ("Run out", "Overdue"), and nothing else. Due soon and a young person without a parent stay Marker. An optional small icon leads the label.

### Segmented control
A track in the field fill (12px radius, 3px inset, no edge) of equal segments; the selected segment is a white tile (9px radius) with the segment shadow. Used for Automatic, Light, Dark, the inbox's Waiting and Everything, and an announcement's Now and Later. A segment can carry a count in the same capsule as the tab strip's (shown only above zero). It is a radio group: one Tab stop on the chosen segment, and the arrow keys, Home and End move the choice.
- **Labelled:** a choice in a form shows its label (caption, 600, ink-2) and an optional hint (caption, ink-3) above the track, like a field; a stacked one gives every option a detail line saying what it means. A bare track is only for switching what a screen shows.
- **Rows:** a rule of a few choices that each need a sentence, inside a settings group: one full-width row per option (touch height, 12px above and below, left-aligned), a hairline between rows, led by a 22px radio ring in ink-3 (2px) that fills with the accent (a 7px ring) when chosen; the label at row size in ink, an optional detail line in ink-2. No track, no tile. The roster's swap rule and what the team sees use it.
- **On masthead:** inside a masthead the track is the masthead chip (white at 12%), its segments 32px tall in on-masthead, and the selected one the page-surface tile with primary text. The calendar's Week and Month switch uses it, beside the masthead title.

### Month of days
`MonthDays`: a month to look at, not a date field. A bar with quiet small ‹ › buttons either side of the month and year (row size, 700); single-letter weekday heads (Monday first, pill size 700, ink-3); then the days as 44px tiles in seven columns, 4px apart, at the tile radius (10px): white (the surface) ringed by a hairline, caption-size 600 tabular figures. Today is ringed in primary (2px). Marks: away all day fills the tile with marker at 55% and marker-ink figures; mornings fill the top half, evenings the bottom half; some hours fill it with marker at 25%; a day skipped is struck through (2px) in ink-3. Past days and days outside the month can't be chosen (the outside ones are hidden). Each day's label says the date and its mark in words. A caption key in ink-2 sits under the grid. Pressing a day scales it to 94%.

### Stage pipeline
For a status that moves forward through stages, where a segmented track read "like tabs, not progression buttons" (owner, on the decision page, 2026-11-11). One row of chevron-cut stages that fill the width equally, each overlapping the one before by 60% of a 12px notch: the first has a flat, rounded (9px) leading end, the last a rounded trailing end. Each stage is a 44px button with its label centred in caption at 600 (1.2 line height, wrapping to two lines in a narrow column, kept to one line from 600px of width).
- **Passed:** deep aqua at 18% with a deep-aqua label led by a small check (26% on hover).
- **Current:** solid deep aqua with the label in the page colour; not pressable. It is marked as the current step for screen readers.
- **Ahead:** fill-on-surface with an ink-2 label; hover tints it with deep aqua at 12% and turns the label ink.
- **Focus:** no ring; the stage fills with deep aqua at 34% and its label is underlined (3px offset). Fills ease over 160ms.
- **Narrow (440px of width or less):** the checks go and the side padding tightens.
- **While moving:** every stage is disabled with the progress cursor.
- **Read only (`ReadOnly`):** the same row and fills, but each stage is a plain label, not a button: no hover, no focus, nothing to press. The current stage is still marked for screen readers. For the person a thing belongs to, who can see where it is but not move it.
The docked feedback ticket's status uses it, and a prayer request's care steps (New, Followed up, Answered) on the phone and the Prayer desk; the person's own request shows it read only (Sent, On the wall, Answered). Pressing a stage moves to it, backwards too; leaving the sequence (*Mark not doing*) is a separate quiet action beside it, not a stage.

### Examples in copy
**Copy never names a real ministry, group, room or person as an example** (owner, 2026-11-10: "i don't really want direct mentions of it in the frontend as being as/for kids life. there's lots of references and such of stuff like that"). Placeholders, hints, option details and empty states stay generic ("Documents and links for its people"), or use the ministry's own name and data when the words are about that ministry ("{Name}'s leaders lead these too"). A field with no generic example has no placeholder. Sample data (Development only) may use real-sounding names; nothing on screen depends on it.

### Saving
A form with unsaved changes always asks before they're lost (owner, 2026-11-10: "we need prompts any time data might be lost without saving"). There are two kinds of form, and a person can always tell which one they're on.
- **Saves by itself** (autosave): for long work that's built up over time and has its own publish or send step, such as app styling drafts, an announcement being written, or the digest. It shows `SaveStatus` near its title: "Saving…", "Saved" (or "Saves as you type" before the first save), "Couldn't save. Trying again by itself." with *Try now*, or why it can't save yet ("Give it a title so it can save"). It never has a Save button. It asks before leaving only while a save is waiting, running, failed or not possible yet.
- **Saves with a button**: for everything else, a record someone changes and then commits (a ministry's details, an event, a sermon, a break). Its one save action is `SaveBar`: Cancel (quiet) at the left, a primary button named for what it does ("Save", "Add the regular event", "Add the ministry") at the right, and above them, on its own line at the right, "You have changes that aren't saved" (a pencil in ink-3) while there are changes and "Saved" with the check only once the save has happened. A new record (`New`) never says it has unsaved changes. Any other action sits after Cancel and moves to its own line above the buttons when the bar is 520px or narrower. It always asks before leaving with changes.
- **Can't save yet:** the button is disabled and the reason takes the status line above it, in words, caption at 600 in ink-2 ("Give it a title to add it."), never only in a tooltip.
- **Where the save goes:** in a desktop editor with a navy header, on the header's action row at the right, as the on-masthead button (white with navy text; disabled, the masthead's outline, see Buttons), its status and reason in on-masthead-2. In the workbench's record pane, docked at the pane's foot; in a settings group, in its sand foot; elsewhere in a panel or a page, the editor foot (sticky) or the form's last row. On a sheet, in the sheet's actions. On the phone, the editor foot, the save full width (`Block`) with Cancel beside it and the status centred above. Delete is never in a save bar: it is in the item's ⋯ menu.
- **Instant changes** (a switch, a role tick, linking a person) take effect the moment they're made and need neither a Save nor a status. Keep them visibly apart from a Save-button form on the same screen: in their own group below the form's, whose foot says "These change straight away", never in the same group as a Save button.
- **Leaving:** `UnsavedGuard` covers going to another page, a sidebar link, Back and closing the browser tab (the browser's own warning). Screens that swap what's open without navigating (the calendar, sermons, series and speakers) ask `UnsavedChanges.MayLeaveAsync()` first. The sheet says what would be lost and offers *Save and go on* (when the form can save), *Discard the changes* and *Stay here*.

### Lists and rows
The workhorse. A row is a full-width line at least 56px tall: optional leading tile or thumbnail, a title (row title) with an optional caption subtitle, and trailing metadata with a chevron (or an external-link icon when it leaves the app). Rows are divided by hairlines, start with a hairline above the list, and fill with the hairline colour while pressed.
- **Leading tiles:** 34px, 10px radius; sand with navy icon by default, aqua at 16% with deep-aqua icon for actions, orange at 30% with orange-ink icon for warm items, sand with ink-3 for quiet ones.
- **Thumbnails:** 96px wide at 16:9, 8px radius, because sermon graphics are made for 1920 by 1080 screens; a progress track (4px, strong sand, orange fill) can sit under the subtitle for sermons in progress.
- **Agenda rows:** at least 44px, 10px above and below, top-aligned: a 4.75rem time column (label size, 600, tabular, primary) before the title. A special or one-off carries a label line above its title, pill size at 700 in accent, and its time in primary; a regular row is quiet: no label, its title at 400 in ink-2 and its time at 400 in ink-3. A date of a regular event carries the small repeat icon in ink-3, 6px after its title, spoken as how it repeats ("Repeats every Sunday"). A date that isn't on keeps its row, title and time at 400 in ink-3, its title struck through (the line in ink-3 at 60%), with a caption line under the title in ink-3 led by the small circle-off icon saying why ("Kids camp weekend · back on 20 November"), never a suffix on the title. A row missing details says what's missing in a caption in orange ink at 600. Past rows turn their words, times and icons ink-3; a selected row takes the Tonal Selection. In the app's lists a special or one-off row is set on marker at 14%, 12px wider each side at the field radius, with the hairlines either side of it hidden; a quiet row and a date that isn't on stay plain. Within a day section the hairline sits above each row.
- **Unread and read rows:** an unread row has a 9px orange dot in a 17px gutter before its title, and the title at 700; a read row has no dot and its title at 500. Rows that are neither keep 600.
- **Inbox:** the Notifications page's masthead note counts what needs you ("2 waiting for you · 3 new"). While something waits, a Waiting and Everything switch sits above the list, its Waiting segment carrying the count. Waiting rows are reading rows on the warm tile with an action line below in deep aqua at label size and 600 ("Preview it" and a chevron, or "Do this in the admin panel"), and a hint under the list. Everything shows every item (warm tiles for those still waiting, quiet tiles for the rest), an Accent "Test" pill on test notifications, and a small quiet "Mark all as read" under the list while anything is unread.
- **Hint:** a caption line in ink-2, up to 65ch, 14px under a list, for one sentence about how the list behaves.
- **Toggle rows:** a title and caption beside a 51 by 31px switch (off, ink-3 at 24% over the surface with the field-edge ring; on, live with no edge; a white knob; disabled, the same at 45% opacity; 2026-11-13). When a switch has a consequence, the caption itself changes in place to say it ("Only its own people and staff see it" once hidden), rather than a line appearing under the row. In the admin panel the switch of a row with a caption sits level with the title (top-aligned, 8px down), so it doesn't move when the caption grows (owner, 2026-11-13, #80: "nothing else on Settings moves"); the phone keeps it centred. **Muted** (`Muted`): a row whose thing is turned off keeps its place with its title in ink-3, and its caption says what off means ("Hidden from everyone, leaders too, until you turn it back on. Nothing in it is deleted.").
- **Row menu** (`ListRow` `Actions`): a small ⋯ `Menu` ("More for …") beside the row, never inside its link or button, so pressing ⋯ never also opens the row. Centred on the row's height: on a row that opens something it sits 24px from the right, just before the chevron (the trail keeps 36px clear for it); on a row that doesn't, it sits at the right edge and the row keeps 40px clear. The row opens the thing; the menu acts on it, Delete, Remove or Unlink last and red after a rule, asking once in a `Sheet`.
- **Boxed list:** the same rows inside a white, lifted, 14px panel with 16px side padding.
- **Reading rows:** for entries you read rather than tap. Top-aligned, 22px above and below, the leading tile nudged up to sit with the title, and the subtitle at body size in ink, up to 65ch. Anything more (facts, a text link) goes below the subtitle.
- **Title extra:** a slot just after the title, for a pill such as "Staff only".
- **Fact list:** labelled lines on hairlines: a 600 caption label in ink-3 over row-size text in ink. With an icon, each fact has a 34px fill-on-surface tile with a navy icon, as a row does, and every fact starts with a hairline. Plain facts set their text in ink-2 and drop the first hairline, so they read as a row's detail. A fact's value can be rich content (a pill, a date, a link) instead of text. A fact with neither is not shown. In the admin panel a fact list runs two to a line, one at 760px and under; inside a settings group it is one column of label and value rows, the label 11rem wide beside its value above 760px and stacked over it below (2026-11-13, #81).
- **Text link:** a deep-aqua 600 label-size link with a trailing chevron and a 44px target, for a "Show me" inside a row.

### Cards / Containers
- **Callout:** sand, 14px radius, 15 by 16px padding, a bold lead line then text. The bulletin's shaded box (Your challenge this week).
- **Sermon head:** a sand band (14px radius, pulled 12px into the gutter) holding a 112px 16:9 thumbnail, the sermon title and "speaker · watch or listen", linking to the sermon.
- **Feature card:** the only picture card, for upcoming events: a 16:7 cover over a white body, 14px radius, lifted.
- **Covers:** images fill their box; sermon artwork boxes are always 16:9 so the graphic's text is never cropped. With no image, a navy (or aqua-tinted, or orange-tinted) block with the mark at 10% and an optional title.
- **Series cards:** a 16:9 cover, then the series name and message count, two to a row.

### Inputs / Fields
- **Style:** the field fill with a 1px inset field edge (Field), 12px radius, 13 by 14px padding, body size; a 600 caption label above in ink-2; placeholders in ink-2. Notes fields use the 14px radius and a 180px minimum height. Every field takes it: text, multi-line, selects, the search field, the chip picker's search, the copy field, the colour hex, rich text, the chat composer and the admin panel's plain inputs.
- **Focus:** no outline; the 2px inset deep-aqua ring replaces the edge. Elsewhere focus-visible is a 2px deep-aqua outline at 2px offset.
- **Rich text** (`RichTextField`): the live editor, a sticky bar over the text. The bar is the surface with a hairline edge under it (4px above, 6px below), sticking to the top of what scrolls; it wraps on the desk and slides on the phone. Its tools are quiet 36px icon buttons at the 9px radius (32px wide at 480px and under, 44 by 40px on the keyboard), ink-2 on nothing, fill-on-surface with ink on hover, navy with on-primary while on; groups split by 1px hairline dividers 22px tall. The style select is a small quiet menu button (field fill, ink, a chevron) at least 7.5rem wide; Insert a small primary button with its icon. The text is a sand field: the field fill and 1px inset edge at the 12px radius, 12 by 16px in, body size at 1.55, the 2px inset deep-aqua ring on focus. Menus are the shared `Menu` with group titles (micro at 700, ink-3); the phone's Insert sheet is tiles four across (sand, 72px, accent icons) under caption ink-3 group heads at 700, and its style and ⋯ sheets are rows with the current one on Selected Fill. Inline styles as it reads: highlight on `--highlight` (marker at 42%, 34% in dark) at half the inner radius, strikethrough in ink-2, code on sand in `--font-code` at 0.88em, headings `#` to `###` at 1.35, 1.18 and 1.05em and 700, a line across as a hairline, a card split as a dashed double rule round "Next card" in micro ink-3 at 700, tables in a hairline box at the 9px radius with a sand head row, footnote marks in accent at 700, and code fences folding away off the cursor's line. `RichTextEditor` (events' and settings' older fields) keeps its wrapping row of field-fill buttons over a plain field with a "How it looks" preview until those move.
- **Search field (admin):** a 40px field (fill and edge) at the 12px radius, up to 420px wide, with a leading search icon in ink-3, row-size text, an ink-2 placeholder and a deep-aqua caret; focus is the 2px inset deep-aqua ring on the whole field. It settles 250ms after typing stops before it searches.
- **Copy field:** a labelled, read-only 40px field (fill and edge) at the 12px radius (row-size tabular text, a little tracking), with a small quiet Copy button beside it that turns to "Copied" with a check, and a polite caption status line beneath (a hint, or how to copy by hand if copying fails). Used for sign-in codes and recovery links; selecting the field selects the whole value.
- **Code field** (`CodeField`, Platform.Ui): a text field for a code someone types or pastes (a parent's sign-in code, an app reviewer's code). As they type it turns to capitals and puts a dash after every four characters, so "zf3b2td9" shows as ZF3B-2TD9; a pasted code with or without its dash works the same. The placeholder shows the shape (ABCD-EFGH), and the phone's keyboard opens in capitals.
- **Copy button:** the copy field without its field, where the value is already shown beside it: a small quiet capsule with a link icon ("Copy link"), or the copy icon when the value isn't a link ("Copy the code" beside a sign-in code), that turns to "Copied" with a check, or "Couldn't copy: select the address" with an alert icon, spoken politely. In Places in the app it sits between the primary action and the quiet ones.
- **Who sees it (audience picker):** a segmented Everyone, Signed in and Roles (a fourth "Set up specially" appears only for an audience the three can't express), and under Roles the chip picker of roles ("Find a role") with a hint line; one audience can name several roles.
- **Picture field** (`PictureField`, Platform.Ui): the one way to choose a picture in a form (an event, a Home tile, an announcement). Empty, it is an add row ("Add a picture", "Choose a picture"). Chosen, the picture is shown as people will see it, up to 240px wide (at least 160px) at the field radius: in the app's 16:9 frame when the form's surface uses one (an event: a picture far from 16:9 shown whole on the masthead colour, as the event page does), otherwise in its own proportions up to 200px tall on sand. Beside it, wrapping under it when narrow, how the app shows it in caption ink-2 ("The app shows a 1280×1707 copy · 256 KB") over Change and Remove as small quiet buttons with their icons. A file that isn't a picture shows its name with the file icon.
- **File pick** (`FilePick`, Platform.Ui): the add row that opens the phone's photo library or files. `Kinds` says what it takes (pictures, pictures and documents, pictures and videos); `Multiple` takes several at once up to `Limit`, keeping the first that fit and saying so ("Only the first 4 were kept; that's the most at once."); `TooLarge` turns a file away in words before it is kept. While it keeps them it reads "Keeping it on this phone…" in ink-3; problems show as one warning notice under it. Its content can be replaced (Send feedback's empty frame, the filmstrip's add tile), given whether it is busy. `Box` draws it as the shared `DropBox` with `Label` as the line in bold and `Hint` under it; `Icon` replaces its icon (the composer's paperclip).
- **Drop box** (`DropBox`, Platform.Ui): the one way to take a file. A dashed frame (1.5px ink-3 at 45% on fill-on-surface, the 14px radius, at least 84px tall, 20 by 16px in), centred: an optional accent icon, what it takes at label size and 700 in ink, then how in caption ink-2. Hover and focus turn the dashes deep aqua, with the 2px deep-aqua focus ring 2px out; files dragged over it (or over its `DropOn` area) turn it deep aqua on a 10% deep-aqua wash; busy, it turns ink-3 with the progress cursor; `Chosen` makes the frame a solid hairline (the sermon's audio once picked). It hosts any file input (its child), so features with their own reading of the file (the media library, a sermon's audio, a Home icon) use it as they are. It can also take files pasted anywhere on the page (`TakesPaste`) and files dropped on a surrounding element (`DropOn`, which carries `is-dropping` while files are over it), and can hand everything it kept to its caller in one go (`OnChosen`) instead of keeping them itself; a file of a kind it doesn't take is turned away in words.
- **Showing any file** (`MediaPreview`, Platform.Ui): a picture through `MediaImage`, a video through `MediaVideo` (plays inline, metadata only until played; muted and without controls where it is a thumbnail), anything else as its name with the file icon. A video not yet ready is an empty placeholder of the same box.
- **Media chooser:** choosing a picture or file opens a wide sheet titled "Choose a picture" with Media's own picker. It opens on the last folder used. On top, a search ("Find a picture in every folder", or "Find a file in every folder") and a quiet Upload; then the folder breadcrumbs (Library first) and the open folder's subfolders as compact folder tiles (at least 180px wide, 8px gaps, 50px tall with a 32px icon tile); then an auto-filling grid of items at least 150px wide (14 by 12px gaps, up to 52vh or 560px tall, scrolling on its own), each a 4:3 thumbnail on strong sand at the field radius over its name in caption at 600 (one line, ellipsis) and where it's used in caption ink-3 ("Not used yet", "Used in 3 places"). Searching looks in every folder: the breadcrumbs and tiles give way to "Results for “…” in every folder" at 600 in ink-2, and each item says which folder it's in ("In Sermons › 2026 series"). An empty folder is an empty state ("No pictures in this folder"). Hover rings the thumbnail 2px in marker at 55%; the chosen one is ringed 3px in marker with a 26px marker tick disc in its top-right corner; focus is the deep-aqua outline 4px out. A hairline-ruled foot holds the chosen name (or "Nothing chosen yet") in caption ink-2, Cancel (quiet) and Choose (primary, disabled until something is chosen). Double-clicking an item chooses it. Opening it moves focus into the sheet without scrolling the page behind.
- **Place field:** how staff choose what something opens. With nothing chosen it is a quiet button with a link icon ("Choose a place in the app"): a place here is a link into the app, so the map pin is kept for physical places (an event's place, the site map). The chosen place is a token: a fill-on-surface box at the 12px radius, at least 56px tall, holding the row's 34px tile (sand for pages, accent for things in them), the name at row size and 600 over where it is (or its date) in caption ink-2, both cut off with an ellipsis, then a small quiet Change and a small quiet icon-only clear ("Open nothing"). A place that can no longer be opened takes the warm tile (marker at 30%, marker-ink alert icon) and says why. Change opens a wide sheet carrying the Places directory, listing only what the audience can open.

### Navigation
- **Top-bar actions:** quiet icon buttons and a `Menu`'s ⋯ at the end of the phone's top bar have no fill and take the bar's colour (white over the masthead and on the stuck bar), with a 24px icon; pressed, or while their menu is open, they take the masthead chip fill. A cog opens the settings of what you're looking at; Delete and other actions on it go in the ⋯ menu beside it.
- **Top bar:** 48px plus the top safe area, three-column (back, centred title, actions). Transparent over the masthead with white icons; it turns solid navy as soon as the masthead's big title reaches it (so the title slides under it, never under Back), and the small title fades up into view once the big one is fully under, or past a per-page threshold (120px by default, 44px on Home's compact masthead), whichever comes first. Detail pages with a hero use it solid from the start. The back button carries the parent's name, and is spoken as "Back to" that name, or just "Back" when that is its label; a page can give it its own action instead of a parent (the event editor's "Cancel"). A word action at the end ("Edit") is on-masthead text at body size and 600, 44px tall.
- **Tab bar:** fixed, up to five equal items: Home, always first, then up to three chosen by staff (Sermons until they publish a change), then More, always last; each a 24px icon over an 11px 600 label in ink-3; the active tab is deep aqua. Tapping the active tab scrolls to the top. While audio plays, three small orange bars animate beside the Listen icon on every screen. Frosted page colour with a hairline top edge.
- **Notifications bell:** a 44px icon button in the top bar of every tab root, with an 18px orange count badge ringed in masthead navy; it opens the inbox, a pushed page whose top bar carries a sliders icon button to Notification settings.

### Masthead (signature)
The cover of every tab root: navy ground, the mark at 7% opacity, 210px wide, bleeding off the right; the display title (balanced, up to 13 characters a line); a note line in beige at 600 (verse and date, a date range, a one-line purpose); then an optional slot for the screen's live element. The masthead sits under the transparent top bar, padded down by the safe area plus 56px.
- **Status strip:** inside the Sunday masthead, a 12px-radius strip of translucent white (12%) with a clock icon or a pulsing aqua live dot, the next service or "Live now", and a ghost or live button.
- **Now playing:** inside the Listen masthead, 136px 16:9 artwork beside the title. With nothing playing it shows the latest sermon with Listen and Watch buttons; once audio is playing it pins that sermon instead, with a 4px orange progress line and a transport row (elapsed, 15s back, a 56px white play/pause disc, 30s forward, remaining).
- **Beside:** a slot beside the title; the title and it share a row that wraps when tight (10px gap, bottom-aligned), the slot nudged 4px up to sit on the title's baseline. The calendar's Week and Month switch sits there.
- **Week rail:** in the calendar's sticky week band, seven equal day buttons 4px apart, each at least 44px at the 12px radius in on-masthead: an uppercase micro weekday (600, +0.04em, on-masthead-2), the date at lead size and 700 (tabular), and a 5px beige dot when something is on. Days before today turn on-masthead-2; the day in view takes the masthead chip; today is a white (surface) tile with primary text and an orange dot. Pressing fills with the masthead chip; focus is a 2px marker outline 2px out. Tapping a day scrolls the agenda to it.

### Section tabs (signature)
A sticky, horizontally scrolling row of 600 labels in ink-3 (18px apart) that pins directly under the top bar on sectioned pages. As you scroll, the tab for the section in view turns ink and a 3px orange underline slides and resizes to sit under it (240ms ease-out); the row scrolls itself to keep the active tab visible. Tapping a tab smooth-scrolls to its section with a selection haptic. All of Sunday's five tabs (Welcome, Sermons, This week, Next, Give) fit a 390pt width.

### Sermon outline and player
- **Outline:** point headings at 700, items in ink-2 prose with a 6px orange dot bullet, passage chips beneath; a "Main text" line leads with its chip.
- **Player:** a sand panel (14px radius) with a 48px navy round play toggle, label and tabular position, 15s back and 30s forward skip buttons, and an orange-accented scrubber.

### Sheet
A bottom sheet over a 38% black scrim: page-coloured, top corners 22px, a grabber, a title-large heading and ink-2 lead, rising 320ms with the ease-out curve. Used for My notes, the first-launch topics welcome, the notifications welcome, confirmations and What's new. On wide screens (720px and up) the sheet is centred (560px), and one without a cover hides its grabber. Sections inside a sheet drop the page gutter and their top margin, and take the sheet's. With the keyboard up it sits on the keyboard and its search field takes focus only once it has arrived (see Layout).
- **Actions:** the main action comes first. On the phone they stack full width, the main one on top (a red Delete in a confirm too). In the admin panel they sit in one row: the main one at the right, the quiet one that backs out (Keep it, Cancel, Stay here) at the far left, any others between. A confirm says what goes with the thing in its lead and asks once.
- **Tools:** a sheet that edits one item puts that item's ⋯ menu (Delete last, red) on the title's line at the right (`Tools`), never on a row of its own. The roster's date, position and days-away sheets use it.
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
- **Month grid:** micro weekdays (600, +0.04em, ink-3), then day buttons at least 46px at the 12px radius: the date at row size and 600 (tabular), and under it up to three marks: pale (strong sand) for a regular event, accent for a special, marker for a change, and an open 1.2px ink-3 ring for a date that isn't on. Days before today in ink-3; days outside the month in ink-3 with the date at 400; hover fills sand. Today is filled primary with on-primary (marks in on-primary, regular at 50%); the chosen day is an inset 2px primary ring. A caption key in ink-3 lists the four marks.
- **Event head:** a navy band like the masthead, the mark at 7% bleeding off its top-right, holding the date block beside the words (16px apart): the label line (pill size, 700, beige), the title (headline, 700, -0.02em, balanced) and the when line (beige, label size, 600, tabular: "8:30–11:30am · every second Saturday"). The label line carries the event's own label or its calendar's name, never words the page invents. A date of a regular event ends its when line with "· this date only"; a regular event's own head has no date block, and its when line is the pattern led by the small repeat icon ("Every Sunday · 10:30am–12pm"). Compact in the admin panel's panels: 16px padding, a 12px gap, the title at title size.
- **Date block:** a white (surface) tile, 78px wide at the 14px radius, the weekday, the date numeral and the month stacked in primary (see Date numeral). Flat: no shadow. In dark it is a raised navy tile with aqua numerals (see Colors). It is a picture of the date, spoken as the full date.
- **Event body:** 16px under the head, items 14px apart: an optional 16:9 picture edge to edge with square corners above the body; the actions wrapping 10px apart (Add to calendar as the primary with its icon, Register when there is a link and Share quiet); a change note in marker at 22% at the 12px radius (11 by 14px, caption in ink, "Changed" leading in orange ink at 700); a date that isn't on as a sand note at the 12px radius with its icon in ink-2 at 600; then the fact list with icons (where, opening Maps; contacts, calling or messaging), About in prose, and Other dates as rows with the time trailing in ink-3 (a date not on in ink-3).
- **Edit where you read it:** Edit turns a one-off event's page into its editor without leaving it; on a date of a regular event it opens that date (see below). The top bar reads Cancel and "Editing"; the head keeps its place as a live preview and nothing in it can be tapped (owner, 2026-11-08: the head is a preview; fields are where you edit). Below, Title and Label (with its hint) come first, then a one-off event's Date and Until as a pair and its times, then fields at their 12px radius, add-rows (a full-width 44px line under a hairline, a plus and deep-aqua label at 600, ink on hover), contacts as rows (the sand row tile, name and number, a pencil) that open into a sand box at the 12px radius with surface fields; then the foot bar.
- **Foot bar:** sticky at the foot, docked on the tab bar: frosted bar colour (18px blur) with a hairline above, 12px by the gutter, 10px between its lines: Save (primary, block) beside Not on this date (quiet, "On again" once it's off), then a small quiet Ask to publish when the editor must ask, and a small danger text link to delete. It never holds a choice between this date and every date: a date and its regular event are edited in different places (owner, 2026-11-08). In the admin panel it is the same bar on the panel's surface (no blur), sticky to the panel's bottom with 14 by 16px padding.
- **Widening sentence:** when showing an event on another calendar would reach more people than its own calendar does, a caption at 600 in orange ink with a leading icon says so, 6px under the field; when it needs an answer it becomes a box in marker at 18% at the 12px radius (11 by 14px) with its choices under the words.
- **Regular event page:** a regular event is edited on a page of its own, never through one of its dates. The navy head (the event head without a date block: its calendar's name, the title as a preview, the pattern line) carries the date strip and its key. Under it *Every date* (time, place, contacts, About, Register, links, picture) and *How it runs*, side by side 40px apart once the page is 760px wide and one under the other below that; each opens on a heading at title size and 700 (-0.015em) over a caption lead in ink-2. Every date opens on Title and Label fields. How it runs is the shared row with a chevron, saying its value trailing in ink-3 (Repeats, Breaks, Each date needs, Also shown on, Shown in the app; the value right-aligned, up to 60% of the row), each opening a sheet with only that part. Delete the regular event is last, as the small danger text link. In the admin panel Save every date sits at the head's top right as the on-masthead button, after a caption at 600 in the beige note colour saying whether it's shown (Ask to publish beside it as ghost on masthead); on the phone it is in the foot bar.
- **Date strip (signature):** a regular event's dates, today to a year ahead, as one row of white (surface) tiles on its navy head that scrolls sideways and snaps: 56px wide, 8px apart, at the 12px radius, the weekday (micro, 700, +0.06em, ink-3), the day at title size and 700, the month (micro, 700), in primary and tabular, as the date block is. A changed date is tinted marker at 30% with a 7px orange-ink dot at its top right; a date that isn't on (a break's dates too) is an open tile, an inset 2px ring of on-masthead at 45% with on-masthead numerals; today is underlined by an inset 3px marker bar; the chosen date takes a 3px marker ring. Tiles lift 1px on hover and press to 0.96; focus is the 2px marker outline 2px out. Thirteen show, then a More dates tile on the masthead chip; with a mouse a thin translucent scrollbar shows. Under it a caption key in the beige note colour: a 9px marker swatch naming the first changed date and what it is, an open ring naming the first that isn't on and why, and "Choose a date to change only that date". Choosing a tile opens that date alone; in dark the tiles are the dark date block's (night surface, aqua numerals).
- **A date of a regular event:** the event head with its date block, then a sand band edge to edge (12px by the gutter, caption in ink, the small repeat icon in ink-2): "One of the Morning service, every Sunday." with "Edit the regular event ›" in deep aqua at 700. Its fields say they are for this date ("Title on this date", "Time on this date", "About this date"), and a field that differs from the regular event has a caption line in ink-2 under it, "Usually 10:30am–12pm." with "Put it back" in deep aqua at 600. A date in a break says so in the not-on note, and that breaks are changed on the regular event. Its foot bar holds Save this date and Not on this date. In the admin panel it opens in the right pane, beside its regular event or the agenda.
- **A note on a date:** a feature can hang a link to one of its posts under a date's row (`DaySection` `NoteOf`), as a Life Group does with its Sunday News posts in *Coming up*. The note is a 44px link pulled 8px up under the row, starting at the 4.75rem time column so it lines up with the title: the small megaphone, the post's title and a chevron, at caption size and 600 in accent, spoken as "…, a post in News". A post saying there's no class also makes that date not on (struck through, its reason on the circle-off line), so the row and its note say the same thing.
- **Kept by another feature:** a regular event another feature keeps in step (Life Groups' Sundays) shows no edit fields, on its page or on one of its dates: a sand line at the field radius (11 by 14px, caption at 600 in ink-2) led by a small lock, "Life Groups keeps this in step with its own dates and times, so it's changed there, not here.", then the text link "Change it in Life Groups ›".
- **Once or regularly:** New event asks first, with two choice rows: each a surface button with a 1.5px hairline border at the 14px radius (14px padding and gap), a 44px sand icon tile (calendar, or repeat) in primary, a row-size 700 title (Once, Regularly) over an ink-2 caption saying what it covers. Hover borders it in primary, pressing fills sand. On the phone it is a page under the masthead; in the admin panel a 22rem lifted menu under New event (8px padding, the choices borderless and filling sand on hover), its button saying whether it's open.
- **Regular events list:** each calendar's regular events as rows with the repeat icon in the sand tile, the title, and a subtitle with how it repeats, its time and its next date ("On today", "No more dates"), and a pill when something needs saying: "1 date changed" in marker, or quiet for "Asked to publish" and "Not shown yet". An ended one is ink-3 at 400. With none, an empty state with the repeat icon. On the phone it is a section of the calendar's page, for its editors only; in the admin panel it is a calendar's third view (Agenda, Month, Regular events), and a regular event opens across the middle and right panes under a "‹ Regular events" text button in deep aqua.
- **Calendar workbench (admin):** the sand column opens on "Calendar" at title-large size, then items (All your calendars, Needs details, Asked to publish) as 40px buttons at the 10px radius, label size at 600 in ink, the current one on the segment colour (white in light), hover at 60% of it, a count in a 20px orange capsule (pill type, on-marker, tabular) only above zero. Below a hairline, the calendars as 44px rows in the church's order (a `ReorderList`: Calendar Managers drag one by its handle, or press Alt and an arrow on it, and the order is the one phones show, owner 2026-11-12): the name at label size and 600 with an optional pill-size ink-3 line ("Needs a publisher", or "Copied from iCal"), and, while All your calendars is showing, a small switch (36 by 22px) that shows or hides it there. The grip sits in the column's left margin, outside the row. The chosen calendar row takes the segment colour; a calendar someone can't change is plain text. Under the list, for Calendar Managers when there are two or more, the caption line in ink-3 saying how to reorder. "+ New calendar" follows in deep aqua at label size and opens a New calendar page in the middle: the same settings card, with Cancel and Add the calendar; Breaks sits in a foot group under a hairline with a quiet tabular count in ink-3. Day heads in the middle pane are label size at 700 with the date in ink-3 at 400.
- **A calendar's page (admin):** choosing a calendar opens its page in the middle, as a ministry's does (owner's pick on the decision page, 2026-11-12, "like ministries does it"): its name as a `PaneHead` at headline size with a one-line lead ("For everyone · Needs a publisher · Shown on new phones"; "Copied from its iCal address" or "Added from Browse calendars" where they apply) and, for Calendar Managers, its ⋯ menu (Remove this calendar, which asks once in a sheet saying what goes with it), then `TabStrip` tabs **Events · Regular events · People · Settings**. Events keeps Agenda or Month (Segmented), From another calendar and New event; Regular events is the regular events list; People is a grouped card, *People on it* (its lead says what Editors and Publishers do), listing who is on it with their roles inline (`PeopleSheet Inline`) and Add someone; Settings is a grouped card, *About this calendar* (name, description, who it's for, shown on new phones, needs a publisher, copy from an iCal address) with Cancel and Save in its foot, then *Calendar apps* with the subscribe link. A calendar copied from iCal has no Regular events tab. The settings no longer open beside the list.

**The Quiet Regular Rule.** In the calendar the ordinary stays quiet and only what's different speaks: a regular event is ink-2 at 400 with its time in ink-3 and no label; a special or one-off carries its label in accent and its time in primary; a change is marked in orange until it has been opened; a date that isn't on keeps its place in ink-3, struck through, with the circle-off line under its title, never a suffix and never dropped from the list.

**The One Date Rule.** A date of a regular event is only that date, and the regular event is changed only on its own page. Never put a switch between "this date" and "every date" in an editor; link the two with words ("One of the Morning service, every Sunday. Edit the regular event ›") and let a date say how it differs from the usual, with Put it back.

### Admin workbench and drafts
- **Workbench:** the sand filter rail, the list and the record pane, as Layout describes. The record is flat on the page, ruled off by a hairline, never a card; it opens on a `PaneHead` and ends in its `SaveBar`, docked at the pane's foot over the page colour, flush with the bottom of the window however short the record is (#63). Adding opens a blank record, already chosen, in the same pane.
- **Pane head:** the record's title in title-large type (700, -0.02em, 1.2, balanced, stopping at three lines), a caption lead in ink-2 4px under it, then at the right its tools: the ⋯ menu of everything you can do to the item (Delete last), *Open as a page* and Close, as quiet icon buttons with no fill until hovered. 20px of space under it.
- **Tab strip:** a row of label-weight tabs in ink-3, 22px apart and 44px tall, over a hairline; the selected tab turns ink with the 3px orange underline. A tab can carry a count in a capsule of marker at 22% with marker-ink text (tabular figures), and a trailing slot holds an add action in deep aqua. It is the static sibling of the section tabs: it switches what is shown, it does not scroll the page. **Link mode:** given an address per tab, each tab is a link and the chosen one is the current page (still the 3px orange underline), so a page's areas are tabs with their own addresses, as a ministry's are; a tab replaces the address rather than adding to history. **Sticky** (phone) pins it under the top bar on the page colour, gutter-padded, one row that scrolls sideways without a scrollbar. **Narrow:** at 760px and under every tab strip is that one row, never wrapping; the selected tab is scrolled into view, and a strip that isn't sticky fades its last 24px as a sign there is more (2026-11-13).
- **Save status:** one caption line at 600 that is polite to screen readers: a small spinner and "Saving…", a deep-aqua check and "Saved", a pencil in ink-3 and "You have changes that aren't saved" for a form saved with a button, or, on failure, a cloud-off icon and "Couldn't save. Trying again by itself." in orange ink with an underlined "Try now". An optional detail follows in ink-3 after a middle dot.
- **Selected row:** see the Tonal Selection Rule; the same treatment marks the open draft in the Drafts menu and the chosen row in the work area. In the workbench's sand list column a list-style table hides its header and rounds each cell to the field radius, so the selected row is a white segment tile.
- **Admin overrides:** in the admin panel a workbench that opens the page cancels the work area's padding and runs edge to edge at the window's height; one under other content keeps the sides and is ruled off by a hairline above it.

### Menus and popovers
- **Menu:** one quiet ⋯ icon button (no fill until hovered or open, then the field fill). It opens a panel 6px below, held to its right edge (or its left, `AlignStart`); when the panel would run past the bottom of what holds it (the window, the phone's tab bar, or a sheet or pane that scrolls), and there is room above, it opens 6px above instead (`menu__panel--up`, placed as it opens). When it fits neither way inside a short sheet, it is placed in the window instead, above or below its button, so it is never cut off; only when the window itself is too short does it cap its height and scroll within (#69). The panel is at least 15rem and at most 22rem wide: the surface at the 12px radius with the Pop shadow, 6px in. Each item is a 44px row at the 9px radius: a small leading icon in accent (a check for the chosen one), the label at row size and 600, and an optional caption line in ink-2 under it saying what it does. Hover and focus take Selected Fill. A rule (hairline) starts a group; the danger item comes last after one, its words and icon in danger. Arrow keys, Home and End move through it; Escape or a click outside closes it. A menu with words (`Text`, `Chevron`) is a button showing the current choice with a chevron that turns over while it's open; `Block` makes it full width at the 12px radius.
- **Role menu:** the menu with words, showing the person's role ("Team", "Leader"); each role an item with what it allows under it, the current one ticked, and *Remove* in red last. It applies at once. Several roles at once stay a tick list.
- **Popover:** the same surface for a small panel that isn't a list of actions. **Account menu:** opened from the sidebar's Account row, a 312px popover fixed 12px from the window's left and 48px from its foot, at the 14px radius and 12px in: the person's name at row size and email in caption ink-2 over a hairline, *Light or dark* (`SchemeChoice` with its label), a rule, then *Your account* and *Sign out* as menu rows.

### Putting things in order
`ReorderList`: rows a person puts in order, each move applying at once (no save).
- **Rows:** at least 56px on the surface, hairline-divided (none under the last), 8px between parts: the grip, the row's own content (a toggle row inside drops its own hairline), an optional trailing slot, then a small ⋯ menu ("Move …").
- **Grip:** a 44px target pulled 8px into the gutter, the small grip icon in ink-3, the grab cursor. In `Quiet` (the admin calendars) it sits inside the row's left edge, 16px wide, so the chosen row's fill holds it; the row's words start 20px in, as the column's other items do. Only the grip starts a drag (touch or mouse), so the page still scrolls by the rest of the row.
- **Lifted row:** while dragged it follows the pointer, takes the field radius and the Pop shadow above its neighbours, its grip turning ink; the others slide out of its way over 160ms on the ease-out curve. Letting go puts it there: the list draws the new order at once, holding it until its caller's list changes, and the dropped row settles the last few pixels into its slot, so nothing snaps back or waits for the server first (#58, #62, 2026-11-12). On the phone a queued change shows at once and stays until Sync confirms it; the page's own tab strip changes once, at the drop.
- **Move up and Move down:** each row's ⋯ menu, with the chevron icons, for a keyboard or screen reader; Move up is off on the first row and Move down on the last. Every move is announced politely ("Moved News to 2 of 4.").
- **Preview strip** (optional, `Preview`): a line above the list showing the result as it will look, following the drag live. A ministry's tabs show as caption-size words at 600 in ink-3, 20px apart, over a hairline 6px down; the fixed first one (About) is in ink with the 3px marker underline, as the tab strip's chosen tab. Tabs that are off are left out of it.

### Settings groups
`Section Grouped`, with an optional `Foot`. **Admin:** a card the full width of its pane (2026-11-12, #71), short fields two to a line in a `ui-pair--wide` (two columns once each can be 18rem, one on a phone), on the surface at the 14px radius, ringed by a hairline and lifted, 20px below what's above: its title at label size, 700 and ink (so the page or sheet title leads; 2026-11-12) and its lead inside, 16 by 20px in, then its fields (20px in at the foot), then a sand foot under a hairline (sand at 55% over the surface, 12 by 20px in) holding the group's save bar or the caption "These change straight away". **Phone:** an inset group: a caption-size heading in ink-2 16px in, the rows on the group colour (white on a sand page in light, a raised panel on the night page in dark) at the 14px radius, 12 by 16px in, with the first and last hairlines hidden; a foot is a caption line in ink-2 under the group, or the group's save bar (a quiet button there keeps the group fill). A page of grouped sections takes the group ground. On both, the last row of a group (a list row or a toggle row, also the last in a form inside it) has no hairline under it; the card's own ring is the only edge (2026-11-12). A small ⋯ menu in a card's head (an account's actions) adds no height to the head (2026-11-13).

### Date and time fields
In the admin panel a date or time is a text field you type into ("Like 15 Nov", "Like 7:30pm"), tabular, with a 40px button at its right end (a calendar or clock icon in ink-2, Selected Fill on hover and while open). What it understood reads back under the field in caption ink-2 led by an accent check; something it can't read is a problem line. The date popover is 344px on the surface at the 14px radius, 12px in, with the Pop shadow, fading in over 160ms: the month at label size and 700 between 36px step buttons, micro weekday heads (Sunday in accent), then 36px days at caption 600 (days outside the month in ink-3 at 400, Sundays in accent, today a 2px primary ring, the chosen day primary with on-primary), and quick choices as filter chips under a hairline (only those inside the field's range, none with `Shortcuts="false"`). The month heading is a button with a small chevron (Selected Fill on hover): it opens the twelve months as a 3-column grid of 56px tiles under a year stepper, and the year opens twenty years as a 4-column grid of 44px tiles with ‹ › stepping twenty at a time; the shown one takes Selected Fill, the chosen one primary, this month or year the 2px primary ring, and any wholly out of range is ink-3 at 400. Choosing a year goes back to its months, a month back to its days; Escape steps back a level before it closes. The time popover is a list of every 15 minutes, up to 260px tall, the chosen time on Selected Fill with an accent check. `DateTimeField` puts both under one label. On the phone each is the native wheel.

### Picture viewer
A picture opened bigger (`PictureOpen` on the thumbnail, a zoom cursor) fills the whole window on the viewer backdrop (near-black at 97%), in white: the title line ("Picture 2 of 5 · …") in caption at 600 at 78%, Download and Close as 44px round controls on white at 12% (22% on hover), the picture contained between the previous and next controls, and a filmstrip of 56px thumbnails at 60% (the current one whole, ringed 3px in marker). Arrow keys, Home and End move through them, Escape closes, a swipe across turns and a swipe down closes. Under 640px the step controls go and the thumbnails are 44px.

### Boards
`Board` and `BoardCard`, one board for anything that moves through stages (feedback tickets). Sand columns at the 14px radius, 12px in, 12px apart, at least min(62vh, 640px) tall and 180px wide (a narrow column 160px; `Compact` keeps them at 200px beside an open record so they stay readable), scrolling sideways when they don't fit. A head at label size and 700 with its count in a page-coloured capsule (pill size, 600, ink-2, tabular). An empty column says so in a caption ink-2 sentence inside a dashed frame (1px of ink-3 at 45%, the tile radius); a column can show only its latest and say how many more; *Add one here* is a caption 600 accent link at its foot. A column that folds becomes a 46px strip with its name and count set vertically. A column being dropped on gets a 2px inset accent ring; cards move by dragging, or Alt and the arrow keys. A card is the surface at the tile radius, 12 by 16px in, ringed by a hairline (stronger on hover): an optional 76px picture at the 9px radius on strong sand, a tabular caption meta line in ink-3, the title at row size and 600, then its labels. The open card is ringed 2px in marker.

### App styling
The app's look on the desk: the app as everyone sees it, said in one sentence and shown on a phone, with drafts as the way a change travels, behind one menu. One layout for every state (the Still Frame Rule).
- **Title band:** "App styling" at headline size over one sentence in ink-2 at row size whose facts are ink at 600 ("Everyone sees **the church's brand**, **11 tiles** on Home and **4 tabs**."), led by the event's draft when one is on for everyone. In a draft the title is the draft's name and the line becomes the save line: the save status with who sees it after a middle dot, a middle dot in ink-3, *Back to what everyone sees* as a caption link at 600, and the undo note when there is one. The actions in order: Undo and Redo as quiet icon buttons, Share and More (their words hidden under 1480px), *Publish now* or *Ask to publish* as the one primary button, then the Drafts menu.
- **Drafts menu:** a quiet button reading "Drafts", the count of open drafts in a 20px orange capsule (pill type, on-marker, tabular) and a chevron. It opens a popover 8px below, held to its right edge: the surface colour at the 14px radius with the lift, up to 440px wide and 70% of the window tall (at most 640px), padded 18 by 20px, its groups 14px apart, scrolling on its own; Escape or a click outside closes it. Inside are the draft list's groups, each under a label-size 700 head in ink-2: *On for everyone, or on a date* (with a pill saying when), *Waiting to publish* (with its count), *Open* (with a small quiet *New draft*, or a caption saying what starting a draft means when none is open), then *Published recently*, folded: a label-size 700 toggle in ink-2 with a chevron and its count in tabular ink-3, opening to quiet rows, an *Undone* pill on one that was undone.
- **Phone column:** the app's own Home on the phone frame, scaled to fit the column and never below half size. Outside a draft it shows what everyone sees; inside one it is the Hub canvas and the Tab bar canvas. Clicking a part of it opens that part in the panel.
- **Preview chips:** under the phone, centred and wrapping (6 by 10px gaps, up to 40rem), one sentence at caption size in ink-2 at 600: "Preview as [a visitor] in [light] mode, on an [iPhone 15 Pro] for [today]". Each bracket is a 34px sand capsule in ink that sizes to its word (a select, or a date field for the day); *Back to today* follows as a caption link once another day is chosen. They change only the phone.
- **Panel:** outside a draft, *What everyone sees* (Colours with the theme's swatches, Home screen and Tab bar as rows with a chevron) and *Coming up*; inside one, *What this draft changes* (each part with its count of changes) or the chosen part under a deep-aqua back link. Groups are labelled by an uppercase micro head in ink-3 (600, +0.06em), the Places directory's group head; a part opens on a panel head (title type over a caption lead in ink-2); and guidance closes a group as a caption in ink-3 ("Click a part of the phone, or one of these, to change it.").
- **Coming up:** a hairline list of what changes in the next six months, each row at least 48px (12px above and below): a 6.5rem day column in ink-2 at 600 (tabular), then the words at caption size; hover tints it sand at 60%, and a click shows the app on that day on the phone. Five show, the rest behind *Show all n*; with nothing ahead one quiet caption says so.
- **Theme cards:** *The app's colours* as cards two to a row (10px gaps), each a sand card at the 14px radius, at least 112px tall and padded 14px: the theme's swatches, its name at row size and 600, a caption note in ink-2, and a pill-size 700 state line ("Everyone sees it now" in ink-2, "Chosen in this draft" in orange ink). The chosen card turns the surface colour with an inset 2px marker ring; a theme made here carries *Change it* as a caption link. *Make a theme* closes the grid as an outlined card (an inset hairline) in deep aqua. A theme with dates says so in a notice under the cards, with *Change the dates*.
- **Theme swatches:** three overlapping 22px discs (6px overlap, each ringed 2px in the page colour) of a palette's colours, a made theme's being its masthead, accent and marker. One component, shared with the app's Appearance page, so a theme looks the same wherever it is chosen.

### Admin sidebar (signature)
The admin panel's masthead, turned on its side. A 248px navy column, 22px between its parts: the mark in beige at 30px beside "Good Shepherd" at 700, then the navigation, 18px between its groups. Pages sit in areas cut by the job staff are doing, in the order they reach for them (owner, 2026-11-12): Sundays & sermons, This week, Ministries & groups, Safety, People & access, The app, Technical, and the foot, set off by a 1px rule of the masthead tint and pushed to the bottom: actions a feature adds come first (Send feedback, for staff who may send it), then Account, always last, which opens the Account menu.
- **Jump to:** first, a 38px button on the masthead tint at the 9px radius: a small search icon, "Jump to…" at label size and 600 in white at 76%, and the key hint at its end ("⌘K" on a Mac, "Ctrl K" elsewhere) in beige micro at +0.04em. Hover lifts the fill to white at 18% and the text to white. It, ⌘K and Ctrl K open Jump to.
- **Home, then Pinned:** Home sits alone under Jump to. The pages someone pins follow under an uppercase beige micro label, "Pinned", in the order they were pinned; pins are kept with the account, so they follow it to any computer (owner, 2026-11-09). When there are pins, the areas get an "Everything" label of the same kind above them; without pins neither label shows.
- **Area heads:** each area opens on a 36px button at the 9px radius: a beige chevron that turns 90° when the area is open (200ms, ease-out), the area's name at label size and 700 in white, and a count at the end. A folded area shows the orange count capsule of what's waiting inside it when there is any, otherwise a muted count of its pages (pill size at 600, white at 76%, tabular); an open one shows the page count. Hover fills the head with the masthead tint at 60%.
- **Folding:** an area starts open when the person sees 8 pages or fewer, folded otherwise, and folding or unfolding it is remembered on that computer. The area of the page you're on opens by itself, without being remembered. An opening area's pages fade in and slide 4px down into place over 200ms (ease-out). Pages in an area are in alphabetical order and indent 16px, their icon 8px from the label (2026-11-13); page names stay short enough for one line (*Import sermons*).
- **Links:** 38px, label type at 600 with a small icon, in white at 76%, the title cut with an ellipsis; hover fills them with the masthead tint at 60%, and the current link takes the full tint and white text while a 3px orange bar, rounded on its outer end, scales in from its centre at the sidebar's left edge (240ms, ease-out). A pinned page is marked current only under Pinned; its row in its area stays a plain link. A count of things waiting sits at the end of a link in an orange capsule (20px, pill type, tabular, on-marker text), only when the count is above zero, and refreshes as you move between pages. A feature adds its page's count through `INavCounts`, which the sidebar adds to the office week's.
- **Pin:** every page under Pinned or in an area carries a 30px pin button at the 9px radius, 4px in from the row's end, in white at 76%. It appears when the row is hovered or holds focus (the link makes room for it) and is always shown on touch screens; hover fills it with the masthead tint. A pinned page's pin is orange and filled. Pinning is only in the sidebar, not on each page (owner, 2026-11-09).
- **Actions in the foot:** a row that opens a sheet over the page instead of going somewhere (a `SidebarAction` a feature registers, shown only with its permission). It is a button drawn exactly as a link (full width, left-aligned, the same icon and label), marked current while its sheet is open, and says it opens a dialog. Send feedback is the first, with the speech-bubble icon (2026-11-11); Account is the last and opens the Account menu as a popover rather than a sheet.
- **A failed save:** if pins can't be saved, a line on the masthead tint at the 9px radius (8 by 10px padding, caption size, white) says "Couldn't save your pins." with "Try now" in beige at 700, underlined.

Focus rings in the sidebar, and on the narrow window's Menu button, are orange.

### Jump to
Finds any page the person's role opens, by its name, its area or its summary. A `Sheet` named "Jump to a page", with no title, that sits near the top of the window on windows 720px and wider (max(48px, 12vh) from the top) rather than centred. Inside, 10px apart: the shared search field at full width ("Type the name of a page", focused on open), then up to 12 matches as 52px rows at the 9px radius (6 by 10px padding, 12px gaps): the page's icon on a 34px sand tile at the 10px radius, its title at row size and 600, and its area and summary ("The app · …") at caption size in ink-2, cut with an ellipsis. The active row takes sand, its tile turns to the surface colour and a chevron in ink-3 appears at its end. The arrow keys move the active row (wrapping, scrolled into view), and Enter or a click opens the page and closes the sheet; the list scrolls past min(420px, 60vh). A polite status says how many pages match; when none do it shows "No page called “…” that your role opens." at caption size in ink-2 (14 by 10px padding). ⌘K or Ctrl K opens and closes it from anywhere unless another sheet is open, and focus returns to where it was on close.

### Title band
The page head in the admin panel: a headline-size title (balanced, focus target for navigation), an optional caption lead in ink-2 up to 72ch, or a lead slot, an optional back link in deep aqua above the title, and actions at the end. It also sets the browser title as "Page · Good Shepherd admin".

### Office week (signature)
- **Day rail:** one shared rail, a week on the home page and a fortnight on Announcements. A sand track (14px radius, 10px padding, 8px column gap, at least 196px tall) of white day columns at the 10px radius that run the rail's full height. Each column carries an uppercase micro weekday in ink-3 ("Today" on today, in orange ink) over a tabular title-large date. Today is outlined with an inset 2px primary ring; days before today keep their column with the weekday and date in ink-3.
- **Fortnight:** fourteen days from this week's Monday, each at least 56px wide with a 6px gap and 8 by 8px padding, the rail as tall as its contents and scrolling sideways when the days don't fit; its capsules are tighter (5 by 7px) and set entirely in pill type. A span covering the whole rail reads "The whole fortnight" instead of "All week".
- **Capsules:** each dated item is a capsule (9px radius, 6 by 10px padding, caption) laid across the days it covers: a 600 title cut off with an ellipsis over a tabular pill-size date range in the capsule's own text colour. Capsules pack into bands: each takes the highest band where it doesn't touch another, so items on different days share a line. Three kinds: **Everyone** (what everyone sees) in a primary fill with on-primary text; **Event layer** in accent at 10% on the surface with accent text; **Draft** (not out yet) in marker at 24% on the surface with marker-ink text. A capsule presses to 0.98; the chosen one has a 2px marker outline at a 2px offset.
- **Moments:** an item at one time rather than across days (an announcement's send time) reserves its own day and the next two (fewer at the rail's end), so its title has room for two lines; another item that close moves down a band. Its second line is the time alone ("7am"); the full day and time are in its tooltip.
- **Legend and quiet state:** a caption legend in ink-3 with 14 by 10px swatches appears only when something is dated, and lists only the kinds on the rail, each in the page's own words (the home page's Draft reads "Not out yet: a draft or a scheduled announcement"). With nothing dated, one centred caption line in ink-3 sits across the rail.
- **Needs you:** hairline rows with a marker-tinted tile (orange at 30%, orange-ink pencil), a 600 title link, a caption detail with a relative date, and a small quiet button for the action. With nothing waiting, a single hairline-ruled line with a deep-aqua check.
- **Your areas:** hairline rows linking to each area: a sand tile with a primary icon, a 600 title, a one-line caption purpose in ink-2, and an ink-3 chevron; hover fills the row with sand at 60%.

### News page (phone)
`/news`, a tab of its own: the masthead "News" with the note "From the church and the ministries you're in", then `NewsList` in a first section. Owner's pick on the decision page, 2026-11-13.
- **Whose news:** a sticky tab strip in its link mode, *The church* (`/news`) and *Your ministries* (`/news/yours`, the news of every ministry you're in, gathered). It shows only to someone in a ministry; anyone else sees church news with no tabs.
- **Urgent today:** a post sent as urgent today sits above everything, one band each: marker at 18% on the page at the 14px radius, 12px in, 4px above and 12px below, no hairline. A 34px tile at the 10px radius (marker at 30%, a marker-ink News icon), the title at row size and 600, "Today · " and the start of the text on one line in marker-ink at caption size, and a marker-ink chevron.
- **The lead:** the newest other post, its first picture across the column at 16:9 on the 14px radius (sand-strong while it loads), then the title at title size and 700 (1.2, -0.015em), up to two lines of text in caption ink-2 (1.45), and the meta line. No picture, no frame: it starts with the title.
- **Rows:** every other post, newest first, hairline below, 16px above and below: the title at row size and 600 (1.3), two lines of text in caption ink-2, the meta line, and its first picture at the right as an 88px square at the 9px radius. The text is cut at 160 characters before the clamp.
- **Meta line:** caption ink-2 wrapping 4 by 8px, 2px down: "From …" in accent at 600 with the Ministries icon for a ministry's post, a state in marker-ink at 600 when it has one, and how long ago ("· edited"). Every story is a link that presses to 85% opacity.
- **Staff tools** (on *The church*, for those who may write church news): a block primary "New post" with a plus, then, while anything waits, a warm list row "3 posts to look at" ("Drafts, posts waiting to be published and ministries' asks") with a chevron to *To look at* (`/news/desk`): sections *Ministries asking*, *Waiting to be published*, *Scheduled* and *Being written*, each a lead line over rows (a warm people icon for an ask, a clock for scheduled, a pencil otherwise). 8px between the tools, 12px under them.
- **Empty:** the News empty state, "No news yet", saying "News for the whole church shows here when it's posted." on *The church* and "News from the ministries you're part of shows here." on *Your ministries*.

### A post's page
`PostView`, the same for church news (`/news/{id}`), a ministry's post on the phone, and the desk's preview.
- **The strip:** with two or more pictures or videos (YouTube links count), `MediaStrip` leads flush under the solid top bar, across the gutters: frames at 4:3 on black, swiped one at a time (snapping, no scrollbar). A count pill ("2 of 5", pill size, 600, tabular, white on black at 55%, full radius) sits 12px in from the bottom right above the dots; under the frames, 6px dots 6px apart, the shown one in accent, the rest in line colour. A picture opens the picture viewer. With one picture or video, there is no strip: it shows in place in the text.
- **Head:** 16px above and below: the title at title size and 700 (1.2, -0.015em, balanced), then a caption ink-2 line wrapping 4 by 12px: who it's from in accent at 600 with the Ministries icon (a link to that ministry's News tab), who wrote it, and the date in long form ("· edited"). States sit under it as pills: a church post's state in marker ("Scheduled", "Being written"); a ministry post's *Important* in marker, its church news line in accent, and who it's for, quiet.
- **The band** (*default, owner to review*, #88; owner's words "incredibly barren looking"): with `Band`, no strip leading and no picture opening the post, the title moves onto a navy band (masthead with on-masthead) that continues the top bar, flush across the gutters, 20px above and 24px below, the title at headline size and 700 (1.18); who and when sit 12px under it on the page. A post that opens with a picture or leads with the strip has no band. The app's post page, a ministry's post and both *Preview on a phone* panes set it.
- **Body:** the rich text at body size at 1.6 up to 65ch, paragraphs 0.85em apart, headings `#` to `###` at 1.35, 1.18 and 1.05em and 700 (1.3em above, 0.45em below). Media blocks in the text: a picture at the 9px radius with a caption ink-2 caption 6px under; a gallery as two columns 6px apart of 4:3 pictures (with three, the first spans both at 16:9); a video on black at the 9px radius; audio and files on sand at the 9px radius, 12px in. A file is a link row at least 56px tall: a 34px tile at the 10px radius (live colour at 16%, an accent icon), its name at row size and 600 over its size in caption ink-2. Code sits on sand in `--font-code`; a table in a hairline box at the 9px radius, its head row on sand; a highlight on `--highlight`. Footnotes are numbered in accent at 700 and gathered at the end in caption ink-2 over a hairline.
- **Cards:** each part split by `===` is a card; between cards a sand bar 8px tall runs across the page, gutter to gutter, with 24px above and below it.
- **App cards:** `AppCardFrame` in one of three looks, stored as `look=` on the card's line (owner's pick of all three, 2026-11-14; `.impeccable/mocks/app-embeds/`). 6px above it and 20px below. Under the title (row size, 700, 1.3, balanced; a link to the thing) a caption meta line at 600 in the kind's tone with the kind's icon: "Event", "Event · regularly", "Sermon · Series, part 2", "Ministry · You lead it". Then caption ink-2 detail lines with an ink-3 icon (when and where; who, the day and the length). Never a kicker above the title.
  - *Side rule* (the default, writes nothing extra): a 4px rule in the kind's tone at the left, 12px in; a 68px square at the right at the 9px radius (the picture, or a solid tile); its actions as words (`Link` at caption size, 16px apart).
  - *Timetable line* (`look=line`): a 64px date block (on the kind's tile colour) or the picture or tile at the left, hairlines above and below, 12px in; small buttons, the main one primary and the rest tinted, with icons.
  - *Box* (`look=box`): sand at the 14px radius, a 16:7 cover on top capped at 220px (16:9 for a sermon, so its artwork is never cropped), the title at lead size, small buttons with the main one widened to fill the row.
  - *Tones:* an event and a ministry in accent on live tiles; a sermon in primary; a Life Group in marker-ink on marker. With no picture the tile or cover is solid in the kind's colour with its icon (a regular event's is the repeat icon, a ministry's its own icon), so it reads as a design, not a gap. A ministry's own colour is not used.
  - *States:* changed, a caption line at 600 in marker-ink ("Changed: …"); cancelled, the title struck in ink-2, a caption line at 700 in danger ("Cancelled: …"), the tile or date in danger and a cover in greyscale; over, a quiet sand line ("An event on … that's over now."); gone or not visible, nothing for readers. A sermon shows a progress track and time left once started, and "Heard" once finished; it opens the sermon, never plays in place.
  - *In the editor only:* when the post may be for more people than can see it, a caption ink-2 note under the card with the eye-off icon, "Only people who can see this event will see its card." (it still saves; owner, 2026-11-14); a card for something gone is a quiet sand line ("This event isn't in the app any more, or you can't see it. Take it out or choose another.").
- **Tables:** unchanged by the editor's table panel; still the hairline box at the 9px radius with its head row on sand.
- **Its link:** a block button 20px under the text, up to 65ch: the places icon for a page in the app, external-link for the web, named by the post ("Open it" when unnamed).
- **Actions:** a ⋯ menu in the top bar, "More for this post": *Change the post*, then, after a rule and in red, *Delete the post…*. On church news, a ministry's post shared into it offers only *Take it off church news…*. On a ministry's post, staff who publish also get *Put it in church news…* (or *Take it off church news…*) with the megaphone, confirmed on a sheet with *Send a notification* and Not now. Deleting confirms on a sheet with Keep it.
- **An ask:** staff who publish see a card under the post, "… asks for this to go in church news", with *Send a notification* (on), then block buttons *Put it in church news* and, quiet, *Keep it in …*.
- **Gone:** "This post isn't here any more", pointing to the News page (or the ministry's News tab).

### Writing a post
`PostEditor` (Platform.Ui), the same in the app's `/news/write`, a ministry's post sheet on the phone and the admin panel's writing pages, where it sits in `WritingDesk` (owner's picks on the decision page, 2026-11-13, #83 to #85, and 2026-11-14, #86 to #88). A post is a title and one editor; the blocks, their handles and the Add row are gone. 12px between its parts.
- **Title:** a text field, "Give it a title". On a writing page it drops the field and reads as the page's headline: headline size at 700, -0.015em, no fill and no side padding, over a hairline.
- **The post:** one `RichTextField` (see Formatted text) with `Headings` and `RichTextMedia.Posts`, labelled "The post", its placeholder "Write the post. Formatting is in the bar; pictures, video, files and more are under Insert." Pictures, galleries, videos, video links and files sit in place in the text with their captions; `===` splits it into cards.
- **Settings row** (phone): who it's for, the notification and when, summed up on one sand row 56px tall at the 9px radius ("Everyone · Notification on · Now", with a people icon and a chevron), opening a sheet with Done. Inside a sheet the settings sit inline instead, under a hairline with their caption label at 700 ("Who it's for and telling them"). On a writing page they are the desk's settings column.
- **Church news settings:** *Who it's for* (the audience picker), *Send a notification* with *What it says*, *Its line* and *Urgent* (or, once sent, a caption line saying when it went), *A link at the end (optional)* with *The link's words*, and for publishers, *When* as Now or Later with *Publish it at*.
- **Saving:** `SaveBar` with Cancel at the left, its primary named for the result: "Post for everyone", "Post it", "Schedule it", "Ask for it to be published" (someone who can't publish), or "Save" once published; *Send to my phone* (quiet) after Cancel on a saved draft. Without a title it says "Give it a title to post it." On a writing page it is the desk's foot.

### Formatted text
`RichTextField` (Platform.Ui Content): live Markdown on CodeMirror 6, loaded only when an editor opens. Bold looks bold and lists look like lists as you type, for staff who don't know Markdown (owner's pick of three on the decision page, 2026-11-13); what's stored is Markdown, as before. It is the one editor for formatted text from now on. Formatting lives in one row of quiet icons and everything that goes *into* the text is behind one navy Insert (owner's pick, 2026-11-14, #86): never an Add row of chips under the editor, block cards with handles, or a bar of thirty icons.
- **The bar:** a toolbar "Formatting", sticky at the top of what scrolls, on the surface with a hairline edge under it, 4px between: with `Headings`, the *Text style* select (Text, Heading 1, Heading 2, Heading 3, the current one ticked) and a divider; Bold, Italic, Strikethrough, Highlight; a divider; Bulleted list, Numbered list, Quote, Link, Code; ⋯ *More formatting* (Superscript "x²", Subscript "H₂O"); and, when the place allows anything, **Insert** as a small navy primary with its icon, pushed to the right. Each tool is a quiet icon `Button` with `Pressed` (see Inputs / Fields, *Rich text*); tooltips give the shortcut (⌘B, ⌘I, ⌘⇧X, ⌘⇧H, ⌘⇧8, ⌘⇧7, ⌘⇧9, ⌘K, ⌘E; ⌘⌥0 to 3 for the styles).
- **Insert:** the shared `Menu` with group titles: *Pictures and media* (Picture "From the library or upload", Video "Uploaded", Video link "YouTube, Vimeo", File), each through Media's chooser with Upload; *Layout* (Split into cards, Line across, Table, Code block); *In the text* (Footnote, Emoji). What it offers is the place's `RichTextMedia`: `Posts` everything; `Layout` the layout and in-the-text choices only; none, no Insert at all. Audio is never offered (existing audio still plays).
- **The text:** a sand field (see Inputs / Fields, *Rich text*), at least 96px tall, a deep-aqua caret, an ink-3 placeholder, selection in deep aqua at 24%.
- **As it reads:** bold at 700, italic, strikethrough in ink-2, highlight on `--highlight`, code on sand in `--font-code`, a link in deep aqua underlined 3px below, headings at 1.35, 1.18 and 1.05em and 700 (`#` biggest, owner: "more # makes it smaller"), a quote indented 12px behind a 3px hairline rule in ink-2, a list item hanging 1.2em with its bullet or number in ink. The marks (`**`, `#`, `>`, `==`) show only on the line with the cursor, in ink-3 at 60%; code fences fold away off it, table pipes and footnote lines are faint. A line across is a hairline; a card split is a dashed double rule round "Next card". A blank line is shortened (0.7 of a line; 0.2 inside a list; 0.4 beside a rule).
- **Media in place:** each picture, gallery (two columns, 4:3), video or video link (a 16:9 black tile with a round play mark), or file (a row with a 34px accent tile) shows where it sits, at the 9px radius. Under it a caption input ("Add a caption", caption size in ink-2, a deep-aqua underline while typing; Enter or Escape returns to the text) and its library path in micro `--font-code` ink-3 (`~/Main Photos/church-ministry.jpg`; the stored `media:<id>` never shows, owner: "handle that invisibly"). At its top right a pill on the surface with the lift shadow holds 32px round controls: *Drag to move it*, *Add a picture beside it* (pictures), *Replace it* and *Take it out*. Chosen, it is ringed 2px in deep aqua.
- **From the app:** Insert's fourth group, *From the app*, with Event, Sermon, Ministry and Life Group (each offered only where a feature registers its card, and only with `RichTextMedia.AppCards`, which `Posts` includes). Each opens `AppCardPicker`, a sheet "Put in a card for an event": a search field ("Type part of its name") over rows with the picture, only what the writer can see ("Only events you can see are listed."). A regular event then asks "It's a regular event. Which should the card show?": *Just this one date*, *The next date, rolling on*, *No particular date*, with *Back to the list* and *Put it in*. On the phone the search doesn't take focus, so the keyboard stays down until asked for.
- **Cards in place:** each card is drawn as the card itself in its look, inert while writing, with its pill of tools above it at the right, never over its title: *Drag to move it*, *Change how it looks* (a panel under the bar: Side rule, Timetable line and Box as `Segmented Rows` with a line each, then Done), *Replace it* and *Take it out*. A Box shows on the surface with a hairline ring so it stands off the sand field.
- **Tables in place:** a table is drawn as the post page's table, with its pill above it (drag, *Change the table*, *Take it out*). Pressing it, or the pencil, opens a panel under the bar: a grid of cells on the field colour at the 9px radius, the head row at 700 on sand-strong, × after each row and under each column to take it out (a table keeps a head and one row), then tinted *Add a row* and *Add a column*, and at the right Cancel and *Done*. Insert → Table opens the same panel, its primary *Put it in*.
- **A link:** Link opens a panel under the bar: a *Link to* field (https, a page in the app, mailto or tel, or a problem line saying so) and small Cancel and *Add the link* at the right, 8px apart. Video link and Emoji open panels the same way (emoji a grid of 40px squares).
- **Pasting:** pasting from Word or Google Docs keeps bold, italic, lists and links, and everything else comes in as plain text; `- `, `# ` and `> ` start a list, heading or quote for those who know.
- **On the phone:** the bar is one row that slides sideways without a scrollbar, Insert first and pinned at its left edge; Insert opens a sheet of tiles under its group names, and the style and ⋯ open sheets of rows. While the field has focus and the keyboard is up, the bar docks on the keyboard, full width on the surface over a hairline, its buttons 44 by 40px; the page gains room at its foot so the text clears it.
- **Not moved yet:** events' *About*, a ministry's *About* and the app's formatted settings keep `RichTextEditor` until the owner decides; moving one is a swap of the component.
- **In chat bubbles:** messages render the chat scope (`RichTextScope.Chat`) in `.chat-text`: paragraphs, lists and quotes 6px apart; in your own bubbles links, code and highlight take the on-primary colour (code and highlight on it at 18%, a quote's rule at 45%). The composer stays plain (*default, owner to review*: a bar didn't fit beside attach and send).

### Church news desk (admin)
`/news` in the admin panel's This week, a `TitleBand` "Church news" with *Earlier announcements* (quiet, with a clock, for those who may still see them) and a primary *New post*, which opens the writing page `/news/write` (see Writing on a page). The desk is the list; posts are written on their own page (owner, 2026-11-13, #83).
- **Rail:** one `FilterChips Rail` group with counts: *Ministries asking*, *Being written*, *Scheduled*, *Published*, *Kept in ministries*. It opens on the asks when there are any, then what's being written, then published. On a laptop the rail folds into a *Show* button.
- **List:** list rows with the title over a line ("Kids church · asked by Sam", "By Ruth", "Publishes tomorrow 9am", or "From …" and the date), and at the right a pill (*Asking* and *Waiting to be published* in marker, *Urgent* in marker, *From a ministry* in accent) and a 64 by 48px thumbnail at the 9px radius. The chosen row is current. Empty: "Posts show here as they're written, asked for, scheduled and published."
- **Record pane:** a `PaneHead` with the title, a state line ("Published 13 November, with a notification."), and a ⋯ menu: *Back to writing* (scheduled, or waiting to be published), then after a rule *Delete the post…* or, for a ministry's post, *Take it off church news…*. Choosing a post that may be changed opens it on its writing page, `/news/{id}/change` (so does *Change the post* in the ⋯ menu); the pane is for what's answered rather than written: a ministry's ask and a shared post, and anything you can't change, shown as the post's page, up to 720px, its strip at the 14px radius.
- **An ask:** a grouped section "… asks for this to go in church news" with *Send a notification*, and its foot split end to end: *Keep it in …* (quiet) at the left, *Put it in church news* at the right.
- **New post:** the writing page, "New post" in its title band with Back to *Church news* and no ⋯ menu until it's saved; an address with `?opens=` starts one with that link.

### Writing on a page (admin)
`WritingDesk` (Platform.Ui), where the admin panel writes something long on a page of its own rather than in the record pane: church news at `/news/write` and `/news/{id}/change`, a ministry's post at `/ministries/{id}/news/write` and `…/{post}/change` (owner's pick on the decision page, 2026-11-13, #83, with the steer "allow collapsing settings"). Its slots are the writing, `Preview`, `Settings` (with `SettingsTitle` and `SettingsSummary`) and `Foot`.
- **Above it:** a `TitleBand` with Back to where you came from, "New post" or "Change the post", and, once the item exists, its ⋯ menu (a new post has nothing in it to do). Under the title band the desk runs edge to edge in the work area (to the 40px sides and the 64px foot, or the gutter and 48px at 900px and under), ruled off by a hairline.
- **The page:** a white page up to 800px on sand, 24px below the top and 32px from the sides, 6px in at the top, 24px at the sides and 48px below, rounded 14px at its top only, drawn by a hairline and the lift shadow, and at least the window's height. Its head is a tab strip, *Write* and *Preview on a phone* (owner: "tab it, like is often for markdown editors"), 20px above the writing; the preview is the item in a phone frame, centred.
- **The settings column:** 360px of white at the right, ruled off by a hairline, sticky at the top and scrolling on its own, 20px in. Its head is 36px tall: the title as a caption at 700 in ink-2 ("Who it's for and telling them") and at the right a quiet fold button (`AppIcons.HideSidePane`, "Hide these to give the writing more room"). The settings are grouped `Section` cards 12px apart, their fields 16px apart.
- **Folded:** the column narrows to 64px holding only the unfold arrow (`AppIcons.ShowSidePane`), and the settings sum up at the right end of the tabs in one accent line at caption size and 600 ("Everyone · Notification on · Now"), underlined on hover; pressing it unfolds them.
- **The save bar:** the `SaveBar` in the foot, sticky at the bottom of the window on white over a hairline, 12px above and 16px below its buttons, 20px at the sides; it is as tall as what it holds. It sits under the column, or across the page when folded (32px at the sides).
- **1228px and under:** one column on sand: the page (rounded all round, as tall as its writing), then the settings under it, up to 760px and centred, 24 by 32px in, always open (no fold arrow or summary), then the save bar across, 32px at the sides. At 760px and under the sides become the gutter, the page is 16px in at the sides with 24px below, and the column 20px in.

### Earlier announcements (admin)
`/announcements`, read only, reached from the church news desk (church news replaced the announcements desk, owner 2026-11-13). A `TitleBand` "Earlier announcements" ("What the office sent before church news …") with a *Church news* button. A data table, newest first: the announcement at 600, when (short, with the time), phones reached and read by (numbers right-aligned). Choosing one opens a pane: its title, a state line ("Sent … to about 42 phones; 30 people have read it.", or recalled, or scheduled), the message in prose up to 65ch keeping its line breaks, and a ⋯ menu with *Don't send it* for one still scheduled and, in red after a rule, *Recall…*, which confirms on a sheet ("It stays here as a record"). Empty: "Nothing was sent before church news."

### Weekly digest desk
The admin's digest (`/digest`) in the split view, after the approved mock. Above the list, capped at 64rem: the controls as one grid, From (an 11rem date field at 42px, its label level with the others), How long (One week, Two weeks, Month, Custom, a segmented control with its caption label above, up to 26rem) and, for Custom, To (11rem); the range under them at row size and 600 in primary (tabular); then a "Calendars" caption label over the quiet toggle chips. Each item row is a grid, `16px 20px 84px minmax(0, 1fr) minmax(9rem, 14rem) 64px` with 12px between: grip, tick, a 16:9 thumbnail, the body (label, title with its pills, detail), the when (caption at 600 in primary, right-aligned and balanced, wrapping inside its column, the time under it at 500 in ink-2) and the move arrows (32px, ink-3, always shown, sand on hover, the outline when disabled). Every row's when ends at the same x. When the list is 600px or narrower the when drops under the body, left-aligned with the time after a " · ", and the thumbnail shrinks to 56 by 32px. Moving a row with its arrows keeps focus on the moved item and says where it went ("Moved Kids camp to 3 of 6") in a polite status line.

### Prayer desk (admin)
`/prayer` on the Workbench, under a `TitleBand` ("Prayer"). The rail holds the search ("Words or a name", hidden in Settings) over one `FilterChips Rail` group: New and For the wall with counts, On the wall, Followed up, Answered, Everything, then Settings; the pastoral team sees all of them, a wall approver only the wall's. The list is a data table of requests: the words cut to 70 characters at 600 over their pills (New, Asks for the wall in accent, Under 18 in marker, Get in touch quiet), then who it's from (quiet, one line) and how long ago it was sent. Choosing one opens the record pane:
- **Head:** a `PaneHead` titled by who sent it ("Sam S.", or no name), its lead when it was sent with the time; its ⋯ menu holds Delete.
- **Body,** up to 820px with 16px between parts: an Under 18 pill when it applies, the status as the care steps (the stage pipeline; a wall approver gets one strong status line instead), then their words at lead size (1.6 line height, up to 66ch) and any warning notice.
- **Groups,** 20px apart, each a `Section Grouped` card: The prayer wall, Getting in touch, Notes for the pastoral team (each note a hairline row of the words over its author and age in caption ink-3, with its own ⋯ menu to delete it; the note field and *Add the note* in the foot), and What's happened.
- **Marking it answered** asks for words for the wall in a sheet first (optional, up to 500 characters); the wall shows them under the request.

### Prayer desk settings (admin)
`/prayer`: the desk's last filter chip, *Settings*, shown to the pastoral team and to whoever may change prayer settings, sets when people hear that someone prayed for their request (2026-11-12, chosen on the decision page). Choosing it hides the search and the list, and the settings open in the work area, capped at 680px with 22px between parts.
- **Head:** "When people hear someone prayed" in title type at 700 (balanced), then a lead in body ink-2 at 1.55, up to 62ch, 10px under it.
- **Timeline:** one row of stops, each a 12px dot over its label in caption at 600, tabular, in ink-3, joined by a 2px strong-sand line. It starts at "On the wall" (a ring of accent, label ink-2), then each chosen time as an accent dot ("15m", "1h", "2d"), spaced by the square root of its gap so later, longer waits sit further apart, and ends at "Then every day at 12pm" (a 3px ink ring, label ink) when the daily time is on. It wraps rather than scrolling, each stop at least 56px.
- **Changing it:** "A few times after it goes on the wall" at label size and 700 over a caption ink-3 hint and the chip picker of times (written out, "2 hours"; its search up to 360px); then *Then once a day* as a toggle row and, when on, *Each day at* (7am to 8:30pm by half hours, up to 240px, hinted "Church time."). Save (`SaveBar`, which won't save with no time chosen) and a quiet small *Back to the usual times* when they differ. The page asks before losing changes.
- **Read only:** someone who may see it but not change it gets the timeline and a notice, "Only the pastoral team can change these."; everyone gets the closing caption ink-3 line "People can turn these off in Notification settings."

### A prayer request's page (phone)
A request's page has a solid top bar and no masthead. Under it, a full-width band in strong sand as the theme renders it (it reads a cooler sand than the group ground), 16px by the gutter with 12px between its lines:
- **Meta line:** caption at 600 in ink-2, its parts 12px apart and wrapping: when it was sent, who it's from ("Sam S.") and an Under 18 pill (marker); on the person's own page, when it was sent and how the wall shows them.
- **Status:** for the pastoral team, the care steps (New, Followed up, Answered) as a pressable stage pipeline; for a wall approver, one line at row size and 600 in ink. The person sees their own request read only (Sent, On the wall, Answered; Sent and Answered when they didn't ask for the wall) over the same strong line, and a ⋯ menu at the right of the meta line holds Take it off the prayer wall and Delete. Until it has sent, the band shows its `SaveStatus` instead of the steps.
- **Then** their words at lead size (1.6 line height), and the page takes the group ground for its inset groups: The prayer wall (the words as the wall shows them, the name as a segmented choice, *Shows until* as a date field; Save or *Put it on the wall* at the bottom right of the foot, *Not for the wall* or *Take it off the wall* as the bar's quiet action), Getting in touch, and Notes for the pastoral team.
- **Quiet buttons in a group's foot** keep the group colour as their fill, so they still read as buttons on the group ground.
- **The person's page** ends with a block *It was answered* button and a centred caption hint; it asks for a few words of thanks in a sheet.

### Data table
The admin's working list, in the workbench's list pane; its search and filters live in the rail when the page has one, otherwise in a toolbar row above. A full-width table at row-title size. Headers are caption-size 600 in ink-3 over a hairline; sortable headers are buttons whose arrow is hidden until hover and shows in orange ink on the sorted column (fading 160ms). Rows are divided by hairlines with 12px cells; numbers and dates align right in tabular figures, secondary cells are caption in ink-2, long text is cut off at 30ch. A selectable row shows a pointer, fills with selected-fill at 55% on hover and the full selected-fill when chosen; focus is an inset 2px deep-aqua ring, and the arrow keys move the selection. An empty table shows an empty state beneath it.

### Charts
- **Stat tile:** a hairline above, the value in display size at 700, a row-size 600 label in ink, then an optional caption change in ink-2 and explanation in ink-3. A value too small to show (Fewer than 5) is the quiet tile: title size in ink-2.
- **Ranked list:** a row-size title at 700 and a caption explanation in ink-3, over hairline rows (at least 40px, caption size): the rank in ink-3 tabular figures, the label in ink, the value in ink-2 tabular figures at the end. Unnumbered, it drops the rank, for counts that aren't a ranking (Insights' "How phones are set up").
- **Trend line:** the main series is a 2px deep-aqua line ending in a labelled dot, the comparison series a 2px dashed ink-3 line (its legend swatch dashed too), over hairline gridlines with pill-size tabular ticks, the plot capped at 760px. A hover crosshair shows a lifted tooltip. Columns are not focusable: the table of every number under the chart, behind a deep-aqua 600 summary, is the accessible view. With fewer than two points the chart is a sand panel at the 14px radius with one caption line in ink-2.

### Split view
`SplitView` is the workbench without a rail, kept for the pages not yet moved onto it (a drift ratchet counts them, and the count only goes down). The record is the workbench's record pane, a hairline-ruled column, not a card; at 760px and under it drops beneath the list with the line above it. Without a selection the list takes the full width.

### Places in the app
The app laid out on the desk: every place in the app, grouped by area, beside a phone showing the chosen one. Reached from the sidebar's The app area (link).
- **Directory:** the shared search field and filter chips (All and each area) wrapping above the list, then a group per area: an uppercase micro head in ink-3 at 600 (+0.06em) over hairline rows, each with its icon on a sand tile for pages or an accent tile for things in them (events, files), the name, where it is ("More → What's new") or its date, and who can open it as trailing text. The chosen row takes the Tonal Selection. A group cut short ends in a deep-aqua 600 "Show all …" at label size with a chevron (44px target). Nothing matching is an empty state. The same directory is the place picker in a wide sheet.
- **Phone column:** centred under the phone: the name in title type (balanced), where it is in caption ink-2, then who can open it as a 600 caption line in ink-2 with a people icon. Then the actions, centred and wrapping 8px apart: the place's own change action as a small primary ("Change it in a draft"), the copy button, then small quiet actions ("Announce it"). Under them the link itself (host and path, caption ink-2, tabular, breaking anywhere) and one note in ink-3 caption up to 40ch on what the link does on a phone. A place that can't be shown is an empty state in the column.
- **The phone:** a feature can register a live view of its place (Home draws through HomeScreen); any other place is the phone frame with the app's masthead carrying the place's name and where it is as its note.

### A person's page and roles
People opens each person on their own page (`/people/{id}`): a title band with a back link to People and only their name, plus the account state pill when the account needs staff (turned off, not invited yet, invitation expired), then the tab strip (Profile first, then Roles, What they can do, and Activity for people who manage accounts), 24px above the tab (owner, 2026-11-13, #81).
- **Profile:** two settings groups (`Section Grouped`). *Their details*: name, email, then each fact a feature adds with a `PersonFact` (Platform.Client: a key, a label, an order, the content and the permission it needs). *Their account*: its state, passkey, joined, the invitation (who and when), and the recovery link, with the account's actions (turning it off, deleting it, an invitation's) in a small ⋯ menu in the card's head, beside the confirmations they need. Both are fact lists of label and value rows.
- **Roles:** two columns, the ticks (capped with the panel at 1280px) beside a 300 to 400px sand explanation panel at the 14px radius (20 by 22px padding), sticky 24px from the top, 40px apart; from 1328px down the ticks are one column and the panel narrows to 260 to 340px (28px apart) so it stays beside the role being explained, and only under 680px does it drop below them. The ticks flow in two CSS columns (at least 280px, 32px apart), one block per feature that never breaks: an uppercase pill-size head in ink-3 at 600 (+0.06em), then role ticks on hairline rows. Under them, a hairline and *For one thing*: hairline rows with the role and place at 600 and a deep-aqua "Change it there" link.
- **Role tick:** a 40px row: an 18px checkbox in deep aqua, the role name at row size and 600, and a 32px round (i) button in deep aqua that fills sand on hover. The chosen row takes deep aqua at 9% on the surface and its (i) gains a 2px deep-aqua ring, so having the role (the tick) stays the stronger mark. A tick the viewer can't give is disabled, its name in ink-3, with the reason as its title and in the explanation.
- **Explanation:** the role's name in title type at 700, then caption-size groups: a 600 ink-2 lead ("Announcements Writer lets Ruth:"), a bulleted list, and for a kit "Also, from Media, because the job needs it:"; a hairline in sand-strong above who gave it and when.
- **Family and Safeguarding tabs:** laid out as the Roles tab is: the rows or fields beside a sand panel at the 14px radius (20 by 22px padding, caption size in ink-2, a title-size ink heading, a bulleted list at 1.7), sticky 24px from the top, 300 to 380px wide at a 40px gap, capped together at 1280px; the panel narrows to 260 to 340px (28px gap) at 1328px window width and drops under them, no longer sticky, at 928px. Groups are 28px apart, each under an uppercase pill-size head at 600 (+0.06em) in ink-3 ("Waiting", "Children Chris looks after · 2", "Chris's parents and guardians").
- **Birth date:** a fact under *Their details*, labelled "Birth date" ("3 Dec 2001 · 24 years old", ", under 18" for a young person, "Says they're 18 or older", or "Not given"), for anyone who may see families; for anyone who may change them, a deep-aqua *Change* (or *Add it*) link opens a sheet titled "Chris's birth date", whose lead says what the date decides, with the date field and a save bar (Cancel, Save, and Clear it as its other action) (#37, 2026-11-12: it isn't a family fact). It is the first `PersonFact`.
- **Family:** how ministries reach a young person (Segmented, with a hint); Waiting asks on the warm tile with small Take it back and Link them; children and parents led by their Avatar with small quiet Open and Unlink; "No parent linked yet" on the warm tile while a young person has none; and an accent add row ("Link a child", "Link a parent") that opens the person finder in place (and, for a child, an inline name and birth date to add one who isn't in the app), closed with a small quiet Done. The panel says what linking does.
- **Safeguarding:** any flag first, as a notice at the 14px radius (13 by 16px, row size at 600, an alert icon): due soon in marker at 22% with marker-ink text, run out in danger at 12% with danger text. Then *Blue card* and *Safe Ministry training*, each a pill-size head over its fields two to a row (12 by 28px gaps; one column at 928px), and a primary Save beside quiet "Remove the blue card" and "Remove the training"; a hint says who recorded it and when. The panel says the record is entered by hand and never blocks anyone, and shows the blue card as the person sees it.

### People on a thing
The shared way to put people on one thing (a calendar, a regular event, a draft). In the thing's panel a **People row**: a hairline above, a 600 label in ink-2 over the names on one line (cut off with an ellipsis) and a caption count in ink-2 ("2 people"), and a small quiet *People* button with a people icon. It opens the wide **People sheet**: the lead says what each role there does; the person finder ("Add someone: find them by name"), whose results are hairline rows with a small primary "Add as Editor"; then each person as a hairline block: name at 600 over a caption "Added by …, 9 Nov", a small quiet *Take off* at the end, and their roles for this thing as role ticks in a wrapping row. The (i) opens the explanation inline in a sand box at the field radius under that person. Done closes it.

### Safeguarding page
Who to follow up (`/safeguarding`, People area): a title band whose lead says nothing here stops anyone serving, then a data table. Its toolbar holds filter chips with counts (Everything, Run out, Due in 60 days, Not recorded, Young people without a parent). Columns: the person at 600, what it is, a status pill (Danger for run out, Marker for due soon and without a parent, Quiet for not recorded), and when in tabular ink-2, soonest first. Choosing a row opens the person's Safeguarding tab, or their Family tab for a young person without a parent. With nothing flagged, a shield-check empty state ("Nothing to follow up"). Run-out items and young people without a parent are the Needs-you items: they become rows in the office week's Needs you ("Record the new one", "Link a parent"), and the sidebar's orange count on Safeguarding counts those alone, not every flagged row; due soon and not recorded wait on the page.

### Person finder
The shared way to find someone and do one thing to them (`PersonFinder`, Platform.Ui): the People sheet's "Add someone", and linking a child or a parent on a person's Family tab. The shared search field (its label and placeholder say who: "Find their parent or guardian by name"), searching from two letters, then the matches as hairline rows at least 52px tall (6px above and below): the name at row size and 600 over a caption detail in ink-2 cut with an ellipsis, and a small primary button that names the act ("Link as their child", "Add as Editor"). People already there are left out. With no match, one caption line in ink-2: "No one matches “…”."

### Your family (Account)
The phone's Account page (and the admin panel's Account page) keeps a family as rows you act on, under a *Your family* section whose lead says what it is for ("Children you look after in the app.", or for a young person "Your parents see your details in the app while you're under 18."). One hairline list, in this order:
- **Asks of you** first, as rows on the warm tile with a people icon (who asks, and the child's age and when), with Below actions: a quiet "Not mine" (or "Not my child") and a small primary "Yes, link us".
- **Children** you look after, each led by their `Avatar` (a 40px fill-on-surface circle, initials in primary at 700) instead of a tile: the name, a subtitle with their age and how they're reached, and a chevron that opens the child sheet.
- **Parents**, led by their Avatar, "Your parent · linked …", with a small quiet Unlink for an adult.
- **Your own asks** on the quiet tile with a clock ("Waiting for …"), with a small quiet *Take back*.
- **The add row** on the accent tile: "Add a child" for an adult, "Ask a parent to link to you" for a young person; each opens a sheet with one or two fields, a block primary that names the result ("Add Leo") and a quiet Not now.

*Your family* is one settings group (a card on the admin panel, an inset group on the phone), so the add row sits inside it with the children it adds (#60, 2026-11-12). Under the list, for a young person with a parent, a hint saying how they're reached. Your birth date is a fact about you, so it lives in the **You** group, after your email row (and the email code while it isn't confirmed); a feature adds a part there with an `AccountYouPart` (Platform.Client), as it adds a whole section after You with an `AccountSection`. Until you've answered, the age question as plain fields (a birth date field, "Like 4 Mar 2011", whose hint says why the church asks and who sees it, then a quiet "I'm 18 or older" beside *Save birth date*); once answered, a quiet-tile row with a calendar icon ("Born 4 March 2011") and who sees it; after "I'm 18 or older" nothing shows, and a demo account has none of it. Then *Blue card and training*, for anyone with either recorded: the blue card, and Safe Ministry training as a row (accent tile with a shield check when current; warm tile with an alert and a pill when not: Marker "Due in 12 days", Danger "Overdue").

### Child sheet
Tapping a child opens a sheet titled with their name. In order:
- **Sign-in code:** a sand card ("Sign in on their own phone", or "A new sign-in for their phone"). Before a code: one line on when to make one and a small primary "Make a sign-in code". After: the code at headline size and 700, tabular, tracked 0.08em in ink (spoken digit by digit), with the button-only copy, using the copy icon, at the other end of its row (wrapping when tight), and a caption hint beneath on how long it lasts.
- **How to reach them:** a section with a Segmented choice (Directly, Through you, Both) once the child can sign in, its lead saying what each means.
- **Details:** name (for a child the family added who can't sign in yet) and birth date, with a small quiet *Save details*.
- **Another parent:** waiting asks as quiet clock rows with *Take back*, then an email field and a small quiet *Ask them*.
- **Remove:** a small quiet "Remove …" or "Stop looking after …" that confirms inline: a warning notice saying what happens, then a small quiet Keep beside the small Danger action.
- Done (primary, block) closes it.

### Blue card (signature)
A Queensland blue card drawn as the card in your pocket (`BlueCard`): masthead navy with on-masthead text at the 14px radius, padded 16px, 18px at the sides and 14px at the foot, the ring at its top right (see Shapes). Its head row carries "QLD blue card" in uppercase pill type at 600 (+0.06em) in the masthead note colour, and the status capsule at the other end (pill type at 700): **current** in the live colour with on-live text ("Current"), **due soon** in the marker with on-marker text ("Expires in 12 days"), **run out** white with danger text ("Expired", or the status the office recorded, such as "Suspended"). Then the number at title-large size and 700, tracked 0.04em, tabular, 8px under the head and 12px over a facts row: caption-size pairs (Expires, Checked), each label in on-masthead at 76% beside its value at 600 in tabular figures, wrapping 8 by 24px apart. It shows on the person's own Account page and, as "How Chris sees it", in the Safeguarding tab's sand panel.

### Breadcrumbs
Where you are inside something nested, as a row of crumbs (a navigation landmark named "Where you are"). Crumbs sit 2px apart with a small chevron in ink-3 between them; each is 34px tall, 8px at the sides, at the 9px radius, row size at 600, with an optional small leading icon (an image for the Library, a folder, a lock for what the app owns). Earlier crumbs are deep-aqua buttons that fill with sand on hover and take the 2px deep-aqua outline on focus; the last is the current place in ink. A crumb can take a drop: only the crumb under the drag lights, with a 12% deep-aqua tint and a 2px inset deep-aqua ring. Placed at the start of a row, the crumbs pull 8px left so their text lines up with the content below.

### Selection tray
A navy bar that docks at the foot of the work area while several things are chosen, carrying what can be done to all of them. It is sticky 20px from the bottom of the window, at least 58px tall, padded 8px 10px 8px 22px at the 14px radius, in primary with on-primary text at 600 and the Lift shadow, 24px below the list. It reads the count with tabular figures ("3 files chosen", spoken politely), then Clear (quiet, on-primary at 14%) and the actions; a primary action inside it turns white with ink text. It rises 10px from 40% over 220ms (not with reduced motion). Media shows it only for two or more files: one chosen file is the split view's detail.

### Media library
Staff's library as two shelves: the Library, which staff organise in folders, and Made by the app, which the app fills. Reached from the sidebar's The app area.
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
- **Sign-in card:** a white card at the 14px radius, lifted, up to 460px wide (28px padding, 20px at the foot), centred on a full-window sand stage, with the mark and name in primary at 700 at its head. A small quiet "App reviewer? Sign in with a code" switches it to the code form.
- **Open in the app:** a shared link opened without the app is the sign-in card on the sand stage, filling the window: the brand head, then the place as its row tile beside its name in title-large type (balanced) and where it is in caption ink-2, then two short ink-2 paragraphs (up to 52ch) on what the link does and how to get the app. A place for signed-in people is named only "A place in the church app"; a place that's gone says "This isn't in the app any more". Signed-in staff also get a quiet "See it in Places in the app".
- **Gate:** when a page needs signing in or a permission the person lacks, the title band stays and an empty state sits in a sand box at the 14px radius beneath it. A page that doesn't exist and a page that fails show the empty state alone, with a quiet button home or to try again.

### Previews
- **Preview banner:** an orange marker strip docked just above the tab bar on every screen while a preview is on: an eye icon, the bold name of what is shown with an optional caption line, an underlined Hide, and a dark capsule Exit in on-marker colour. The page reserves room for it at its foot.
- **Phone frame:** a phone at its true point size (each supported iPhone's width, height, safe areas and cutout), with a status bar (9:41, signal and battery), the Dynamic Island or notch, and the tab bar; the frame lifts and is scaled to fit the space it is given: the width of its holder and the window height below it, or the holder's own height when the holder sets one (as the Places phone column does). It can carry its own palette, so a preview shows another palette without changing the page around it. Rows and tabs inside it do not respond to taps.
- **Lock-screen proof:** what a notification will look like on a locked phone. A masthead-coloured panel at the 14px radius with the date in caption at 600 (on-masthead at 76%) over a 9:41 clock, then a page-coloured notification at the sheet's 22px radius: a row with a 20px masthead app icon, the app name in uppercase pill type, "Time sensitive" in marker ink when it is urgent, and "now" in ink-3, then the title at row size and 600 and the message clamped to four lines. Empty fields show placeholder words in ink-3. An optional caption sits under the panel.
- **Home screen and surface views:** the Home masthead and its Hub tiles are drawn by one component, and each surface registers one view; the app's Home and the admin preview both draw through them, so a preview is the real screen, not a copy.

### Ministry page (bulletin boxes)
A ministry (`/ministries/{id}/{area}`) reads like the Sunday bulletin's shaded boxes: in each area, what you may see is boxed by who it's for.
- **Masthead and its one action:** the ministry's name as the title and its one line (what it is, or when it meets) as the note, then, 14px down, one action in a wrapping row (8px gaps): your place as a pill on the masthead chip (6 by 12px padding) with a check ("You lead it"); otherwise *Join*, or *Ask to join* when every open choice needs a leader's yes, as the on-masthead button; *Sign in to join* when signed out; or the same chip pill with a clock, "Asked · waiting for a leader". A hidden ministry (or one under a hidden one) adds the same chip pill with the eye-off, "Hidden" (2026-11-13). Joining confirms with a notice above the area.
- **Area tabs:** the tab strip in its link mode, sticky under the top bar, shown once there is more than one area: About first, then its parts (News, Calendar, Files, Roster, Chats, each only while the ministry uses it) in the order set in *Its tabs*, then People (with the count of asks waiting) and Settings for those who may use them. A Life Group's Sundays live in its Calendar tab (the old Sundays address opens it). Each area has its own address, and a tab replaces it in place. The sticky strip scrolls sideways only (never up or down, no scrollbar; its hairline is an inset line and the underline sits inside it), and scrolls itself to keep the chosen tab in view.
- **Calendar area:** with calendars, *Coming up* first: the next dozen events of its calendars (and, when it shows them, the ministries under it) as day sections of agenda rows, each row's label line naming its ministry when more than one is in the list, regular rows included. Then its calendars in their boxes, and *From the ministries under it* as quiet calendar rows ("Impact Kids · For everyone").
- **Audience boxes:** `MinistryBox`, at the 14px radius, 12px 14px 4px in, 10px apart, always in the order team, families, parents, everyone; each tinted on the page colour with a pill-size uppercase head at 700 (+0.06em) saying who it's for. **For the team:** primary at 7%, head in primary. **For families:** accent at 8%, head in accent. **For parents:** marker at 18%, head in marker-ink. **For everyone:** sand, head in ink-2.
- **Items in a box:** hairline-divided, 10px above and below: the title at row size and 600 (1.3), the body in caption ink-2 up to 65ch keeping its line breaks, then a caption meta line in ink-2 wrapping 4 by 8px: "Important" in marker-ink at 600, "From …" for a post from the ministry above (or, when it shows them, one under it), how long ago ("· edited"), and where it stands with church news in accent at 600: "In church news", "Asked for church news" or "Kept in this ministry's news". On the phone each post is a link to its own page (see A post's page), its body clamped to three lines; in the admin panel a leader's own post is a button that opens the post sheet. Files and calendars sit in the boxes as rows on accent tiles (a file or link icon; a calendar), with a small quiet pencil for those who may change them.
- **Tools:** above the boxes, a primary with a plus ("New post", "Add a link", "Add a calendar") for those who may add, 8px apart. An empty area is an empty state that says what goes there.
- **About:** the about text up to 65ch, then quiet-tile rows for when, where and who it's for, the leaders led by their Avatar, *Waiting for a leader* (quiet clock rows with a small quiet *Take back*), its teams (or groups) as rows with a chevron, and *Your place*: quiet door rows that act at once ("Leave the team", "Stop coming along", "Mia stops coming").
- **Join sheet:** "Join …", led by "Who's it for?" when there is more than one choice. Each choice is a card: sand at the 14px radius, 14 by 16px in, 10px apart, a 20px radio (a 2px ink-3 ring) beside the title at 600 over a caption detail in ink-2. The chosen card turns accent at 8% on the page with an inset 2px accent ring, its radio filling to a 6px accent ring; a choice that isn't open is in ink-3 with its reason as the detail. The first open choice starts chosen; an ask adds an optional note for the leaders, and the block primary names the result.
- **Post sheet:** in the admin panel a ministry's *New post*, changing its post and *Tell the group about a Sunday* open a writing page, `/ministries/{id}/news/write` or `/ministries/{id}/news/{post}/change` (`PostSheet OnAPage`, in `WritingDesk`; owner, 2026-11-13, #83): Back to the ministry, a lead "In …. A title and the post. Pictures, video, files and more are under Insert.", the same settings as grouped cards in the column (*Who it's for*, *Telling people*) and the post in a phone frame under *Preview on a phone*. The phone keeps the sheet: "New post" (or "Change the post"), led by "In …", holding the post editor (see Writing a post) with its settings inline under a hairline, "Who it's for and telling them": *Who it's for* as a stacked segmented control, then a sand note at the field radius (10 by 12px, caption ink-2) saying exactly who reads it; *Important* (sends a notification to everyone it's for, straight away) and, for a post to everyone, *Ask for church news*. Its actions are the save bar: Cancel bottom left, and the primary naming the audience ("Post for parents"), or "Save" for a saved post, whose Delete is in the sheet's ⋯ menu. In a hidden ministry the everyone choice reads "Everyone who sees it" (in the file and calendar sheets too), the primary "Post for everyone who sees it", the note says only its own people, the leaders above it and staff see it, and *Ask for church news* isn't offered.
- **A Life Group's Sundays:** no list of their own (2026-11-12). Its Calendar tab's Coming up shows each Sunday; *Tell the group about a Sunday* (an accent megaphone row above it) opens the post sheet with *Which Sunday* and *No class this Sunday*, and the post shows under that Sunday as a note on the date (see Calendar), struck through when there's no class.

### Leading a ministry
Leaders do it on the phone, staff in the admin panel, through the same areas.
- **People tab:** with attendees, a segmented control *Team | Attendees*, each with its count. Asks come first: a row-size 700 head ("Asking to join (2)", "Asking to come along") over warm-tile rows, the person's note in quotes beneath, then a small quiet *Not now* and a small primary *Yes, add them*. Then the search field ("Find someone"), 18px down.
- **On the phone:** people as rows led by their Avatar, the line saying their role (when not plain team or attendee), age and parents' first names, or "No parent linked" in marker-ink at 600; the list ends in an accent add row ("Add to the team", "Add an attendee"). A row opens the **person sheet**: their name, a lead of age, role and "no sign-in of their own"; a parents row (warm tile when there are none) that opens the person finder ("Link as their parent"); then each change as a row that acts at once, accent with a plus to give ("Make a leader", "Add to the team as well"), quiet with a minus to take ("Take off the team", "Mia stops coming"); and a hint that only the leaders and the church office see this. Someone who leads from the ministry above gets a line saying where to change that instead.
- **In the admin panel:** the same list as a data table (Name at 700, Role, Age in tabular figures, Parents, or a Marker "No parent linked" pill) in a split view; the detail column holds the name in title type (-0.015em), the lead as a hint and the same rows. The add action is a quiet button beside the search.
- **Settings:** settings groups (`Section Grouped`), the same on the phone and in the admin panel, in this order. *The details* ("What people see about it, how they join, and where it shows."): Name, In a line, About, When and where it meets, Who it's for with the stack gap between fields; *Joining* (`JoiningFields`, shared with *A new ministry*) as two labelled stacked segmented controls 22px apart: *Joining the team* (Open, Ask, Invite only) and *Coming along* (adds No attendees), each with a hint saying what it controls and a detail on every option saying what happens to the person ("People ask to join the team, and a leader says yes"); then *Life Groups* (`LifeGroupsFields`): a *Show in Life Groups* toggle row and, while it's on, *When it shows* as a stacked segmented control (All year, then each set's dates that hasn't finished); then *Hidden from everyone else* (`HiddenFields`, 2026-11-13, #73), a toggle row ("Only its own people and staff see it") saved with the rest. A switch that can't change stays, saying why: under a hidden ministry it is locked on ("Hidden, because Music is. Only its own people and staff see it."); for a Life Group it is muted and off ("A Life Group is listed for everyone, so it can't be hidden"), and while hidden *Show in Life Groups* is muted the same way ("A hidden ministry isn't listed in Life Groups, which everyone sees"). While hidden, a sand `ministry-who` note under it names exactly who still sees it and what that means, and *Joining* gains a hint that only people who can already see it can join or ask. The switch sits in *The details* as the station's default; the placement awaits the owner's pick on the decision page (`.impeccable/mocks/hidden-ministries/`: in the details, its own group, or the ⋯ menu). Its foot is the `SaveBar` (Cancel, *Save the details*, "Give it a name to save it." while it has none). *Its tabs*, *Chats* and *From the ministries under it* change straight away and say so in their foot ("These change straight away.").
- **Its tabs:** the `ReorderList` of the ministry's tabs, its lead naming the menu in words ("or use its More menu"), with the preview strip ("About is always first"), each a toggle row that turns the tab on or off; an off tab is muted, saying it's hidden from everyone, leaders too, and nothing in it is deleted. The order is the order on the ministry's tab strip. **Chats** (while chats are on): *Who can start a chat* as a segmented control, Leaders or The team too. **From the ministries under it** (when a part that is on can gather): a toggle row per part, *Show news from the ministries under it*, *Show the calendars of the ministries under it*. The parts come from one list (`MinistryParts.All`), so a new part is one entry there.
- **Where it sits:** its lead says where it sits now ("Under Music. Its people, posts, files and calendars move with it."), then *Sits under*, the same search picker as *A new ministry* (`SitsUnderPicker`), leaving out itself and everything under it. Choosing another place shows an info notice in sentences saying what changes (where it goes and with how many ministries under it, which leaders will lead it and which won't any more, whose combined news or calendar it joins or leaves, whether it becomes hidden ("It will be hidden, as Music is: only its own people and staff will see it.") or stops being hidden ("It won't be hidden any more: everyone will see it."), and that its own people, posts, files and calendars stay as they are), over a primary *Move …* beside a quiet *Keep it where it is*. A chosen place not yet moved is behind the unsaved-changes guard.
- **Ministries under it** (a ministry under another is a ministry, never a team): rows with a chevron, then an accent *Add a ministry*.
- **Archive and Delete** live in the ministry's ⋯ menu, never on the Settings page: in the admin panel among the `PaneHead` tools, on the phone in the top bar (ghost on the masthead). *Archive …* ("Puts it away without deleting anything. Staff can restore it.") asks in a sheet saying what leaves every list and what is kept, *Archive it* over a quiet *Keep it*; archiving returns to the ministry above. *Delete …* comes last and red after a rule ("Shows what goes with it first"): its sheet lists what goes and where the ministries under it move, a danger *Delete it* over *Keep it*. Every Ministries and Life Groups form is behind the unsaved-changes guard.
- **The directory** (the app's *All ministries*): the tree, each ministry's rows indented 24px per level beneath it (the row and its hairline both move in, `ListRow`'s `Indent`), three levels deep; a ministry shown in Life Groups has "Life Group" first in its line, and a hidden one has "Hidden" first, before that, and no join line (2026-11-13).
- **A new ministry** (one sheet everywhere: the desk's *New ministry*, Settings' *Add a ministry*, the app's directory): *Sits under* first, the shared search picker with each option's path ("Kids Ministries › Impact Kids"), preselecting the ministry it was opened from, offering "Nothing: it's a top-level ministry" only to people who may make one, and only ministries the person may add under; then Name, In a line, When and where it meets, *Show in Life Groups* with *When it shows* (`LifeGroupsFields`; opened from Life Groups it starts on, with *Coming along* Open, and the sheet and its button say "Life Group") and the joining choices; *Add the ministry* in the SaveBar, saying "Give it a name to add it." until it has one.

### Roster (a ministry's tab)
What a volunteer opens it for comes first (`RosterArea`, Rosters.Client, hosted through `MinistryPartView`).
- **You're on:** a hairline row per date you (or your child) are on: a 34px day tile in marker at 30% with marker-ink figures, the title "Cameras · Sun 15 Nov · 6pm", the event (or "For Tom · …") as its line, an answer pill at the end once answered (Accent "Can do it"), and below it small buttons: primary *I can do it* (*Yes, for Tom*), quiet *Can't do it* (opens an optional reason inline), quiet *Ask for a swap* or *Call off the swap*. "Answered by Sarah, for Tom" and the swap's state are caption lines in ink-2.
- **Can you take one of these?** and, for leaders, **Waiting for a leader**: warm-tile rows with small buttons (*Take it*, *Tom takes it*; *Not now*, *Yes*).
- **The roster:** leaders' tools first (primary *Add a date*, quiet *Positions* and *How it works*), then a *By date | Grid* segmented control (up to 22rem). By date: a row per date led by a sand day tile, the positions on one line ("Sound: Jo · Lights: nobody yet"), a Marker "2 gaps" pill for leaders, a chevron to the **date sheet** (each position as a 700 row-size head with "1 of 2" in tabular caption, its people as rows led by their Avatar with their answer pill and flags as the line, taking someone off in each row's ⋯, and an accent *Put someone on* whose line says what's open ("1 place open · someone trained for Ben"); a trainee is its own row indented under the position, led by a "T" day tile, aqua when someone trained is on (line "Training, with Jo"), warm with the marker-ink flag "Training: needs someone trained on with them" when alone, and their place still counts as open (the add row turns warm too); taking the date off the roster is in the heading's ⋯; picking lists the team, unflagged first, flags as the line on a warm tile, *Put on* quiet when flagged).
- **Grid:** dates across (four on the phone, eight at a desk, with quiet chevrons and the range in caption 600), positions down in a sticky first column with what they need in ink-3 ("1 person · needs a blue card · trained, or training with someone who is"); cells are chips at the field radius: aqua at 10% answered, sand waiting, primary at 7% when answers are off, danger at 10% struck through and ink-3 for "can't"; flags in marker-ink pill-size 600 under the name; a trainee with someone trained carries a small aqua "Training" there instead, and trainees are listed after the trained person; a gap is a dashed hairline cell with an accent *Put on*. The table scrolls sideways with no scrollbar.
- **Days away** on the phone are their own page (#68, 2026-11-12): the Roster tab ends with a *Days away* group of hairline rows, for anyone on the roster or leading it: *Your days away* (warm tile, `AppIcons.DaysAway`, "Next away Sat 21 Nov" or "None coming up") and for leaders *The team's days away* (quiet tile with a people icon, "2 people away in the next four weeks" or "Nobody away in the next four weeks"), each with a chevron to `/account/away` (under More, as Account is) or `/ministries/{id}/away`; on the phone, Account's *Rosters* group, after Your family and the blue card, holds the same *Your days away* row. Each page is a masthead ("Days away" with a note on who sees them; "The team's days away" with a note naming the ministry), then `DaysAway Paged`: whose first, then a `Month | List` Segmented (up to 22rem); Month is the month with its key and the accent *Add days away* row; List is a hint when there are none ("No days away coming up for you.", or for Tom), then the repeating rules, then one-off days soonest first, then the add row. Someone who doesn't lead the team sees an empty state on the team's page ("Only the team's leaders see this"), and before the roster reaches the phone, "The roster isn't on your phone yet". The admin panel keeps the editor inline.
- **Days away editor** (`DaysAway`, on the phone's Days away pages, the admin panel's Days away tab of a person, and the admin roster): whose first when it covers more than one person (a Segmented control, "Me | Tom", for up to three; a Whose select beyond that), then repeating rules as hairline rows led by a warm repeat tile ("Sunday mornings" or "Every Sunday", its line "From 15 Nov, until further notice, not 20 Dec" and the note) with a chevron, above the `MonthDays` month (up to 30rem), its caption key ("Shaded: away. Top half: mornings; bottom half: evenings. Struck through: not away that time."), then one-off days as rows led by a quiet tile, and an accent *Add days away* row. Choosing a day opens what's there: a one-off day opens its editor, a free day opens a new one prefilled with that day, and a day from a repeating rule opens a small sheet titled with the date ("Away, from “Sunday mornings”.", or "Not away this time, from …" on a skipped day) with *I'm not away this day* (*Alex isn't away this day* for someone else, *Away this day after all* on a skipped day) over a quiet *Change the rule*. Leaders see **The team's days away**, for everyone on the team.
- **Days away editor** (`AwaySheet`): titled "Days away", or the rule itself when changing one; its ⋯ (Delete these days away…) sits in the heading. Who's away (when several), How often (*Once | Every week*); every week shows the weekday chips in one row of seven, Starting, and Until ("Until further notice" when empty); once shows First day and Last day. Away for (*All day | Mornings | Evenings | Times*, Times opening From and Until side by side), *Not away on* as quiet chips of the dates skipped (choose one to be away after all), and a note for the leaders. Cancel bottom left, Save (*Add the days* when new) bottom right.
- **How the roster works** (leaders, `RosterSettingsSheet`): grouped sections in order: *Answers* (ask people to answer, the nudge and the reminder); *When someone asks the team for a swap* as Segmented Rows (Anyone on the team can take it, Anyone can offer and a leader says yes, Nobody takes it, each with its detail line); *Asking one person* (a toggle row); then a sand caption sentence in ink-2 at the field radius that follows the choices ("So when Alex can't do Sun 15 Nov, they ask the team, and stay on until it's taken. Leaders hear."); *What the team sees* as Segmented Rows; *Calendars it rosters from* as toggle rows. Cancel bottom left, Save bottom right.
- **Flags never block:** they are words, in marker-ink, beside a name; never a disabled button.
- **Hub tile** *Your roster*: "You're on Sunday" (or "Tom is on Sunday"), the position and event, and "Say whether you can do it" while an answer waits (unread).

### Ministries desk (admin)
`/ministries`: a title band ("12 ministries · 3 asks waiting for an answer") with *New ministry* as its primary. Below it, 18px down, the tree (240 to 300px) beside the chosen ministry, 28px apart; under 900px they stack and the tree stops being sticky.
- **Tree:** sticky 24px from the top: the search field ("Find a ministry"; searching lists matching names flat), then a button per ministry, 2px apart, at least 40px tall, 6 by 10px in, at the 9px radius: a small icon and the name at label size and 600 in ink (cut off with an ellipsis), indented 20px per level. A hidden ministry has the small eye-off in ink-3 after its name (`ministry-tree__hidden`, titled and spoken "Hidden from everyone else"; 2026-11-13). Asks waiting show at the end in the tab strip's orange count capsule, spoken "3 asking to join". Hover fills sand at 60%; the chosen one takes Selected Fill and is the current page.
- **The ministry:** a `PaneHead`: its name, a lead of where it sits ("Under Kids › Impact Kids") and its one line, led by "Hidden from everyone else" for a hidden ministry, and its ⋯ menu (Archive, Delete) among the tools; then the tab strip in link mode (not sticky) and the area at desk width. With nothing chosen, an empty state: "Choose a ministry".

### Life Groups
Every ministry with *Show in Life Groups* on, wherever it sits, in dated sets or all year; staff plan the sets in the admin panel. People never see the word for a set: only its dates.
- **In the directory:** a *Life Groups* section with one accent row, "Sunday classes", saying "Yours: …" or "Choose a class for you or your children".
- **The page:** a card for the next set once it shows, titled "New groups from 1 February", one line ("6 groups on 6 Sundays, 9am to 9:45am. Choose one for you, and for each of your children.") and a primary *Choose a group*. *Your groups* comes first when you or your children are in any. Then *Now on* (the dates and time as its lead) and *All year*, each group a row on the accent tile: where it meets, your place or its line, and "until …" for a dated set. With nothing on, a book empty state.
- **Choose a group:** a paged sheet, one page per group: a cover heading (book icon, the name, where it meets), its line, quiet rows for its Sundays and time, where it meets and *More about it* (chevron to its page), and a block primary *Join …* that opens the join sheet (signed out, it goes to Account). Close ends it.
- **The desk (admin, `/life-groups`):** a title band with a quiet *Plan the next groups* (planners only) and a primary *Add a Life Group*, then a quiet calendar row saying where Life Groups show ("Shown in Sunday services", or "Not shown in a calendar"), opening the calendar sheet for planners. Under it, 18px down, a sand strip at the field radius (10 by 14px in, 8px gaps): "All year" at row size and 700, the all-year groups as link chips (32px tall, white, full radius, caption at 600), and an accent *Add an all-year group*. A group not on a set's Sundays turns its chip to the field radius, marker at 14% with a 1px marker ring, and adds "not on …" in marker-ink. *Archived groups (N)* is a text link at the strip's right; it opens the archived sheet.
- **Board by stage:** sets of Sundays move left to right through four equal columns, 18px apart: *Being planned*, *Shown in the app*, *Now on* and *Finished in 2026*; two columns under 1180px, one under 640px. Each column opens on a sentence-case head at label size and 700 in ink-2 with its count in tabular ink-3 at the other end, 12px above its cards. A set is a card on the 14px radius, 14 by 16px in, with a hairline ring: its short dates at row size and 700 (-0.01em), for planners the button that opens *Sundays and time*, with a small pencil in ink-3 (underlined on hover); a caption fact line in ink-2 ("6 Sundays · 9am to 9:45am · shows in the app from …", or "Sunday 3 of 6" once on); its groups as hairline caption rows at 600 linking to each group, a group without a place adding "· needs where it meets" in marker-ink, and an all-year group sitting it out as an ink-2 row with a circle-off icon. What a set still needs is a caption in marker-ink at 600 ("No groups yet, and people can already see these Sundays."). A card carries one action, a small quiet *Add a group*. Only the next set to fill takes the 2px marker outline 2px out; the set now on takes an inset 1.5px accent ring. An empty column is a caption note in ink-2 inside a dashed outline (ink-3 at 45%) on the 14px radius, saying what comes there and when.
- **Finished:** the shared segmented control chooses the year once there is more than one. The column is one card of 40px fold lines, hairline-divided: a chevron, the dates in ink at 700 and the group count in tabular caption ink-2 at the end. The latest three show, then an "N more" line. Opening a set lists its groups, with a small quiet *Archive their groups* (or *Delete these Sundays* while it has none), each asking first in a sheet.
- **Archived sheet** (`ArchivedSheet`): opened from *Archived groups* here and from a quiet *Archived* in the Ministries desk's title band. "Archived Life Groups" (or "Archived ministries"), its lead saying restoring brings one back as it was; quiet rows with the archive icon, the name over where it sits and how long ago it was archived, and a small quiet *Restore* trailing. Empty, an archive empty state ("Nothing archived").
- **Planning sheet** (wide, *Plan the next groups* or *Sundays and time*): the chosen Sundays as on filter chips that take a Sunday off, a From and To pair with a small quiet *Add the Sundays in these dates*, Starts and Ends, and *Shows in the app from*. Its actions add *An all-year group isn't on these Sundays* and, while a set has no groups and isn't on, *Delete these Sundays*.

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
| News (church news from the store; the posts bring their own pictures, so the tile's Picture field isn't used) | Front page | any | the newest post's headline and when ("2 hours ago"; "From Kids ministry · 2 hours ago" for a ministry's post at 2 × 2) with its picture: behind at 1 × 1, beside at one row, above when taller; 2 × 2 adds the post before it on one line under a hairline, opening that post; a post without a picture keeps the tile's icon and words | always |
| | What's new | any | posts new since this phone last opened News, with an unread dot: 1 × 1 "2 new" over the title; 2 × 1 "2 new in News" over the newest headline; 1 × 2 adds when; 2 × 2 the newest headline, "2 hours ago · 1 more new" and "Read it"; with none new, "Nothing new since you last looked" | never |
| | Headlines | any | 1 × 1: the newest headline alone, when under it; bigger: the title with "All news" beside it over agenda rows, three at one row or 1 × 2, five at 2 × 2 (one more per extra row, two at two columns), each headline up to two lines (one at 240px wide and over) | never |
| | (urgent today) | | takes the tile in every layout, as at the top of the News page: a Front page of 2 × 2 or bigger keeps its lead and gains a marker band "Today: …" across its top (`HubTile`'s `Banner`); anything smaller, and What's new, becomes the post itself in Highlight with the News icon and "Today · " and its first words; Headlines lists it first, "Today" in the when column and a Today pill before the headline | |
| | (no news) | | the tile's title and "Nothing new yet", any layout | |

In the editor a layout that doesn't suit the tile stays listed but greyed, and its caption says why instead of
what it draws: "Needs a wider tile", "Needs a taller tile", "Needs a bigger tile", or "Needs a picture: choose
one under Picture". The automatic choice reads "Best for this size (now Picture beside)", naming what it picks.

Recorded on the decision pages but not built: This week as a day strip, the sermon series card, Outreach's map
card (needs real routes), and News as no tile at all (declined; `.impeccable/mocks/news-tile/`). The pages are in `.impeccable/mocks/decision/hub-variants/<kind>/`.

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
- **Agenda inside a tile:** hairline-ruled entries, a 5.4rem tabular time column at row size and 700 in primary (the text colour on solid tones), the day under it in caption. A date that isn't on reads "Not on" in the time column, its title at 400 in the tile's secondary ink. On the phone each entry is its own button above the tile's hit area, opening that date's page and pressing to 55%; the rest of the tile, its heading included, opens the calendar.
- **Ministries and Life Groups tiles:** both say something about you once they can. Ministries: when you're in any, "Your ministries" with up to two lines ("Impact Kids: newest post title", or the name and your place); otherwise the staff's title and words. Life Groups: in a group now, "Your Life Group" and "Name · Room, 9am"; with new groups coming, the staff's title over "New groups from 1 February"; otherwise the staff's words.
- **Unread dots inside a tile:** the newest titles at row size and 600, 8px apart, each one line with an ellipsis after its 9px dot.
- **Banner:** `HubTile`'s `Banner` slot is a strip across the top of the tile, above its picture, that opens its own target (a `HubListEntry` with `hub-tile__banner`). It is marker with on-marker, 8 by 12px padding, caption size at 700: a small icon, words on one line that end in an ellipsis, then a small chevron. A picture tile's corner mark moves down to 48px under it. Only News's urgent band uses it.
- **Date after the title:** `hub-list--after` is for a single "what came before" row under a tile's lead (News's Front page at 2 × 2): the title takes the line, ellipsized, and the date follows it, quiet, at caption size and 400 in the secondary ink. Lists of several (Headlines) keep the agenda's when-first column, at caption size, with ages: "Just now", "46 min ago", "3 hours ago", "Yesterday", the weekday within the week, then "9 Nov".

### Hub canvas (signature)
Home arranged on the desk, on App styling's phone inside a draft: the phone column is the canvas, and the panel beside it holds the Home screen controls or the chosen tile's settings.
- **Home screen panel:** the panel head (Home screen, then a caption saying how to move and resize with the mouse and the keyboard), then Add a tile as a primary button beside Columns (a caption label at 600 in ink-2 and a segmented control), wrapping 12 by 16px. Tiles not on the phone for the person previewed follow under a label-size 700 head in ink-2 ("Not on the phone for a visitor") as quiet rows saying why, with tiles taken out in the draft muted and offering *Put back*; the list holds at most 40% of the window height and scrolls on its own. The tile library closes the panel.
- **Stage:** the phone frame alone, scaled with a transform (never CSS zoom, whose boxes WebKit misreports) to fit the phone column, never below half size. Whom the draft's Home is drawn for, light or dark, the phone and the day are the preview chips under it. The phone column stays put while the panel scrolls with the page. Nothing on the stage can be text-selected.
- **On a tile:** hovering draws an inset 2px marker ring at 60%; the chosen tile is outlined by a 2px marker ring, and carries its size as a marker capsule in words ("2 wide, 1 tall", pill type at 700, tabular, on-marker) and a corner grip, an 18px marker square at the inner radius ringed 3px in the page colour, set 8px out from its bottom-right corner. Capsules sit across the tile's top edge, 10px in from the right; the size capsule shows only on the chosen tile, and on the others what changed in the draft shows as a marker capsule in the same place.
- **Moving a tile:** after 5px of travel the tile lifts (a copy in a drag layer over the phone's screen, above the masthead and the tab bar, scaled to 1.05, the drag lift) and follows the pointer 1:1 while its own cell goes invisible. Nothing marks the landing place but the gap itself, as on an iPhone's home screen: no slot box, no column guides. The grid repacks live in the page by the same packing as `HubGrid`: once the tile's top-left cell has rested 130ms over a new cell the order changes (always planned from the arrangement the drag started with, so the picture depends only on where the tile is held), and the neighbours glide to their new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)`. Only the tiles that must move do: a small tile over a big one makes it give way downwards and the row above fills from below; nothing ever jumps. Holding the tile near the phone's top or bottom scrolls the screen every frame, faster the nearer the edge, never past the end, and the grid stays still until the scroll stops. Arrow keys move the chosen tile one cell in that direction through the same plan. Letting go settles the tile into its gap over 220ms on a gentle spring (`linear()` with a 3% overshoot) while its shadow fades; Escape, or letting go off the phone, glides everything back and scrolls the phone back smoothly. Resizing from the grip keeps the drag lift without the scale and repacks the others live as the size changes. With the keyboard, arrows move the chosen tile and Shift with arrows resizes it, and the others glide the same way. Reduced motion drops the glide and the lift's scale. Edge scrolling arms only once the pointer has left the edge zone or moved half a row. However a drag ends (drop, Escape, a lost pointer, the tab hiding), the grid is left clean: no hidden or shifted tile, and the dropped picture is exactly the order saved.
- **Settings panel:** the tile's name in title type over a caption, then fieldsets with legends at label size and 700: size on the grid (36px round steppers with a tabular value), layout at this size, colour from the theme, icon, words, picture, opens, and who sees it and when, with the actions under a hairline, wrapping at their natural widths (a danger action on its own line). It fills App styling's panel under a *Home screen* back link and scrolls with the page while the phone stays put.
- **Colour swatches:** four to a row (10 by 8px gaps), each a 40px "Aa" chip at the field radius drawn in its own tone, in the draft's theme and the preview's light or dark (the chip carries the phone's palette and scheme, so it always matches the phone beside it), with a hairline inset (the Card chip lifted), over its staff name in caption ink-2, centred; the chosen chip has a 2px marker outline at a 2px offset and its name turns ink at 600. A caption under them says the words change colour by themselves to stay easy to read.
- **Fade over the picture:** under Picture, only while the tile's words sit over its picture, the same swatches pick the colour that fades over it; the words take that colour's paired text colour.
- **Top of Home on the canvas:** the masthead is one target; hover draws the inset 2px marker ring at 60%, chosen an inset 3px marker ring (the masthead clips outside rings), and its *Changed* capsule sits inside the top edge under the status bar.
- **Layout picker:** the kind's layouts stacked 6px apart, each a fill-on-surface box at the field radius (10 by 12px padding) with the name at row size over a caption saying what it draws; the chosen one turns the page colour with an inset 2px marker ring, and a layout that doesn't suit the tile has its name and caption in ink-3, the caption saying why.
- **Contrast:** there is no light and dark proof on show. The tile is drawn twice off screen, in light and in dark, and the contrast of its text measured; only when a scheme falls under 4.5 : 1 does a warning notice appear at the top of the panel, in plain words ("Words on this colour are hard to read in dark mode. Try another colour.").
- **Icon picker:** the chosen icon in a 38px fill-on-surface cell at the tile radius beside its name at row size and a small quiet "Change icon" (Done while open); Change opens the grid of 38px cells, auto-filling, 6px apart, at the tile radius on fill-on-surface in ink-2, the chosen icon primary with on-primary, with the icon upload under it. The icon colour select follows when there is an icon.
- **Add a tile:** a sheet of kinds grouped under label-size 700 heads in ink-2 (Your library, Your own, From the app), each a fill-on-surface card at the 14px radius (14px padding) in an auto-filling grid of at least 17rem: its row tile (warm for the church's own and saved templates, accent for the app's), the name at row size, a caption line, its starting size in tabular ink-3; hover steps to strong sand.

### Tab bar canvas
The tab bar arranged on the desk, on the same phone as the Hub canvas: the draft's Home with the draft's tab bar, and the Tab bar panel beside it.
- **On the phone:** each tab is a button; hover draws an inset 2px marker ring at 60%, and the chosen tab the full 2px marker ring at the field radius. Clicking a tab opens it in the panel; Home and More open the Tab bar panel and can't be chosen (their tooltips say Home is always first and More always last).
- **Tab bar panel:** the panel head (Tab bar, then a caption: "Home first, up to three tabs you choose, then More."), Add a tab as the primary button for whoever may change it, then *On the bar, in order*: 44px sand rows at the field radius, 4px apart, each numbered in tabular ink-3, the tab's icon and word at caption size 600, an 8px marker dot when the draft changed it, and quiet *Move left* and *Move right*; the chosen row turns the surface colour with the inset 2px marker ring. Home opens the list and More closes it, each an outlined row with a lock in ink-3 ("Always first", "Always last" as its tooltip and for screen readers). Tabs not on the bar for the person previewed follow as quiet rows saying why, and a caption in ink-3 says a place that isn't a tab is still in More.
- **Settings panel:** the tile settings panel's shape, under a *Tab bar* back link: Opens (the place field, leaving out places another tab already opens), On the tab (the word, with its 12-letter limit as a hint, and the icon picker without *No icon* or uploads; with no icon yet it says "Comes with the place you choose"), Who sees it and when with *On the bar* (the switch that puts a starting tab back), and *Take it off the bar* as the danger action.

### Send feedback (phone)
Reached from a triple tap on the church mark at the foot of More (three taps within 1.2 seconds; the mark sits in a 7px-padded target that doesn't select or flash). Since 2026-11-12 it is written like a message (owner's pick on the decision page, `.impeccable/mocks/admin-nav-calendar/fb-composer.html`, after #55: "The send sheet and such just take up so much space, especially with keyboard up"), with the screenshots back at the top (#59: "Looks way too barren now. Put the images back up top where they were before. But keep the textbox and send like it is"): a pushed page under the solid top bar ("Send feedback", back to More), no masthead.
- **Pictures, at the top:** with none chosen, one dashed tile up to 12rem wide at 3:4, centred: an accent image tile, "Choose screenshots or videos" at row size and 700, "From your photo library. Up to 6." in caption ink-2. With some chosen, the stage: the chosen one large in its own shape (up to 9rem by 14rem, the 14px radius) with a 32px round Don't send on the video shade at its top right, beside a column of 46px 9:16 thumbnails (the shown one ringed 3px in marker, the rest at 72%) ending in a 46px sand + tile; "1 of 6 chosen" in caption ink-2, tabular, centred under it. This is the only way to add pictures; the composer has no paperclip.
- **Then** the page's lead in body size, ink-2, up to 65ch ("Something broken, looking wrong, or an idea? Say what happened below."), then what goes with it in caption ink-2 at 1.35, led by a small info icon ("Sent with it: …", which names the screenshot pages when there are some). The upper part scrolls on its own and never squeezes what's in it: with the keyboard up it keeps its order, the pictures (or the empty tile) then the text, and scrolls under the composer (#72). The page fills the screen to the tab bar, and to the keyboard while it's up.
- **Composer (shared `Composer`):** docked at the foot on the frosted bar colour with a hairline above: the message field (the field colour with its edge, 20px radius, starting three lines tall and growing to about six, body size) and a 40px round primary Send (the `send` icon, as in Chats) that stays outlined while there's nothing to send. While the field has focus the keyboard's ^ v ✓ bar is hidden, so the composer sits 8px above the keys.
- **After:** sent, waiting, refused, over the day's limit and "Send it from the app" (not on a phone) are each an empty state; sent carries a quiet Send another beside Done. Nothing typed or chosen is lost without asking.

### Send feedback (admin)
The sidebar foot's *Send feedback* opens a sheet over whatever page you're on, so the page it's about stays behind it (2026-11-11). From 720px wide it sits near the top (at least 48px down, 12% of the window height), 600px wide with 28px padding; narrower it is the usual bottom sheet. Title "Send feedback", lead "About the admin panel: something broken, looking wrong, or an idea?".
- **Taking pictures:** a row of two capsules 8px apart: *Capture this page* (tinted, camera icon), shown only where the browser can share a tab, and *Choose files* (sand, ink, a paperclip; strong sand on hover, a deep-aqua ring 2px out on focus). Capturing hides the sheet while the browser takes the picture; if the browser refuses or can't, a warning notice says so and points to pasting instead.
- **Drop zone:** under them, the shared `DropBox` (`FilePick Box`), centred: "Paste ⌘V or drop screenshots and videos" in label size at 700 in ink (the key in a sand key cap at the tile radius, "Ctrl V" on Windows, Linux and ChromeOS) over "Or click to choose. Up to 6" in caption ink-2. The whole frame is a file pick; pasting anywhere while the sheet is open adds the files, and dropping anywhere on the sheet does too. While files are dragged over the sheet the frame turns deep aqua on a 10% deep-aqua wash. It goes once six files are chosen.
- **Thumbnails** (`AttachmentThumb`, shared with the phone's filmstrip): 132 by 84px at the 9px radius on strong sand, ringed by a hairline, 10px apart, wrapping beside the drop zone. Each grows in from 92% out of a 3px blur over 280ms (none with reduced motion); a video carries the 22px play disc. While a file sends, its tile shows an up arrow and its name (caption ink-2, cut with an ellipsis) gently pulsing; a 28px round *Don't send* button sits 6px in at the top right on the video shade.
- **Then:** *Tell us about it* (multiline), the "Sent with it: …" line in caption ink-2 with its info icon (the page, the last few opened, the browser, window size and scheme, the version and the account), and any warning. The actions are Cancel (quiet) and Send (`SaveBar`) at the end; Send's reason when it can't send (files still sending, or nothing written or added) sits on its own line under them, right-aligned.
- **After sending:** the sheet closes and the sidebar foot shows "Thanks, it's sent." for 5 seconds: a line on the masthead chip at the 9px radius (8 by 10px in), caption at 600 in white, led by a check, fading in over 200ms and announced politely.

### Feedback board (admin)
`/feedback`: numbered tickets moving left to right through status columns, under the title band "Feedback" with its lead of what's waiting ("3 new · 1 in progress · 2 fixed, to check"). Dated 2026-11-11.
- **Where it came from:** above the board, 16px under the tabs, the admin filter chips "Everywhere", "The app" and "The admin panel".
- **Columns:** the shared board (Components, *Boards*), `Compact` while a ticket is docked. Each a sand track at the 14px radius, 10px in, its cards 8px apart, at least min(62vh, 640px) tall; at least 170px wide (140px with a ticket docked), Resolved narrower at three quarters. The head is fixed at two lines of label size at 700 in ink (1.35 line height, cut off past two) so every column's first card starts at the same height, its count at the other end in a white capsule (pill size, 600, ink-2, tabular). An empty column says so in a caption ink-3 line ("Nothing new."); Resolved shows its latest 20 and then "And N more. Find one by its number or words." A column under a drag takes an inset 2px deep-aqua ring.
- **Not doing:** folded by default into a 46px strip of the same sand track, its "Not doing · N" set vertically at label size and 700 in ink-2 (ink on hover); opened, it is a narrow column whose head folds it again. Cards drop on the strip as on a column.
- **Ticket card:** a white card at the tile radius, 10 by 12px in (12px at its foot), a hairline under it: an optional 88px cover (the first screenshot from its top, at the 9px radius on strong sand), "#12 · Ruth" in tabular caption ink-3, the title at row size and 600 in ink (wrapping, never cut off), then labels that wrap 4px apart: pill size at 700 in ink-2 on fill-on-surface at the 9px radius. A ticket from the admin panel leads with "Admin panel" in primary at 12% with primary text, then the browser; one from the app shows the app build and the phone's name. The build, phone and browser labels stay on one line and are cut with an ellipsis (the full text on hover); the source and state labels wrap instead, since their words carry the meaning. Then the file count in deep aqua at 12% with a paperclip, "Pictures deleted" once they're gone, and on a closed ticket the day its files go ("Files go 18 Nov"). A card that is Resolved or Not doing dims its cover to 62% and its title to ink-2. The open card is ringed 2px in marker. Press scales it to 0.98; focus is the deep-aqua outline 2px out. Cards drag between columns; clicking docks the ticket, and ⌘-click or Ctrl-click opens `/feedback/{number}` in a new tab, said in a caption hint under the board.
- **Docked ticket:** a head of "#12 · 2 hours ago" in tabular caption ink-3 (one line, ellipsis), *Open as a page* as a text link and a small quiet close, over the title in title type at 700 (balanced). Then the status as the stage pipeline (New, In progress, Fixed, to check, Resolved; up to 520px, 760px on the page), and under it, on their own lines, a quiet underlined *Mark not doing* (a quiet "Not doing" pill once it is) and a caption ink-3 hint: open, that pictures, videos and phone data are deleted 7 days after it's Resolved or Not doing; closed, when it closed, the day its pictures and videos are deleted, and that the ticket goes entirely 180 days after closing. Reopening a ticket keeps its files. Pictures and videos scroll sideways 12px apart, each 240px tall at the field radius on strong sand with its kind and size in caption ink-3 under it.
- **Ticket page:** the same ticket opened as a page opens on a title band (the title, "#12 · Ruth · 2 hours ago" as its lead, back to Feedback); pictures there are 420px tall. Its side, *Sent with it*, is a fact grid with 5.5rem labels in caption ink-3 at 600 on hairlines: From and Sent, then for a ticket from the admin panel Where ("The admin panel"), Version, Browser, Window (size and scheme), Page and Before (the pages opened just before), and for one from the app App, Phone and Page. A ticket from the app then has *This phone* (how much it has sent, *Block this phone* as a danger text link that opens its reason field) and the stored data as a small quiet download.
- **What's happened:** the timeline under the words: each step a caption line in ink-3 behind a hairline on its left, who at 600 in ink-2; a note a sand box at the field radius (10 by 12px) with its author and a 28px round delete. *Add a note* is a field over its own `SaveBar`.
- **Blocked phones:** the second tab, hairline rows at least 64px tall with a 34px sand phone tile in primary: the phone and how many it sent at row size and 600, the reason, then who blocked it and when in ink-3 with a link to its last ticket, and a small quiet Unblock. With none, a shield empty state.

### Chats desk (admin)
`/chats` and `/chats/{id}`: your own chats on a computer, the same chats and messages the phone has, written through the same methods. Dated 2026-11-13. Chats sits under Home in the sidebar, outside the areas, with its count of unread chats in the orange capsule (`INavCounts`); checking other people's chats is *Chat safety* (`/chat-safety`, Safety area, the shield icon `AppIcons.ChatSafety`).
- **Panes:** the full window height in a grid: the list at 280 to 340px, the conversation taking the rest, and with settings open a right pane at 300 to 360px; at 1360px and under they narrow to 260 to 300px (240 to 280px with settings open) and 280 to 320px. Panes are ruled off by 1px hairlines. Settings start open when the window is wider than 1360px.
- **One head line:** the list's title, the chat's `PaneHead` and the settings pane's head share one 88px band with a hairline under it, 20px in from the sides and 20px from the top, so the three heads line up. The list's title is title-large at 700, -0.02em; the chat's head is its name with a lead of its ministry, what kind of chat it is and how many people ("Kids Ministries · Team · group chat · 3 people"); the settings head is *Chat settings* at title size.
- **The list:** a sticky head of the title and the search field (12px above, 16px each side), then the rows flat on the page (no boxed card), grouped under each ministry's name (label size at 700 in ink-2, 16px above and 4px under; the ministry with the newest message first; owner, 2026-11-13: "make sure it's clear which chats belong to what ministry"). Each row's second line starts with what kind of chat it is ("Team · group chat", "Parents · leaders post", "Asking to join") before the latest message. A row's time sits at its top right in caption ink-3, tabular, with the unread count under it in a marker capsule (pill size at 700, on-marker, at least 20px); a muted chat shows the bell-off icon instead. The unread dot and the chevrons are dropped here, since the count says it. The open chat's row is on Selected Fill. A hint at the foot of the column says where chats come from and where to start one. With nothing open, the middle shows a centred empty state, "Choose a chat".
- **The conversation:** messages 16px from the top and 20px at the sides and foot; the composer and its stop or consent lines also 20px in. While files are dragged over, the conversation takes a 2px dashed deep-aqua outline 8px inside its edge. Each message's ⋯ (a small quiet icon button) sits 4px beside its bubble, on the outer side (each bubble's column fits its content, so the ⋯ stays beside a short bubble; 2026-11-13), fading in over 120ms on hover or focus and always shown on touch screens; the focused bubble takes a 2px deep-aqua ring 2px out. Its actions open in a 340px popover 4px above the message, held to the bubble's side, with the reaction picker at its top.
- **The cog:** a quiet icon button at the head's right; while settings are open it shows a 1.5px deep-aqua ring with a deep-aqua icon and no fill (`aria-expanded`).
- **Chat settings:** the same `ChatPeople` as the phone's sheet, 16px in from the pane's sides: *Mute this chat* as a toggle row in a settings group, *Who's in it* as single-line people with a 28px avatar and "Leader" at the right, then the hints. For a leader the head's ⋯ holds *Delete the chat…*, red, asking in a `Sheet`.
- **At 900px and under:** one pane at a time below the narrow window's bar: the list, or the chat with a quiet "Chats" back button above its head, or its settings in place of the conversation; the heads drop the 88px band for 8 by 16px padding.

### Checking a chat (admin)
`/chat-safety/check/{id}`, in *Chat safety*: one chat folded down to what you're looking for, in the shared workbench under a title band. Dated 2026-11-12.
- **Logged note:** first in the conversation and sticky at its top, a masthead-navy box at the field radius (12 by 14px in) with on-masthead caption text and a marker-orange icon, saying the reading is logged and who can see the log.
- **Messages:** sand tiles at the 10px tile radius (8 by 12px in): a caption head (who at 600 in ink, the time, any quiet pills, a ⋯ menu at the far end), an ink-3 caption quote of what it answers, the text, its photos (up to 220px, opening in the picture viewer) and its reactions as disabled reaction chips. A match stays sand with the found words marked in marker at 45% (at the 9px radius, a hair of padding). The message before and after a match goes transparent with a 1px hairline ring and ink-2 text. A reported message carries a 2px inset danger ring. Days divide the conversation with a centred ink-3 line.
- **Fold line (signature):** whatever doesn't match folds into one full-width Link button, "9 messages · Show", in caption ink-3 between dashed hairlines that fill the width on either side; deep aqua on hover. It opens in place.
- **Reports:** in the record pane, each report a card at the tile radius ringed 1px in the hairline (12 by 16px in), its title at row size and its reporter in caption ink-2, then a field and its own save bar to mark it handled; the checks of this chat are listed under them.
- **At 1360px and under:** the rail becomes a band of three columns above the conversation (search across the top, the Who list scrolling past 15rem); at 900px and under, one column.

### Motion
One curve, `cubic-bezier(0.22, 1, 0.36, 1)`, for presses, sheets and the section-tab underline. Pages appear at once (no entry fade or slide), and a screen opened again draws the data it last showed at once (`LiveData`'s `Remember`), refreshing it in place. Presses scale (0.94 to 0.98). The live dot pulses on a 1.6s cycle. A paged sheet slides a new page 14px in from the side it comes from (260ms), and the current page dot widens over 240ms. In the admin panel the sidebar's marker bar scales in over 240ms, an area's chevron turns over 200ms while its pages fade 4px into place over 200ms, hovers and fills ease over 160ms (120ms for table rows), and sort arrows fade over 160ms. Week-rail days fill over 160ms and month-grid days over 140ms; the rich text toolbar's buttons ease their fill over 140ms. A swipe from the left edge of a pushed page drags it away and goes back. On the Hub canvas tiles glide to new places over 220ms on `cubic-bezier(0.2, 0.8, 0.2, 1)` (a curve of their own), and a dropped tile settles on a gentle `linear()` spring. Reduced motion collapses every animation and transition to 1ms, in the app and the admin panel alike; on the Hub canvas the drag still works but tiles move without gliding or lifting.

## Implementation

The system ships in `src/Platform/GSBC.ChurchApp.Platform.Ui`, and both hosts link it: the app in full,
the admin panel for tokens, type and the shared components. The admin shell, the office week's columns and admin-only page styles live in the admin host's `wwwroot/css/admin.css`; the day rail is shared and lives in `ui.css`. A feature's own screens are styled in its Client's `wwwroot/css` (the announcements desk and the inbox in `notifications.css`, the calendar in Events Client's `events.css`, families, the blue card and a person's Family and Safeguarding tabs in People Client's `people.css`). The agenda row, the week rail, the toggle chips and the masthead's Beside row are shared and live at the end of `ui.css`; the rich text editor and `RichTextField` (`rich-text-field`, `cm-md-*`) are styled in `rich-text.css`, the writing page (`writing-desk`) at the end of `ui.css` with its full-bleed rule in `admin.css`, and the post editor in `news.css`. The editor foot bar is shared in `ui.css`. Send feedback and the Feedback board are styled in Feedback Client's `feedback.css`.

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
| Screen frame, top bar, back | `Page` | `BackHref` makes it a pushed page (back button, edge swipe); `OnBack` gives the back button its own action; `Actions` fills the top bar's end |
| Masthead | `Masthead` | Title, note, a slot for the live element, `Beside` for a control on the title's row, and a `Class` |
| Section and section head | `Section` | Title, lead, a "see all" link, a trailing slot; `Grouped` makes it a settings group (a card on the admin panel, an inset group on the phone) and `Foot` fills its foot |
| Callout, picture card, navy card | `Card` | `CardTone.Callout`, `Raised` (only where a picture leads, via `Media`), `Masthead` |
| Rows and boxed lists | `ListGroup`, `ListRow` | Leading tile tones, trailing text or content, chevron or external-link icon, disabled; `Reading` for entries to read, `TitleExtra` for a pill after the title, `Below` for more; `Unread` true or false sets the dot and the title weight; `Current` marks the open one (`aria-current`, `row--current`) |
| Pull to refresh | `PullToRefresh` | Wraps the whole page body, masthead included; the indicator sits under the top bar. Touch only, so offer a button too |
| Sheet | `Sheet` | Grabber, title, lead, body, actions; Escape and the scrim close it; centred on wide screens; `Cover` adds the navy band (then name it with `Label`); `Wide` for a list to choose from; opening moves focus into it without scrolling the page; it opens in a native `<dialog>` with `showModal()` (the top layer), so no scrolling, sticky or transformed ancestor can clip or hide it (Safari did), falling back to the fixed overlay where `showModal` is missing; opening focuses a child marked `autofocus` if there is one, and closing returns focus to where it was; `LayerClass` adds a class to the layer (Jump to's placement) |
| Sheet heading | `SheetHeading` | `Icon`, `Title` and an optional `Note`; goes in a sheet's `Cover` |
| Paged sheet | `PagedSheet` | `Count`, a `Cover` and `Page` per index, an optional `Action`; page dots, swipe, Next and Done |
| Facts | `FactList`, `Fact` | Label and text (or `ChildContent` for a rich value), with an optional icon tile; a fact with neither is skipped |
| Link in a row | `TextLink` | Deep-aqua label with a chevron and a haptic |
| Empty state | `EmptyState` | Icon, title, line, an action slot |
| Dates and times | `DateLine` | Church time, `Long`/`Short`/`Relative`/`TimeOnly`, ranges, "10:30am" and "6pm" as the bulletin writes them |
| Live/sync | `SyncIndicator` | Up to date, catching up, offline; plain or on the masthead |
| Buttons | `Button` | Primary, Quiet, Tinted, On masthead, Ghost on masthead, Live, Danger; block, small, icon-only, busy |
| Fields | `TextField`, `SelectField`, `ToggleRow`, `Segmented` | Labels tied to inputs; hint and problem lines are described-by; a `SegmentOption` can carry a `Count` |
| Copy a value | `CopyField` | Label, read-only value, Copy that turns to Copied, a polite status line; `ButtonOnly` is the copy button alone, `ButtonIcon` its icon (`link` by default, `copy` for a value that isn't a link) |
| Loaded data | `LiveData` | Loading, failure and stale states; reloads itself when what it depends on changes; `Remember` draws the last value at once when the screen is opened again (forgotten on sign-in, sign-out and access changes) |
| Loading | `Skeleton` | Rows, lines or a block, with a status for screen readers |
| Tab bar and More | `AppTabBar`, `MoreList`, `PhoneTabs` | The phone's tab bar draws the first four published tabs the person sees (`TabBar.For`), then More; More lists every module entry that isn't a tab now. Tabs are placements in the Tab bar area, chosen by staff in a draft |
| Admin sidebar | `ModuleNav`, `SidebarPlan`, `SidebarPins` | Grouped by `NavEntry.Area` (`NavAreas`: Sundays & sermons, This week, Ministries & groups, Safety, People & access, The app, Technical, and You at the foot); `SidebarPlan` lays out Home, Pinned, the areas (open or folded, with what's waiting) and the foot; `SidebarPins` keeps the account's pins; folds are remembered on the computer through `Shell`; `Counts` for the orange capsules |
| Jump to | `JumpTo` | A `Sheet` (`LayerClass` `jump-layer`) with a combobox over a listbox of up to 12 pages the person's role opens, matched on title, area and summary (`PickerMatches`); arrow keys and Enter; a polite count. `ModuleNav` hosts it and listens for ⌘K and Ctrl K |
| Icon picker | `IconPicker` (Configuration.Client) | The chosen icon and *Change icon*, opening the grid; uploaded icons and their upload for tiles, the app's own only for tabs |
| Admin title band | `TitleBand` | Title, lead or lead slot, back link, actions; sets the browser title |
| Admin tables | `DataTable`, `TableColumn` | Columns with a cell, an optional sort key and a class (`num`, `optional`, `quiet`, `truncate`); a toolbar slot, an empty slot, selection with the arrow keys |
| Table and detail | `SplitView` | The workbench without a rail (`workbench--stack`); only goes down |
| Search | `SearchField` | Labelled, settles before it reports the value |
| Filters | `FilterChips`, `FilterOption` | One choice from a set, with optional counts; `Rail` draws a group of rows for the workbench's rail, `ShowLabel` its label |
| Office week | `IOfficeWeekSource`, `OfficeWeek` | Features contribute tasks and dated spans (`Everyone`, `Layer`, `Draft`); `NavEntry.Summary` is an area's one-line purpose on the home page |
| Day rail | `DayRail` | `First`, `Today`, `Days` (7, or 14 for a fortnight), the spans, a hidden heading, the quiet line, a `Legend` of kinds and words, and the `Selected` href; a span whose end equals its start is a moment |
| Admin workbench | `Workbench` | `Aside` (the rail), main and `Detail` (the record) panes; `FoldAside` hides the rail while a record is open at 1360px and under; `Narrow` makes it the one-column form; `Showing` picks the pane in one column |
| Record head | `PaneHead` | `Title`, `Lead`, `Actions` (the ⋯ menu's `MenuItem`s), `Tools`, `OnOpen`, `OnClose` |
| More actions | `Menu`, `MenuItem`, `MenuItems`, `MenuButton` | `Label`, `Items` made with `this.Item(label, action, icon:, detail:, danger:, startsGroup:, @checked:, disabled:)`; `Text` and `Chevron` for a menu with words, `Block`, `Small`, `AlignStart`; `MenuButton` is one row in a hand-built panel |
| A small panel | `Popover` | `Label`, `Open`/`OpenChanged`; focus moves into it, Escape and a click outside close it |
| Roles | `RoleMenu`, `RoleChoice`, `RoleTick`, `RoleGives` | One role of several, with what each allows and an optional Remove; several at once as ticks |
| Boards | `Board`, `BoardColumn`, `BoardCard`, `BoardMove` | Columns (`Narrow`, `Folds`, `Shown` and `More`, `Addable`), `ColumnOf`, a `Card` template, `OnMove`, `OnAdd`, `Compact`; a card's `Shot`, `Meta`, `Labels`, `Open`, `Quiet` |
| Dates and times | `DateField`, `TimeField`, `DateTimeField` (`TypedDates`) | Typed with a calendar or time popover on the desk (`UiHost.Desktop`), the native field on the phone; `Min`, `Max`, `Hint`, `Problem` |
| A picture, bigger | `PictureOpen`, `PictureViewer` | `Pictures`, `Index`, `Noun`, `About`, `Open`; Download of the original |
| Light or dark | `SchemeChoice` | Automatic, Light, Dark through `Appearance`; `ShowLabel`, `Hint` |
| App icons | `AppIcons` (Platform.Contracts) | The one name for each kind of thing's icon; `IconRules` checks one icon per meaning |
| Gallery | `DesignGallery` | Every public component live with sample names and when to use it, at `/gallery` on both hosts |
| Pill | `Pill` | `PillTone.Marker`, `Accent`, `Masthead`, `Quiet`, `Danger` (run out only), with an optional icon |
| Charts | `StatTile`, `TrendLine` | `Quiet` for a value too small to show; a main and an optional comparison series, a table of every number, an `Empty` line under two points |
| Composer | `Composer` | `Label`, `Text`/`TextChanged`, `Placeholder`, `MaxLength`, `Rows`, `CanSend`, `Busy`, `SendLabel`, `SendIcon`, `OnSend`, `Class`, and `Attach`, `Above`, `Below` slots; `EnterSends` (Enter sends, Shift+Enter a new line) with `OnEscape` |
| Message bubble | `MessageBubble`, `BubbleReactions` | `Mine`, `Name`, `Time`, `Quote`/`OnQuote`, `Reactions`, `Status`, `OnLongPress`; `MoreLabel` adds the ⋯ beside it and makes the bubble focusable (Enter or the context-menu key opens it); `Tools` for what opens from it |
| Reorder list | `ReorderList` | `Label`, `Items`, `Key`, `Name`, an `ItemTemplate`, `OnReordered`, optional `Trailing` and `Preview`, `Disabled`, `Quiet` (hover handle, Alt+arrows, no ⋯ menu), `RowClass`, `Class` |
| Ranked list | `RankedList`, `RankedItem` | Title, explanation, label and value items, an `Empty` line; `Numbered` (default true) shows the rank |
| Save state | `SaveStatus` | Saved, waiting, saving, failed with a retry, not saved yet; an optional detail |
| Save action | `SaveBar` | Cancel (`OnCancel`, `CancelLabel`) at the left, the one save button at the right, the reason it can't save (`Blocked`) or its `SaveStatus` above; `New`, `OnMasthead`, `Block`, `Submit`; other actions as child content, after Cancel, moving above when narrow |
| Edits in children | `EditScope` | Re-renders the editor around it when anything inside is edited, so a header or a save button that reads a shared model stays current |
| Unsaved changes | `UnsavedGuard`, `UnsavedChanges` | Leaving a form with changes (another tab, page or sidebar link, Back, or an in-page switch that asks `UnsavedChanges`) opens a sheet: *Save and go on* (when the form can save), *Discard the changes*, *Stay here*; closing or reloading the browser tab gets the browser's own warning |
| Stages | `StagePipeline` | `Label`, `Stages` (`SegmentOption`s in order), `Value`/`ValueChanged`, `Disabled`, `ReadOnly`, `Class` |
| Sidebar counts | `INavCounts` (Platform.Client) | `Feeds` it depends on and `CountAsync` (a count per page href); `MainLayout` adds them to the office week's |
| Sidebar actions | `SidebarAction` (Platform.Client) | Title, icon, the permission it needs and the sheet it opens (given `Open` and `OpenChanged`); `ModuleNav` draws it in the foot above You |
| Tabs with counts | `TabStrip` | `TabOption` value, label and count; orange underline on the selected tab; a trailing slot; `Href` makes each tab a link to its area (the current one marked as the page); `Sticky` pins it on the phone |
| Preview banner | `PreviewBanner` | Docked above the tab bar; title, detail, Hide, Exit |
| Phone preview | `PhoneFrame`, `PhoneDevice` | A true-size device with status bar, cutout and tab bar, scaled to fit its space; an optional palette |
| Lock-screen proof | `LockScreenProof` | Title, body, `TimeSensitive`, the date line, and a caption slot |
| Home and surfaces | `HomeScreen`, `SurfaceViews` | The one way Home and the admin preview draw surfaces |
| Hub grid and tiles | `HubTiles`, `HubTile`, `HubIcon`, `HubTileViews` | The grid packs tiles by `HubGrid` (2 to 4 columns, up to 4 rows); `HubTile` draws a `TileShape` in a `TileTones` tone, with an optional `Banner` strip across its top; each kind's view picks its `TileVariant` |
| Hub canvas | `StylingPhone`, `HomePanel`, `TileSettings`, `AddTileSheet`, `HubLibrary` (Configuration Client) | Arranging, sizing and recolouring tiles in a draft; the Home screen panel; the settings panel with swatches, the layout picker and the contrast warning. `hub-canvas.js` carries the live drag and a port of `HubGrid`'s packing |
| App styling | `StylingWorkspace`, `StylingPhone`, `PreviewChips`, `StylingOverview`, `ColoursPanel`, `HomePanel`, `TabsPanel`, `DraftsMenu`, `DraftListing` (Configuration Client), `ThemeSwatches` (Platform.Ui) | The page, the phone with the Hub and Tab bar canvases, the preview chips, the panel's parts and the Drafts menu; styled in `drafts.css` |
| Choose several | `ChipPicker` | `Items`, `Chosen`, `Text`, an optional `Detail`, `Placeholder`, `Empty`; matches starting with the search come first |
| Who sees it | `AudiencePicker` (Content) | Everyone, Signed in, or several roles through `ChipPicker`; an audience it can't express shows as "Set up specially" |
| Choose a picture or file | `MediaChooser` (Content) | A wide `Sheet` around the registered `MediaPicker` (Media's `MediaChoice`); `ImagesOnly`; opens on the last folder used |
| Pick files | `FilePick` (Content) | `Label`, `Kinds` (`PickableFiles`), `Multiple`, `Limit`, `TooLarge`, `OnPicked` once per kept file, or `OnChosen` once with them all; `TakesPaste`, `DropOn` (a selector around it); `Input` for a caller that hands it files; child content (given whether it's busy) replaces the add row's icon and label |
| Show a picture, video or file | `MediaPreview`, `MediaVideo` | `Media`, `Controls`, `Size`, `Class`; a video plays inline and is muted without controls |
| Feedback | `SendFeedbackPage` (Feedback App); `AttachmentThumb` (Feedback Client); `SendFeedbackSheet`, `FeedbackBoard`, `TicketCard`, `TicketView`, `TicketPage`, `BlockedPhonesList` (Feedback Admin) | The phone's page with its stage and filmstrip; the admin panel's sheet; a chosen file's thumbnail in both; the board, the card, a ticket docked (`Docked`) or as a page, and blocked phones; styled in `feedback.css` |
| Prayer desk settings | `PrayedForTimes`, `PrayedTimes` (Prayer Admin) | The Settings pane of `PrayerDesk`: the timeline, the times (`PrayedTimes.Choices`, the day times 7am to 8:30pm) and their save, read through the shared digest schedule (`DigestSchedulesClient`); styled at the end of Prayer Client's `prayer.css` |
| Breadcrumbs | `Breadcrumbs`, `Crumb` | `Label`, a `Trail` of crumbs (label, key, icon), `OnOpen`; `CanDrop` and `OnDrop` make crumbs drop targets |
| Find someone | `PersonFinder` | `Find`, `ActionText`, `OnChosen`, `Leave` (ids to leave out), `Label`, `Placeholder`, `Busy` (the id being acted on), `AutoFocus`; the People sheet and the Family tab use it |
| Families and safeguarding | `AccountPeople`, `BlueCard`, `YourSafeguarding`, `ChildSheet`, `AddChildSheet`, `AskParentSheet`, `YourBirthDate`, `BirthDateAsk` (People Client); `PersonBirthDate`, `PersonFamilyTab`, `PersonSafeguardingTab`, `Safeguarding` (People Admin) | Your family and the blue card on Account (an `AccountSection`), your birth date in its You group (an `AccountYouPart`); a person's birth date in their title band (a `PersonFact`) and their Family and Safeguarding tabs (`PersonTab`); the Safeguarding page, whose Needs-you items come from `SafeguardingOfficeWeek` |
| Act on several | `SelectionTray` | `Label`, `Count` (hidden at 0), `One` and `Many` nouns, `OnClear`, the actions as child content |
| Media library | `MediaLibrary`, `FolderTiles`, `MoveToSheet` (Media Client) | The shelves, path row, tiles, grid or list, tray and folder sheets; `FolderTiles` has a `Compact` form for the chooser; `MoveToSheet` takes `Current`, `Suggested`, `Blocked` and an `Action` label |
| Places in the app | `PlaceDirectory`, `PlaceExplorer`, `PlacePhone`, `PlaceField` | Search, area chips and grouped rows; the directory beside the phone column with `PlaceAction`s (one `Leads`); a `PlacePreview` a feature registers, else the masthead summary; the picker field in a wide sheet. A holder with `data-fit-height` fits the phone frame to its height |
| Agenda row | `AgendaRow` | `Time`, `Title`, `Label`, `Subtitle`, `NotOn` (the circle-off line), `Quiet`, `Past`, `Warn`, `Selected`; a link, a button or plain |
| Week rail | `WeekRail` | `Days` (`RailDay`: date and whether anything is on), `Today`, `Here`, `Picked` |
| Choose several on and off | `ToggleChips` | `Label`, `Chips` (`ToggleChip`), `Toggled`, a `Trailing` slot; `toggle-chips--wrap` wraps instead of scrolling |
| Calendar | `DaySection`, `MonthGrid`, `EventHead`, `DateBlock`, `EventDetails`, `EventEditor`, `ComingUpTile` (Events Client) | The week, the month, an event and editing it on the phone and in the admin panel (`InPanel`); styled in `events.css`. The admin page is `CalendarWorkbench` with `CalendarSettingsPanel` and `BreakPanel` |
| Chats | `ChatsList`, `ChatView`, `ChatFrame`, `ChatPeople`, `ChatComposer`, `MessageActions` (Chats Client); `ChatsPanel` (Chats Admin) | One chat screen on both sides: `ChatFrame` is the phone's `Page`, or with `InPanel` a `PaneHead` head and a `Beside` pane; `ChatView` takes `InPanel`, `SettingsOpen`, `OnSettings`; `ChatsList` takes `Search`, `Current`, `ShowHint`; `ChatPeople` is the settings and people, in the phone's sheet or the panel's pane; `MessageActions InPanel` is a `Popover`; `ChatComposer` takes `EnterSends` and `DropOn`; styled in `chats.css` |
| Icons, mark | `Icon`, `BrandMark` | Lucide paths (ISC); the mark is the logo pack's file as a mask, never redrawn |

The Design gallery (`/gallery`, in the admin panel's Technical area and in the app's More, for staff who may send feedback) shows every shared component in the current palette. The drift tests (`DesignDriftRules`, `IconRules`, with their counts in `design-ratchets.json`) hold the pattern map.

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
