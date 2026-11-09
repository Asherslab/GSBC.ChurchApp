# Website embeds: events and sermons on the church website

The church website (baptist.com.au, a Tithe.ly site) shows the app's events and sermons in iframes, so nobody keeps
events or sermons in Tithe.ly's site builder any more: the app is the one place, and the website shows it. Asked for by
the owner on 2026-11-08 (events.md, *The website shows the app's calendar*, pinned on 2026-11-07). This doc records the
research, the design and each decision, then what was built.

## What the owner asked for (2026-11-08)

- Replace the website's **Events** page and its **Sermons** pages (the list, by series, a sermon's own page with
  playing it) with embeds from this app, in iframes, through Tithe.ly's source-code embed.
- They look like **the website**, not the app: its fonts, colours, icons, buttons, spacing and cards. They don't have to
  copy today's pages and can be better.
- **Their own module**, cleanly separated, so they can be removed later without touching the rest.
- Suggested: Blazor server rendering in a container of its own.

## Decided

### The owner's answers (2026-11-08, through the main session)

1. **A host of its own is approved:** Blazor static server rendering in its own container, serving the Sermons and
   Events embed pages as HTML to everyone ("Yes, approve SSR host"). The approval covers that host's public, read-only
   embed pages and nothing else: no forms, no writes, nothing per person, no JSON. It is in AGENTS.md's approved
   non-gRPC paths and `EndpointRules`' allowlist. The host reads only what everyone may see.
2. **Hostname: `embed.baptist.com.au`,** its own container. The owner sets up DNS, the tunnel and Cloudflare.
3. **Share links are the website page's address** (`baptist.com.au/sermons?sermon=…`). A small script on the website
   keeps the address in step and sizes the iframe. Without the script, the iframe has a fixed height and shares its
   own address.
4. **Old website sermon addresses** (`baptist.com.au/media/<slug>`) are allowed to break. No redirects.
5. **Search engines: the cheap part only.** Embed pages have proper titles, descriptions and share previews (Open
   Graph) on `embed.baptist.com.au`, so search can index them there, with a link back to the website. Nothing more.
6. **The app, gently:** small *Get the app* and *Add the church calendar to yours* links, *Listen in the app* on a
   sermon. No banners or pop-ups.
7. **Events show every public calendar by default, with a filter.** An embed can be limited to chosen calendars
   (`?calendar=kids`, or several: `?calendar=kids,youth`) for other pages of the website.
8. **Proper events by date, day-to-day ones once** (owner, 2026-11-08). The old website listed Outreach but not
   Sunday morning and night. So the list shows one-off events and changed dates of regular events (specials, such as
   *Anniversary Sunday Service*); unchanged dates of a regular event are left out, by the same rule the weekly digest
   uses (`OccurrenceListing.IsOneOffOrSpecial`, Events.Contracts). Each regular event has a staff switch, **List every
   date on the website** (off by default; Events data, on the regular event's page under *How it runs*, in the admin
   panel and on the phone), and with it on every date is listed (Outreach). Regular events not listed date by date
   appear once under **Regular gatherings**: title, how it repeats and its time, place, a link to its next date, and
   its next date that's not on, with the break's name and when it's back.
9. **Build it overnight on the recommendation,** and pick the look; the decision page keeps the alternatives so the
   owner can change it in the morning.

### Where it lives: one host, `Hosts.Website`

