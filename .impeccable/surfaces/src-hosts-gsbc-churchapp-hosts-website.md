---
version: 1
slug: "src-hosts-gsbc-churchapp-hosts-website"
primary_target: "src/Hosts/GSBC.ChurchApp.Hosts.Website"
related_targets: [".impeccable/mocks/website"]
---

# Website embeds (Events and Sermons on baptist.com.au)

Scope: the pages `Hosts.Website` serves at embed.baptist.com.au, shown in iframes on the church's Tithe.ly website: events list, an event, sermons list, all series, a series, a speaker, a sermon. Mode: Read (people find a date or a sermon and act on it: register, add to a calendar, listen).

Audience: visitors and members on the website, mostly on phones, some on computers; signed out, never staff. Job: find what's on and when, or play a sermon, without leaving the website. Constraints: the website's look (owner, 2026-11-08), only what everyone may see, no forms or writes, light only (the website has no dark mode), one stylesheet with the website's tokens.

Owner's answers (2026-11-08): own host approved; embed.baptist.com.au; share links are the website page's address; old sermon links may break; titles, descriptions and Open Graph on the embed pages; small Get the app / Add to your calendar / Listen in the app links, no banners; events show every public calendar with a filter, an embed can be limited with ?calendar=.

Decision page: `.impeccable/mocks/website/index.html` (Split reader, Shelves, Agenda). Chosen overnight: Agenda (owner may switch).

## Direction contract

seed: seed:2e6865572863ee5c (surface, read; dealt 7, 5, 3; built 3)

THESIS: the church's week, read top to bottom. Events by week with one date tile per day, regular services as quiet lines and one-offs in full; sermons lead with the latest, then by month. Refuses the poster grid and the empty "List | Calendar" switcher.

OWN-WORLD: the website itself: DM Serif Text for reading and headings, Montserrat Light for titles in sage ink, Montserrat Medium for buttons and chips, green #4A7355 for actions, 4px corners, grey panels, thin rules, Bootstrap's widths.

STORY: what's on this week and what's coming, then one tap to an event's facts or a sermon's player; the website's address follows so it can be shared.

FIRST VIEWPORT: events: calendar chips and Subscribe, then the next dated events; Regular gatherings follow them. Sermons: the latest sermon with Listen and Watch, then search.

SIGNATURE: the date tile (green weekday band, serif day number) anchoring each day and heading an event's page; the iframe that grows to its content and keeps the website's address in step.

AVOID: app chrome (navy, masthead, tab bar), cards for everything, banners or pop-ups for the app, kickers, private data, anything that varies per visitor.
