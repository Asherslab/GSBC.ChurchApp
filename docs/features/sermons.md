# Sermons

Listen to and watch the church's sermons: the latest on Home, a Listen tab with series, speakers and search,
audio that keeps playing in the background with the lock screen's controls, and progress that is never
forgotten. Asked for by the owner on 2026-11-04; SPEC §3 (*Sermons*). Written before the code, and kept
current as it's built.

## What people can do

| Who | Can |
|---|---|
| Everyone (the phone) | Browse the latest sermons, series and speakers; search; play audio in the background, with lock-screen, Control Center, AirPods and car controls; watch the video; pick up where they left off, offline too |
| Everyone, signed in | The same, with their listening history on every phone they sign in on |
| `sermons.upload` | In the admin panel: add a sermon by uploading its audio, when sermons come from uploads (the AV team's first pass) |
| `sermons.edit` | In the admin panel: check new sermons and correct a sermon's title, speaker, series, service and video; hide one; manage series (name, picture, description) and speakers (name, other spellings, joining two) |
| `sermons.source` | Choose where new sermons come from (the website, or uploads) |

The **Sermons editor** system role holds `sermons.edit` and `sermons.upload`; the **Sermon uploader** role (the AV team)
holds `sermons.upload`. `sermons.source` is the Owner's until given. The series and speaker lists and the source's
status are `[SignedIn]`: they are public data already (the feeds), and an uploader needs them to fill in the form.

## Where sermons come from: two sources, one library

Owner, 2026-11-04: build both, so the church staff can be shown each. "At worst we scrap the R2 based
build and keep the old." One library, with a setting, **Where new sermons come from**:

| | The website (Tithe.ly) | Uploads (our storage, R2) |
|---|---|---|
| Staff do | What they do today: upload to the website | Upload in our admin panel: audio, title, speaker, series, service, date, video link |
| Sermons arrive by | Reading `baptist.com.au/podcasts/media.rss` hourly | Being saved in the admin panel |
| Series from | The website's series pages (below) | Chosen at upload |
| Audio lives on | Tithe.ly's CloudFront | Our public R2 bucket |
| Apple Podcasts and Spotify follow | The website's feed, as now | Our feed, `feed.xml` in the public bucket (below) |
| The website shows | Itself, as now | Not changed automatically; see *The website, if we host* |

Both sit behind `ISermonSource` (Sermons.Api): the app, its feeds, progress and the admin panel's corrections
don't know which is on. Switching to uploads stops reading the website and starts a one-off **copy into our
storage** of every sermon's audio and artwork, so the church owns every file and nothing depends on Tithe.ly
afterwards. Switching back resumes reading the website; uploaded sermons stay. A sermon read from the website is
matched by its `guid`; an uploaded one has only our id. Ids never change, so links, history and progress
survive a switch.

### The website's feed (v1's default)

What it holds (checked 2026-11-04): 155 items, the last year only (Tithe.ly's feed drops older ones). Per
item: `title`, `link` (the sermon's page, also its `guid`), an mp3 `enclosure` on CloudFront with its length
(about 50 MB on average, the largest 100 MB), `pubDate` (the day, at midnight Brisbane), `itunes:author`
(the speaker), `itunes:image` (a 1400 × 1400 square crop from the site's resizer) and `itunes:summary`: a
service label ("Sun AM", "Wed PM", "Wed", "Sun 9AM - Adults Session", "Christmas Day"), sometimes a second
speaker, and "Watch at: https://youtu.be/…" (112 of 155). No series, no duration.

- **Read hourly** by the API (`WebsiteSermonSource`, a hosted service; one replica at a time through an
  advisory lock), an outbound call, with `If-None-Match` (the site sends a weak ETag) and
  `If-Modified-Since`. Nothing is fetched by phones: they get sermons through Sync.
- **Never deleted because the feed dropped it.** The feed keeps a year; we keep everything we've read
  (owner, 2026-11-04: keep from now on, no archive backfill; the website holds about 890 back to 1998, which
  a one-off import could bring in later).
- **What's parsed:** the service label (before " - " or "Watch at"), normalised for display ("Sun AM" →
  *Sunday morning*, "Wed PM" → *Wednesday night*, "Wed" → *Wednesday*, anything else kept as written); the
  YouTube id from any `youtu.be/…`, `youtube.com/watch?v=…`, `/live/…` or `/embed/…` link; the original
  artwork (the resizer's job names the CloudFront original, 1920 × 1080); the duration, read from the mp3's
  first few kilobytes (a ranged request: the Xing/VBRI header, or the bitrate for constant-bitrate files).
- **Artwork** is made into two copies on the server: 1280 wide for the sermon page and 480 wide for lists and the
  Hub, JPEG, named by their SHA-256 in the public bucket, so phones cache them forever and prefetch the small
  one; speakers' photos (from the series pages) become 192-pixel squares. Sermons stores these itself
  (Platform.Storage), as files the app makes; they are not staff media. **SkiaSharp** does the resizing (MIT; its
  `NativeAssets.Linux.NoDependencies` runs in the stock .NET container). The owner chose ImageSharp first
  (2026-11-04), but ImageSharp 3.2 and 4 refuse to build in Release without a Six Labors licence key, so it
  was swapped rather than adding a key to the build.

### Series from the website's series pages

Owner, 2026-11-04 (recommended): the website already groups sermons into series, kept by staff in Tithe.ly
(75 of the feed's 155 sermons, 16 series, with artwork). There's no Tithe.ly API (checked 2026-11-04: their
help centre documents only the RSS feed and a sermon importer), so we read the pages:

- `/podcasts/media` lists every series (`/podcasts/media/series/<slug>`, recent ones and *earlier series*);
  each series page links its sermons as `/media/<slug>`, which is exactly the feed's `link`. That link is the
  join, so a renamed sermon still matches.
- A series takes its name and its artwork (the page's series image) from the page.
- Read when the feed changed, and once a day regardless. A page that fails or no longer parses keeps the
  last known series, and the problem shows on the admin panel's Sermons page (*Where sermons come from*) and
  in the API's log, so a website redesign is noticed and nothing goes missing. (App health hears only about
  failed gRPC calls, not background work; a feature reporting its own problems there would be a change to
  Analytics.)
- **Suggestions** for sermons in no series: titles like "Anchored: Part 4 - Anchored In Hope" or
  "Cross Words - Part 7: …" suggest "Anchored" or "Cross Words" in the admin panel; staff accept or ignore.
  Never applied by themselves ("Commandment 6" belongs to "The Ten Commandments in Modernity").

### Corrections survive

Every field a source fills is kept as the source's value, beside staff's correction (null until made). What
the app shows is the correction, else the source's value. Re-reading the website updates only the source's
values, so a correction is never overwritten; *Use the website's* clears it. Speakers have other spellings
(`speaker_aliases`): "Ps Peter Mordh" and "Peter Mordh" are one speaker once staff say so. Not drafts:
sermons are a feature's own records (drafts.md, decision 1); a correction is live when saved.

### Uploads (the R2 version)

- **Add a sermon** (admin panel, `sermons.edit`): drop an audio file (mp3 or m4a, up to 300 MB), then title,
  speaker (pick or new), series (pick or new), service, the day, a YouTube link, and artwork (the series'
  by default, or a picture from Media's picture sheet). The audio goes straight from the browser to R2
  through a signed link (`Sermons.BeginAudioUpload`, as Media does), named
  `sermons/audio/<id>.<ext>`; the server checks it arrived, its size and type, and reads its duration.
  A sermon can be saved for later or set to appear at a time.
- **Audio tooling** (levelling, trimming, intro/outro) is the owner's next idea; it plugs in between the
  upload and publishing, and isn't built.
- **The podcast feed** (owner, 2026-11-04): `podcast/feed.xml` in the public bucket, rewritten whenever a
  sermon changes, with `Cache-Control: public, max-age=300` (the only object there that isn't immutable).
  A static file, so no API path is added (AGENTS.md). The owner said an RSS endpoint would also be acceptable;
  static was chosen because ~3 uploads a week need nothing more. Apple Podcasts and Spotify take a new feed
  URL in Podcasts Connect and Spotify for Creators. Apple moves existing subscribers when the old feed
  redirects (301) or carries `itunes:new-feed-url`; whether Tithe.ly can do either is to be checked with them.
  Our feed holds **every** sermon, not the last year.

### The trade-off, for the owner's pitch

| | Stay on Tithe.ly | Host it ourselves |
|---|---|---|
| Cost of storage | In the Tithe.ly subscription (not known to us) | R2, $0.015/GB-month, free egress (Cloudflare's price list, checked 2026-11-04). About 3 sermons a week at ~50 MB is ~7.8 GB a year: within R2's free 10 GB for the first year, then ~$0.12/month in year two and under $1/month after five years. Operations are pennies (Class B reads are free to 10M/month, and Cloudflare caches the files) |
| Staff workflow | Unchanged, manual: upload to the website, type the details | Upload once in our admin panel; the app, the podcast feed (and later the website) follow. Room for audio tooling |
| History | The feed shows one year; the app keeps from now | Everything, in the app and the podcast |
| Series and speakers | Read from website pages (fragile if the site changes) | Ours, chosen at upload |
| Risk | The website's markup changing; Tithe.ly's limits | We run it (backups are the database and R2) |
| The website | As now | Needs a change (below) |

**The website, if we host.** Tithe.ly Sites can import an RSS feed once (its Sermon Importer), but we found
nothing saying it keeps following an outside feed. Options, for later: keep uploading to the website as well
during a trial; embed our sermons on the site (an embed block pointing at a static page on our host that reads
public sermons over gRPC-Web, like `/open`); or move the site. To be decided with the staff.

## The app

Designed with impeccable's full process (see *Design*); the POC's Listen and sermon screens are the
starting point (the team liked them).

- **Listen** (a tab): the latest sermon in the masthead with Listen and Watch; *Keep listening* (started, not
  finished, most recent first); the latest sermons; series; speakers; a search field.
- **A sermon** (`/sermon/{id}`): artwork or the video, title, series and part, speaker, day and service,
  Listen and Watch, how far you got, *Mark as played* / *not played*, Share (later, with the owner's yes),
  *More in this series*.
- **A series** (`/series/{id}`): its artwork, its sermons in order, how many you've heard.
- **A speaker** (`/speaker/{id}`): their photo, their sermons, newest first.
- **Search**: titles, series, speakers, services and the day ("anchored", "parry", "christmas", "2026"),
  across everything on the phone, offline.
- **The player**: a mini-player above the tab bar on every screen while something is loaded (cover, title,
  play/pause, progress), and a full sheet from it: scrubbing, back 15 and forward 30 seconds, speed
  (0.8–2×), a sleep timer (15, 30, 45 minutes, end of sermon), and *Up next* (the next in the series, or the
  next newest), which plays on in the background.
- **Home**: the Hub's *Latest sermon* tile is real (below).
- **Offline:** every screen reads the phone's store. Downloading a sermon to keep is designed (below), not
  built in v1.

## Feeds

| Feed | Audience | Holds |
|---|---|---|
| `sermons.catalogue` | Public | The newest 400 published sermons (a sermon leaving the window is sent as a delete); `sort_key` is the day and time, `group_key` the series, `starts_at` the day |
| `sermons.series` | Public | Every series with a sermon in the window: name, artwork, description, count, first and last day |
| `sermons.speakers` | Public | Every speaker with a sermon in the window: name, photo, count, latest day |
| `sermons.progress` | Own records | The signed-in person's listening history (below), one row per sermon |
| `sermons.admin` | `sermons.edit` | Markers for the admin panel's live pages |

A sermon payload is about 600 bytes, so the catalogue is about 240 KB on a phone. The screens read whole small
feeds (series, speakers) and the catalogue a page at a time; search reads the catalogue once per change into a
small in-memory index (400 rows), which the window bounds.

## Progress: never forgotten

The owner's "massive pet peeve". Each sermon's progress is a small record, kept on the phone always, and in
the account when signed in.

| Field | Means |
|---|---|
| `Position`, `HeardAt` | Where you were, and when you were there (the device's clock, UTC) |
| `Duration` | How long the audio is, as the player measured it |
| `FinishedAt` | When you finished it: reached the last 30 seconds (the last 3% of something shorter than 1,000 seconds), or chose *Mark as played* |
| `WatchedAt` | When you opened or played the video |
| `ClearedAt` | When you chose *Mark as not played* |

**The merge rule** (`ListeningProgress.Merge`, Sermons.Contracts, used by the phone and the server alike):

1. **Where you were: the most recent wins.** The position from the later `HeardAt`; on a tie, the further
   one. Listening again from the start on another phone is honoured, and an older device coming online
   never drags you back.
2. **Finished and watched are sticky.** `FinishedAt` and `WatchedAt` are each the latest of the two; a sermon
   counts as played while `FinishedAt` is later than `ClearedAt`, and watched likewise. Only *Mark as not
   played* undoes them, and only the marks made before it.
3. **Every field is a maximum** (or the position that goes with the maximum `HeardAt`), so merging is
   commutative, associative and idempotent: entries can arrive in any order, twice, from any number of phones,
   and every device ends the same. Tested in `ListeningProgressTests`.
4. **Clocks:** the server refuses to store a time more than five minutes in its future (it clamps it), so a
   phone with a wrong clock can't pin a position forever.

**Where it lives:**

- **On the phone, always.** The native player journals progress itself (`PlaybackJournal`, below) every
  5 seconds while playing and at every pause, interruption, seek and when the app goes to the background, so a
  position survives the app being killed or the WebView being suspended. It's the device's own record and
  isn't cleared on sign-out.
- **In the account, when signed in.** Changed entries go to `Sermons.SaveProgress` (`[SignedIn]`) through the
  outbox (so offline listening reaches the account later), in batches, when playback pauses or stops, when the
  app comes to the foreground, and at sign-in. The server merges each entry into the stored one and writes the
  person's `sermons.progress` row, which reaches their other phones through Sync.
- **Signing in or up** sends every entry the phone has never sent, so what you listened to before you had
  an account becomes the account's (the owner's requirement). Nothing is lost if the same sermon was played
  on two phones: the merge rule decides.
- **What screens show** is the merge of the phone's journal and the account's row.

## The audio player

Native, so it plays with the screen locked, survives navigation and the WebView being suspended, and owns
progress (owner's requirements; SPEC §5's native layer stays generic: the player knows URLs and item ids, never
a sermon).

- **iOS:** `AVPlayer` with `AVAudioSession` `.playback` (spoken audio), `UIBackgroundModes` `audio`,
  `MPNowPlayingInfoCenter` (title, speaker, artwork, position, rate) and `MPRemoteCommandCenter` (play, pause,
  toggle, skip back 15, skip forward 30, scrub, next track when there's an up-next). Interruptions (a call, Siri,
  an alarm) pause and resume when iOS says to; unplugging headphones or AirPods pauses. Sleep timer and up-next
  run natively, so they work in the background.
- **Bridge contract** (`IAudioPlayer`, Platform.Client; platform-neutral): `LoadAsync(item, startAt, play)`,
  `Play`, `Pause`, `SeekTo`, `SkipBy`, `SetRate`, `SetSleepTimer`, `SetUpNext(items)`, `MarkAsync(id, finished |
  watched | cleared)`, `JournalAsync()`, and a `Changed` event with the state (item, playing, buffering,
  position, duration, rate, sleep timer, ended). An item is an id, a URL, a title, an artist, an album and an
  artwork URL. **Android** is stubbed; the browser (the admin panel) uses an HTML audio element behind the same
  interface.
- **Downloads (designed, not built):** `Download(item)` would fetch the audio with a background `URLSession`
  into Application Support, outside the media cache's size limit, listed in Storage with a size and *Remove*;
  the player plays the file when it's there. The bridge leaves room for it; built once the owner wants it.

## Video

YouTube, "proper embedding if possible", otherwise the YouTube app or Safari, **never an in-app browser**
(owner, 2026-11-04).

- **The problem:** since 2025 YouTube refuses embeds that don't identify themselves with an HTTP `Referer`
  (errors 152/153). Its terms ask embedded players to send one (`strict-origin-when-cross-origin`), and say a
  WebView must set it itself. Our app's pages come from `capacitor://localhost`, which sends none.
- **The plan to spike on the phone:** a static page on our https app host, `/embed/youtube.html`, that embeds
  the IFrame Player with the video id from its URL **fragment** (`#v=…`), so the request carries nothing per
  video and the file is byte-identical for everyone: static files, not an API path (AGENTS.md). The app
  frames it; the YouTube frame inside then sends our https origin as its referrer. Inline playback
  (`playsinline`), fullscreen, and picture-in-picture where iOS allows it.
- **Fallback:** open `https://www.youtube.com/watch?v=…` with the system opener (`Shell.OpenExternalAsync`):
  the YouTube app takes it if installed (a universal link), Safari otherwise.

**Spike, 2026-11-04 (iOS Simulator, iOS 26.3, GSBC Dev against `dev-app.baptist.com.au`): it works.** The app frames
`https://dev-app.baptist.com.au/embed/youtube.html#v=…`; the player reported *ready*, then *buffering* and *playing*,
inline in the page with captions, and no error 152 or 153. The embed page is served by the gateway as one of the
admin panel's static files (Hosts.Admin `wwwroot/embed/`), unchanged for every request; the video id travels in the
fragment, which never reaches the server. It answers `play` and `pause` messages from the app (so starting the audio
can pause the video), and reports its state back (`gsbc-youtube` messages), which is how *watched* is recorded and
how the app falls back to the YouTube app when an embed is refused. YouTube's terms are met: the referrer is our https
origin under `strict-origin-when-cross-origin`, the player is at least 200 × 200 (full width at 16:9), nothing overlays
it. `VideoEmbed` (Platform.Ui) is the component; the design gallery shows it. Still to check on the owner's iPhone:
picture-in-picture and fullscreen (iOS's own video controls). In production the page is served from
`app.baptist.com.au`, with `no-cache` like the panel's other unfingerprinted files.

## Places, Home and notifications

- **Places** (`IDestinationSource`, destinations.md): a sermon (`/sermon/{id}`), a series (`/series/{id}`) and a
  speaker (`/speaker/{id}`), so tiles, announcements and notifications can open them. `/sermons` is a page
  (Listen); it leaves `ComingSoonDestinations`. A kind's prefix is one segment and can't be a page's path, so
  the items aren't under `/sermons/`.
- **Home:** the `hub.sermon` tile kind moves from Configuration's waiting tiles into Sermons, keeping its key and
  layouts (Cover first, Where you're up to, Picture behind), so tiles staff placed keep working. It shows the
  latest sermon (or the one you're part-way through, for *Where you're up to*), with a play button that starts it
  in the player.
- **New sermon** notification: **left for later.** Not raised: a notification always reaches everyone's inbox, and only its push is optional, so raising one
  for every sermon would put three a week in every inbox. A kind that reaches only the people who opted in is a
  small change to Notifications (another station's area); when it exists, Sermons raises it once per sermon, only
  for one preached in the last three days and never on the first read.

## Permissions

| Node | Scope | Means |
|---|---|---|
| `sermons.edit` | Global | Correct sermons, manage series and speakers, add sermons by upload |
| `sermons.source` | Global | Choose the source; copy audio into our storage |

## Tests

Real rules only: the feed parser against a trimmed, anonymised sample (made-up names and links), series joined
by link, corrections surviving a re-read, the retention window, the merge rule's laws, progress saved
through the service reaching the person's feed and nobody else's, a sermon only once in the notification,
uploads checked on save.

## Decided while building

- **The phone (owner, 2026-11-05):** the tab is *Sermons*, beside Home; it opens on the sermon you were part-way
  through if you listened in the past week (`KeepGoing`), otherwise the newest; **audio first** (Listen leads, Watch
  is beside it when there's a video). Layout: *Shelves*, chosen on the decision page over a dated timeline with series
  bands and three segmented views (`.impeccable/mocks/decision/sermons/`; direction contract in
  `.impeccable/surfaces/`).
- **The admin panel (owner, 2026-11-05):** the AV team uploads (first pass), the office checks (second pass); an upload
  is on phones at once (later, with audio tooling, it will wait for a check: a held state and a publish permission).
  Layout: the *review queue* (Needs a look beside the sermon being checked, *Looks right · next*), chosen over an upload
  workbench and a two-pass board (`.impeccable/mocks/decision/sermons-admin/`). The source switch is on the page.
- **Needs a look** is a sermon nobody has checked: new from the website (with a reason when its title suggests a
  series it isn't in, or its speaker is new) or just uploaded. Everything from the website's first read, and what was
  there when the check was added, counts as checked, so staff don't start with 155.
- **Titles in a series** drop the series' name where the series line already says it ("Anchored: Part 4 - Anchored In
  Hope" shows as "Anchored In Hope" under "Anchored · Part 4"); elsewhere the full title shows.
- **The website's summary** ("Sun AM - Watch at: …") isn't shown as a description; only an uploaded sermon's own words
  are.
- **The mini-player and player sheet are Platform.Ui's `NowPlaying`**, generic over `IAudioPlayer`, placed above the
  tab bar in the app's layout; an `AudioItem` carries a link back to its sermon.
- **The admin panel plays audio** with the browser's own audio element on the sermon being checked; it has no
  mini-player.
- **Uploading** goes from the browser's file straight to R2 (an XHR with progress), not through WebAssembly memory;
  the browser reads an m4a's length, the server an mp3's.
- **Copying the website's audio** in development copies only the newest 20 (about 1 GB into the local store).
- **An architecture rule** (`NoTwoPagesInTheAppClaimTheSameRoute`) now fails when two app pages claim one route: the
  coming-soon page still claiming `/sermons` stopped the app starting.

## What the owner sets up

Nothing for the website version: it reads public pages, as any browser does.

For the uploads version, before it's used for real:

1. **The media host's cache rule** (Cloudflare → the zone → Caching → Cache Rules) caches everything on
   `<env>-media.baptist.com.au` for a long time, which is right for the content-addressed files but not for
   `podcast/feed.xml`, which changes. Add a rule above it: *URI Path equals `/podcast/feed.xml`* → *Eligible for cache*,
   *Edge TTL: use cache-control header if present* and *Browser TTL: respect origin* (the object carries
   `max-age=300`). Then check through the public host: `curl -sD - -o /dev/null https://<env>-media.baptist.com.au/podcast/feed.xml`
   should show `cache-control: public, max-age=300`.
2. **CORS on the public bucket** must allow `PUT` from the admin panel's host (`https://<env>-app.baptist.com.au`), as
   it already does for Media's uploads to the private bucket; uploads go to the public one.
3. **Storage:** copying the website's audio puts about 7.8 GB in the public bucket (R2's free tier is 10 GB).
4. **Moving the podcast:** in Podcasts Connect and Spotify for Creators, change the feed URL to the one shown on the
   Sermons page; ask Tithe.ly whether the old feed can redirect or carry `itunes:new-feed-url`, so subscribers move
   with it.
5. **Optional:** `Sermons:Podcast:ImageUrl` (the 3000 × 3000 podcast cover) and `Sermons:Podcast:Email` in the API's
   configuration; they aren't secrets.

To try it in dev, flip *Where new sermons come from* on the admin panel's Sermons page; in development only the newest
20 sermons are copied.

## Status (2026-11-05)

Built on branch `feature/sermons`:
- the website's feed and series pages read hourly into the catalogue, corrections that survive, uploads to R2 with a
  podcast feed and a one-off copy, places for a sermon, a series and a speaker (tests: `Sermons.Api.Tests`);
- the phone: the Sermons tab, sermon, series, speaker, search and all-sermons pages, the Home tile, the native player
  (iOS) with the playback journal (tests: `PlaybackJournalTests`), listening history merged and sent through the
  outbox;
- the admin panel: the review queue, Series and speakers, the source switch, uploads;
- YouTube inline through the static embed page.

Checked in the iOS Simulator (the tab, a sermon, the player sheet, playback, the video inline) and on
`dev-app.baptist.com.au` (the admin pages); installed on the owner's iPhone for the real-phone checks (background audio
with the screen locked, switching tabs, closing and reopening, a second device signed in, picture-in-picture).

## Open

- The real-phone checks above, by the owner.
- Downloads for offline (designed above).
- The *New sermon* notification (above).
- A held-for-review step for uploads, with audio tooling.
- Sharing a sermon from the phone (needs the owner's yes as phone UI, destinations.md).
- Notes and passages (SPEC §3): the feed has none; a field staff fill in can come with uploads.
- Importing the website's archive.