The pages read Sermons' and Events' public data, and **features never reference each other** (`ProjectReferenceRules`),
so a feature folder of its own (`Features/Website`) couldn't read either feature's contracts. Hosts compose features,
so the embeds are one project, `src/Hosts/GSBC.ChurchApp.Hosts.Website`: its pages, its stylesheet, the two scripts and
the reader of the public feeds. It references Sermons' and Events' Contracts and Clients (to reuse `SermonShelf`,
`SermonText` and `EventWords`, the app's own wording) and Platform.Ui (for `RichTextView`), and nothing references it
but the AppHost.

**Removing it later:** delete the project and its tests, its lines in the AppHost, `publish.sh`, the solution and the
project map, the *Website embeds* row in AGENTS.md and the matching entries in `EndpointRules`, and this doc. Nothing
else changes; the website goes back to Tithe.ly's own pages.

### How it reads the data: the public feeds, as a signed-out phone does

The host holds one `Sync.Watch` stream to the API (cluster-internal native gRPC, as the gateway's iCal links call
`CalendarFiles`), for the five public feeds: `sermons.catalogue`, `sermons.series`, `sermons.speakers`,
`events.calendars` and `events.public`. It sends no token, so it gets exactly what a signed-out phone gets: **who sees
what stays Access's**, decided where the feeds are written. Private calendars, their events, contact phone numbers and
emails, hidden sermons and anything not yet shown never reach it. A row the API takes away (an event saved for later, a
calendar made private) arrives as a delete or a reset, and leaves the host's copy at once.

- **In memory, rebuilt on each change** (`PublicFeeds`): a few hundred kilobytes (400 sermons, about a year of public
  events). Pages read the latest copy; nothing is fetched per request.
- **Changes show within a second or two** of being published, as on phones; the stream reconnects by itself, resuming
  from its cursors.
- **Ready only once it has data:** the host's gRPC health has a `ready` check that passes after the first full read, so
  Kubernetes sends no traffic to a host with nothing to show.

The alternative, a gRPC method per page (`Sermons.Page`, `Events.ForWebsite`), was set aside: it would add public
methods to two features for one consumer, and the feeds already are the public, permission-checked copy.

### The look: the website's, made clearer

From the live site (2026-11-08) and its stylesheet (`/themes/stylesheet.css`, a Tithe.ly theme on Bootstrap 4):

| | The website | The embeds |
|---|---|---|
| Reading and headings | DM Serif Text (Google Fonts), 15px body | The same, 16px body, 17px in an event's About |
| Titles in lists | Montserrat Light 300, 32px, sage `#84A87D` | Montserrat Light, 22–24px in lists, in a darker sage (`#5E8257`) so it passes contrast |
| Buttons, chips, menus | Montserrat Medium 500, 15px, 4px corners, grey `#F0F0F0`, active sage | The same shapes; the active and primary button in the header green `#4A7355` (white on sage fails contrast) |
| Greens | Header `#4A7355`, sage `#84A87D`, `#699161`, deep `#36543E`, `#2C4433` | The same, as tokens (`tokens.css`) |
| Neutrals | Ink `#222`, greys `#777`, `#E6E6E6` rules, `#F0F0F0` panels, footer `#333` | The same; secondary text `#555`/`#6B6B6B` (`#777` is just under 4.5:1) |
| Icons | Font Awesome 3.2.1 | Drawn line icons of one weight in `icons.svg` (calendar, clock, place, repeat, play, download, share…) |
| Width | Bootstrap container, 1160px with 10px padding; breakpoints 576, 768, 992, 1200 | Content up to 1140px inside the iframe, the same breakpoints |
| Around the content | Green menu bar with the mark in the middle, a page band (sage with a photo on Events, a green texture on Sermons), dark footer | Left to the website: the iframe sits in the page's content area |
| Dark mode | None | None (light only) |

The yearly theme is the website's 2026 greens; the brand's navy and aqua are not used here. Next year's theme is a change
to `tokens.css` only.

**What the old Events page showed** (Wayback Machine, 2025 to mid 2026; staff stopped adding events in September 2026):
a *List | Calendar* switch, a *Filter* menu of categories and *Subscribe to Calendar*; featured events as picture cards
(picture, title, the date, the time, a one-line summary); then *This Week's Events*, a day heading and each event's title
and time. An event's page had its title and a one-line subtitle, the date, the time and the place with icons,
breadcrumbs, a notice once it had passed, its picture, *Save to your Calendar*, the description (when, where, what to
bring) and, beside it, *Upcoming Events* and the *Latest Sermon*. Recurring services were listed one dated page per
week.

**What the Sermons pages show today:** a *By Date | By Series* switch; rows of the series art (16:9) beside the title
(Montserrat Light, sage), the service ("Sun AM"), the series, the Bible passage, the speaker's photo, name and role, the
date and an audio or video mark; a filter column of series, speakers (with counts) and years; numbered pages. A series
page has its art and its sermons; a sermon's page has the YouTube video, an audio player with *Download*, the passage,
the series art, and the series' other sermons beside it.

### The layout: Agenda (picked overnight; the owner can switch)

Three layouts were dealt on the decision page, [`.impeccable/mocks/website/index.html`](../../.impeccable/mocks/website/index.html):
**Split reader** (a list beside the open item), **Shelves** (series shelves scrolled sideways, events as month posters)
and **Agenda**. Agenda was built: it reads the same top to bottom on a phone and a computer (which suits an iframe that
grows to its content), keeps the app's calendar decisions, and is closest to the pages people know, made clearer. The
surface brief with its direction contract is `.impeccable/surfaces/src-hosts-gsbc-churchapp-hosts-website.md`.

- **Events** (`/events`): calendar chips (*All calendars* and each public calendar) and *Subscribe in your calendar
  app*; *Regular gatherings* (answer 8 above) in a grey panel; then *This week*, *Next week*, *Week of 22 November*,
  each day a date tile (green weekday band, serif day, month; today tinted) beside its events. Dates of a regular
  event listed every date are quiet single lines with a repeat mark; one-offs and specials are full rows (title, time, place, calendar or label, picture on the right). A date that's not on keeps its
  line, struck through, with "Not on this week · back on 29 November". *Changed* marks a recent change. Eight weeks at
  first, then *Show the next 8 weeks* (up to the twelve months the feed holds).
- **An event** (`/events/{id}`): *All events*, a large date tile beside the title and the when line, the repeat line
  for a regular event, the change note, the picture, *Register* (when there is one), *Add to Google Calendar*, *Share*,
  the About text; beside it the facts (when, where with *Open in Maps*, contact names, the calendar with *Subscribe to
  this calendar*), and *Other dates* of a regular event as small tiles. Missing details read in plain words (*Place to
  be announced*).
- **Sermons** (`/sermons`): the latest sermon with its art, series and part, speaker, day and service, length, *Listen*
  and *Watch*; a search box (titles, series, speakers, services, the day), *All series*, *Speakers* and *Podcast*; then
  the sermons by month in the website's rows; twenty a page, *Older sermons*.
- **All series** (`/sermons/series`), **a series** (`/sermons/series/{id}`: its art, description, count and months,
  its sermons in order), **speakers** (`/sermons/speakers`) and **a speaker** (`/sermons/speakers/{id}`).
- **A sermon** (`/sermons/{id}`): the YouTube video (or the art), the title, series and part, speaker, day and service;
  the audio player; *Listen in the app*, *Download*, *Watch on YouTube*, *Share*; beside it *More in this series*.

Contacts show names only, as the app shows them to people signed out; phone numbers and emails stay for people signed
in to the app.

### Paths and the HARD RULE

All on `Hosts.Website`, all `GET`, all the same for everyone (approved 2026-11-08; AGENTS.md lists them):

| Path | Shows |
|---|---|
| `/events` | Every public calendar's events (`?calendar=` limits them; `?weeks=` shows more) |
| `/events/{id}` | One date of an event |
| `/sermons` | The latest, search (`?q=`), by month (`?page=`) |
| `/sermons/series`, `/sermons/series/{id}` | All series; one series |
| `/sermons/speakers`, `/sermons/speakers/{id}` | All speakers; one speaker |
| `/sermons/{id}` | One sermon |

Static files beside them: `tokens.css`, `website.css`, `icons.svg`, `frame.js` (fingerprinted when the pages name them)
and `embed.js` (the website's script, under its plain name). There is no root page and no sitemap.

### On the website

What staff paste into a page's source code (Tithe.ly documents `<iframe>` and `<script>` embeds for the source-code
editor; tried on hidden test pages on 2026-11-08, where both survived, though the editor wrapped the iframe in a `<p>`
and dropped its `data-` attribute):

```html
<iframe src="https://embed.baptist.com.au/sermons" title="Sermons"
        style="width:100%;height:1200px;border:0"></iframe>
<script src="https://embed.baptist.com.au/embed.js" defer></script>
```

For events, `src` is `https://embed.baptist.com.au/events`; a Kids page could use `/events?calendar=kids`. The script
finds its iframes by their address (any iframe from the same host as the script), so nothing else is needed.

**Full width (2026-11-08, the owner on the test page: "it isn't full width").** A Tithe.ly text page puts its content in
a centred column (`.container > .row.subpage-main > .col-md-8.col-md-offset-2 > .article`), about 750 pixels wide on a
computer. The script widens each iframe to the page's `.container` content box (1140 pixels, the same left edge as the
header's title and the footer), measured from the page, so it lines up whatever wraps it, and only ever widens: on a
phone the column already is the content width. It follows the window as it resizes, and touches nothing but its own
iframes. Inside the iframe the pages drop their side padding, so their text starts where the website's does. To keep
an iframe in its column, give it `data-gsbc-width="column"` or `class="gsbc-column"` (the class survives editors that
drop `data-` attributes). Without the script the iframe stays in the column at its fixed height and scrolls inside;
every page works from 360 pixels wide, so that's still readable, just narrower.

Tithely Sites' block pages have a width setting per block (*contained* or *full-width*, under *Edit Block*), per their
help centre; a page built from blocks with an HTML block set to *contained* would sit at the container width without the
script's help. Not checked in the editor (the help centre sits behind a bot check); the script makes it unnecessary.

**On a computer the pages use the room** (2026-11-08): the sermons list is a grid of sermon cards (art, title, series,
speaker, day, length; three across at the website's width) beside a sidebar with search, the latest series with their
art, and recent speakers; Events puts *Regular gatherings* in a sidebar beside the weeks. Under 992 pixels (a tablet's
column, a phone) they return to one column: search and links above the list, gatherings after the weeks.

- **With the script** (`embed.js`): the iframe grows to its content (the page inside posts its height on every change;
  the iframe stops scrolling inside only once the first height arrives), so the website scrolls as one page. The website's address follows what's open inside: `?sermon=<id>`,
  `?series=<id>`, `?speaker=<id>`, `?event=<id>`, `?view=series|speakers`, and the list's own `?q=`, `?page=`,
  `?calendar=`, `?weeks=`; opening that address opens the same page in the iframe, so it can be shared and
  bookmarked. Moving to another page inside scrolls the website back to the iframe's top. The browser's Back button
  steps back inside the iframe, as it always does for frames; the script replaces the address rather than adding to
  history, so Back isn't doubled.
- **Without the script** (if Tithe.ly strips it): the iframe keeps its fixed height and scrolls inside; links still
  work; *Share* gives the embed's own address, which works anywhere. The page inside knows which applies because the
  website's script answers its first message.
- **Opened on its own** (a search result, a shared embed link): the page shows a thin green bar with the church's name
  and a link to the matching page on the website.

### Moving between pages without a flash (2026-11-09)

The owner saw a flicker moving between pages on the test pages ("i expect that might be iframe handling stuff"). Frame by
frame, each move was a full reload inside the iframe: about ten blank white frames, the green *on the church website*
bar showing for a frame or two (it was hidden only once `frame.js` ran), and the text shifting ten pixels as the framed
padding arrived. Fixed:

- **Pages swap in place.** `frame.js` follows links and the search form to the same approved pages itself: it fetches
  the next page (the same `GET` a click would make), puts its `<main>` in place of the current one, and keeps the
  address with `history.pushState`, inside a view transition where the browser has them. Back and forward work (the
  browser's history steps through the iframe as before, and the website's address follows), and anything else (an
  error, an outside link, a download) is an ordinary link. Measured on the test pages in Chromium and WebKit: before,
  every move reloaded the frame; after, none do, and no blank frame shows.
- **No new path.** Blazor's enhanced navigation would do the same, but it needs `blazor.web.js` and Razor Components'
  endpoint mapping, which adds Blazor's own *opaque redirection* endpoint that the approval doesn't cover; the swap is
  forty lines on the eight approved pages.
- **The framed look is set before the page paints:** a one-line script in `<head>` marks the page as framed, so the green
  bar and the padding never flash. When a page does load in full (the first one, or a fallback), `@view-transition {
  navigation: auto; }` and a white background on the page avoid a blank flash where the browser supports it.
- **Fonts are the site's own files,** DM Serif Text and Montserrat (latin subset, woff2, SIL Open Font License, licences
  in `wwwroot/fonts/`), preloaded from the page's `<head>` with `font-display: swap`, rather than Google Fonts.
- **The iframe keeps its height** until the new page reports its own (it never falls back to 1200), and when the new page
  starts above the window, the website scrolls smoothly to the frame's top (instantly with *reduce motion*), never
  otherwise.

**Other dates** on an event's page are fixed-size tiles in a grid that wraps (eight across on a computer, five on a
phone), each a link to that date; they had been squeezed to the width of their numbers.

### Search engines and share previews

Each page has a `<title>`, a description, a canonical address (its own, on `embed.baptist.com.au`) and Open Graph
tags (title, description, the event's picture or the sermon's art). Iframes are indexed as their own pages, not as part
of the website, so search finds `embed.baptist.com.au/sermons/…`, whose bar links back to the website.

### Playing a sermon in an iframe, on an iPhone

The page uses the browser's own `<audio>` player (`preload="none"`, so nothing downloads until someone presses play),
which iOS Safari plays inside an iframe after a tap. `frame.js` sets the Media Session (title, speaker, series, art), so
the lock screen and Control Centre show the sermon and its controls. Audio keeps playing with the screen locked, as
Safari allows for audio; it stops if the person leaves the page. The video is YouTube's embed, sent our https origin as
its referrer. For more (keep listening across pages, progress kept), *Listen in the app*.

### Caching (Cloudflare sits in front; checked through the public hostname)

| What | Production (`embed.baptist.com.au`) | Dev (`dev-embed.baptist.com.au`) |
|---|---|---|
| Pages | `public, max-age=60`: a browser keeps a page a minute; Cloudflare doesn't cache HTML by default, so changes show within a minute | `no-store` |
| A page that isn't there (404) | `no-cache` | `no-store` |
| `website.css`, `tokens.css`, `icons.svg`, `frame.js` | Fingerprinted names, `immutable` (`MapStaticAssets`) | `no-store` |
| `embed.js` | Its plain name (the website names it), `no-cache` with an ETag: revalidated every time | `no-store` |

The zone's Browser Cache TTL is already *Respect existing headers*, so these headers stand. No cache rule is needed.
Changing what's cached needs a purge of the host (the owner's Cloudflare dashboard).

**Framing:** pages send `Content-Security-Policy: frame-ancestors` with the website's hosts (`https://baptist.com.au
https://www.baptist.com.au`, the `Website:FrameAncestors` setting), so no other site can frame them. Development allows
any.

### What the owner sets up

1. **Dev:** a public hostname for the dev tunnel, `dev-embed.baptist.com.au` → `http://localhost:5320` on the dev Mac
   (`cloudflared tunnel route dns gsbc-dev dev-embed.baptist.com.au`, and the entry in `~/.cloudflared/config.yml`, as
   in `tools/dev-proxy/cloudflared.yml`). The AppHost runs the host on port 5320 (plus `Dev:PortOffset`).
2. **UAT and production** (gsbc.argo): a deployment of `ghcr.io/asherslab/gsbc-churchapp-website` (built by
   `tools/containers/publish.sh` beside the others) with `Api__Address` set to the API's internal gRPC address, gRPC
   liveness and readiness probes on port 8081 (`ready` waits for the first read), a service, and the tunnel's public
   hostnames `uat-embed.baptist.com.au` and `embed.baptist.com.au` to port 8080. Then the UAT workflow can pin its
   image as it does the others' (its `#image` count goes from three to four).
3. **Cloudflare:** nothing new beyond the hostnames; check with
   `curl -sD - -o /dev/null https://embed.baptist.com.au/sermons` (and `/embed.js`, and a fingerprinted CSS file) that
   the headers above arrive and that nothing else caches HTML.
4. **The website:** paste the embed on the Events and Sermons pages (above), first on a hidden page.

## Alternatives considered (2026-11-08)

| | A. Server-rendered host (built) | B. Static files calling gRPC-Web | C. Pages generated ahead of time into R2 |
|---|---|---|---|
| What | Blazor static SSR in its own container, reading the public feeds | A small WASM app (or plain JS) in the iframe calling the gateway's gRPC-Web, anonymously | A generator writes every page as HTML into R2 when sermons or events change, as `feed.xml` is |
| First load in an iframe | HTML and one small stylesheet; fastest | A WASM runtime (several MB, cached later) or a hand-written JS gRPC-Web client and protobuf decoding; slowest the first time | HTML from Cloudflare's edge; fastest |
| Search and share previews | Titles, descriptions and Open Graph per page | Rendered in the browser: weak | Per page, like A |
| Freshness | Within a second (the stream), a minute in browsers | Live | A rewrite per change; an event's "this week" and "Not on" shift daily, so a nightly rebuild too |
| Caching | Pages briefly, assets forever | Static files forever, data never (gRPC is POST) | Everything at the edge; a purge or short TTL per change |
| HARD RULE | Needs approval (HTML with data is an API path): given | No new path, but CORS on the gateway for another origin, and `Sync.Pull` is a big call to make from every visitor | No path, but a generator and R2 writes in two features, and the website's search (`?q=`) has nothing to run on |
| Removal | One host project | One static app | Generator code inside Sermons and Events |

## Status

**Built (2026-11-08, overnight):** `Hosts.Website` with every page above, `PublicFeeds` over `Sync.Watch`, the two
scripts, the caching and framing headers, health with a `ready` check, the AppHost (port 5320 plus the offset; it waits
for the API), `publish.sh`, the project map, AGENTS.md's approved paths and `EndpointRules`' allowlist. Events gained *List every
date on the website* (a column on `events.regular_events`, `ListEveryDate` on the regular event and on each dated row
it publishes). Tests (`Hosts.Website.Tests`, unit, no I/O): applying feed batches (a delete or reset removes rows),
events limited to chosen calendars (an event shown on another calendar included), which dates are listed and which
gathered once with their next break, the website's share address for each page, the caching per
response, and framing.

## Open

- **Tithe.ly's editor and `<script>`:** documented as allowed; to try on a hidden page. The fallback is above.
- **The website's Events and Sermons menu links** still point at Tithe.ly's pages until staff paste the embeds.
- **Older sermons:** the catalogue holds the newest 400 (sermons.md, *Feeds*); the website held about 890. Importing the
  archive (sermons.md, *Open*) would bring older ones here too.
- **Bible passages** show on the website today but aren't in the feed; they arrive with notes and passages
  (sermons.md, *Open*).
- **A sitemap** for search engines would be a generated page: not asked for, and it would need the owner's yes.
- **A single event's calendar file** (*Add to Apple Calendar* for one date) would need another path; today the page
  offers Google Calendar for the one date and subscribing to its calendar for everything.
