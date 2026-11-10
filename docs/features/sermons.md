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
| `sermons.upload` | In the admin panel: add a sermon by uploading its audio (the AV team's first pass) |
| `sermons.edit` | In the admin panel: check new sermons and correct a sermon's title, speaker, series, service, Bible passage, description and video; hide one, and delete it once it's hidden; manage series (name, picture, description) and speakers (name, photo), and delete them; choose each sermon's service from the standard ones or add one |

Two roles, both under *Sermons* in the roles-by-feature scheme ([access.md](access.md)): **Sermons Uploader** (the AV
team) holds `sermons.upload`, and **Sermons Editor** holds `sermons.edit` and `sermons.upload`. Both bring Media's
`pictures` kit (see the library and upload), as the other editor roles do; their keys are unchanged, so everyone who held
them keeps them. `sermons.import` is in no role: only the Owner (`*`) runs the import, by default. The series and speaker lists are `[SignedIn]`: they are public data already (the feeds), and an
uploader needs them to fill in the form.

## Sermons live in the app (owner, 2026-11-09)

"We'll shift the whole sermons stuff over to the app when it goes into production, using the embed for the website.
With that being the case, I want us to completely chop out/off the website syncing stuff / side of the sermons
feature." So sermons are the app's own: the AV team uploads each one in the admin panel, the office checks it, phones
get it through Sync, and the church website shows it through the embed pages ([website.md](website.md)). Nothing reads
the old website (Tithe.ly) any more, and nothing in Sermons knows it existed.

- **One set of fields per sermon:** title, speaker, series, service, Bible passage, description (*About this
  sermon*), video, picture, audio. Staff edit them directly; there is no website value beside a correction.
- **A series has one picture and a speaker one photo,** chosen from Media (or removed).
- **Ids are random** (UUIDv7) for what is made here; the import derives its ids (see [sermons-import.md](sermons-import.md)).
- **Bringing the old website's sermons across** is a one-off admin page in its own module, *Import from the website*,
  deleted after go-live: [sermons-import.md](sermons-import.md). It copies every sermon (not just the ~100 the RSS feed
  lists), with its audio and pictures, into our storage.
- **Dev and UAT were wiped of the website's sermons** by the migration `SermonsAppOnly`: every sermon read from the
  website goes, with the series and speakers only those sermons used, a feed delete for each (so phones drop them) and
  their Media uses released (the files stay in the app's system folders, so the import finds them by their content
  hash and stores nothing twice). Uploaded sermons stay, with staff's corrections made their values. Listening progress
  and series follows are kept: the import gives each sermon, series and speaker the id the website reader gave it (see
  [sermons-import.md](sermons-import.md), *Ids*), so they attach again once the import has run. Production has no
  sermons yet; it gets the import alone. Tests: `AppOnlyMigrationTests`.

### Services (owner, 2026-11-09)

A sermon's service is a choice, not free text: "Sunday evening", "Sun PM" and "Sunday night" were three names for one
service. The **standard services** are *Sunday Life Groups*, *Sunday Morning*, *Sunday Evening* and *Wednesday Night*
(`SermonServices`, Sermons.Contracts, so the app, the admin panel and the server use the same names). "But we need custom
options as well, e.g. during conference week, we have that on weekdays and such": the picker on a sermon and on the upload
form (`ServicePicker`, on Platform.Ui's `SearchPicker`) offers the standard four, then every other service a sermon uses,
the most recent first, and *Add "…" as a service*. A service added once is offered next time because a sermon uses it
(`Library.ListServices`); there's no separate list to keep, and one no sermon uses any more drops out.

- **Spellings land on one name.** What staff type goes through `SermonServices.Named`: a clear spelling of a standard
  one becomes it ("sunday night" → *Sunday Evening*, "Wed" → *Wednesday Night*), and anything else that matches a
  service already used, ignoring case and spaces, takes that service's spelling, so "conference, tuesday NIGHT" joins
  *Conference, Tuesday night*. Tests: `SpeakersAndServicesTests`.

### Speakers: the list staff manage (owner, 2026-11-09)

*Other spellings* and *Or join them with* are gone, with what was behind them (the `speaker_aliases` table and merging):
"remove that and its functionality completely. no need. being superseded by managing the list fully in app." A speaker is
a name and a photo in the list on *Series and speakers*. No two speakers can have the same name (ignoring case and
spaces), and a sermon credited to the wrong person is corrected on the sermon. A new speaker typed on the upload form
lands on the speaker with that name if there is one (`Speakers.ResolveAsync`). A sermon can have no speaker; it shows
*Guest speaker*. A speaker can carry an **honorific** (*Bro*, *Pastor*…; owner, 2026-11-09: stored for later, not shown
yet), which only the import fills in. A speaker with no sermons isn't shown in the app (the speakers feed holds only speakers with sermons)
but stays in the admin panel's list until staff delete it. Tests: `SpeakersAndServicesTests`.

### Deleting a speaker, a series or a sermon (owner, 2026-11-09)

"If I can create something, I should be able to delete it" (AGENTS.md, *Create means delete*). Holders of
`sermons.edit` delete from the admin panel (`Library.DeleteSpeaker`, `DeleteSeries`, `DeleteSermon`; `SermonDeletion`).
Every delete asks first, says what happens, and says it can't be undone.

- **A speaker or a series with sermons:** the dialog says how many and offers one picker, *Where their sermons go*:
  another speaker (series), or *Leave them without a speaker* (*not in a series*). A speaker or series with none just
  goes. Hidden sermons go the same way as the rest.
- **Moving them** puts every sermon credited to it on the one chosen, and series followers follow it (`MoveScopeAsync`).
- **Leaving them:** the sermons show no speaker, or no series. Series followers stop following (`ForgetScopeAsync`).
- **What goes with it:** the speaker's photo or the series' picture (its Media use is released; a picture stays in
  Media like any unused one), its description, and its row in the speakers or series feed, so it leaves every phone
  when it next connects.
- **A sermon, only once it's hidden** (owner, 2026-11-09: "only delete after it's hidden"). Deleting removes the
  sermon, its uploaded audio file in R2 (unless Media keeps it), its picture use and everyone's listening progress on
  it (deleted from the progress feed), and rewrites the podcast feed. Audio Media keeps (every upload, once
  `AudioKeeper` has hashed it, and every imported recording) stays in *Sermon audio* as *Not used*, and holders of
  `media.delete` delete it there (`StaffDeleteUnused`, as fonts are; the same for *Sermon artwork*). Notifications already sent about it open nothing.
- **Custom services need no delete** (owner, 2026-11-09): one drops out of the picker once no sermon uses it
  (*Services*).
- Tests: `DeletingTests` (feeds, followers, pictures; a sermon's audio and progress).

### Uploads

- **Add a sermon** (admin panel, `sermons.upload`): drop an audio file (mp3 or m4a, up to 300 MB), then title,
  speaker (pick or new), series (pick or new), service, the day, the Bible passage, a YouTube link, a line about it, and
  artwork (the series' by default, or a picture from Media's picture sheet). The audio goes straight from the browser to
  R2 through a signed link (`Sermons.BeginAudioUpload`, as Media does), named `sermons/audio/<id>.<ext>`; the server
  checks it arrived, its size and type, and reads its duration.
- **Sermon audio is a Media item (owner, 2026-11-06: "keep an eye on sizes").** Uploaded audio is hashed in the
  background after it's added (`AudioKeeper`) and kept in Media's *Sermon audio* system folder through
  `IMediaSystemFolders.KeepStoredAsync` (registers a file already in the bucket by key and hash, with no bytes in
  memory; a second file with the same hash reuses the first, and the duplicate blob is deleted), and every sermon holds
  a reference (`sermons/sermon/{id}/audio`). Audience `sermons.edit` (and `media.system.view`). Media shows audio with a
  music icon, its size, and a player in the details panel. Tests: `AudioStorageTests`.
- **Pictures** are Media items: a sermon's picture is chosen from Media (`sermons/sermon/{id}`), a series' picture
  (`sermons/series/{id}`) and a speaker's photo (`sermons/speakers/{id}`) likewise. Sermons is an **`IMediaHolder`** for
  every `sermons/…` owner (`SermonPictures`): when Media removes or puts back an original, or makes sizes for a picture
  in use, Sermons re-verifies that record's pictures and republishes it. The *Sermon artwork* system folder holds
  pictures the app keeps itself (the import's), each kept once by its content. In the app, `MediaCover` shows a picture
  whole (`contain`, on the masthead colour) unless it's within a few percent of 16:9, and each screen asks for the size
  it shows (`MediaCover`/`MediaImage` `Size`). **SkiaSharp** reads a picture's size and format where the server needs it
  (MIT; its `NativeAssets.Linux.NoDependencies` runs in the stock .NET container).
- **Audio tooling** (levelling, trimming, intro/outro) is the owner's next idea; it plugs in between the
  upload and publishing, and isn't built.
- **The podcast feed** (owner, 2026-11-04): `podcast/feed.xml` in the public bucket, rewritten whenever a
  sermon changes, with `Cache-Control: public, max-age=300` (the only object there that isn't immutable).
  A static file, so no API path is added (AGENTS.md). The owner said an RSS endpoint would also be acceptable;
  static was chosen because ~3 uploads a week need nothing more. Each episode's `guid` is the sermon's id. Apple
  Podcasts and Spotify take a new feed URL in Podcasts Connect and Spotify for Creators. Apple moves existing
  subscribers when the old feed redirects (301) or carries `itunes:new-feed-url`; whether Tithe.ly can do either is to
  be checked with them. Our feed holds **every** sermon. The admin panel's Sermons page shows its address.
- **Storage:** R2 is $0.015/GB-month with free egress (Cloudflare's price list, checked 2026-11-04). About 3 sermons a
  week at ~50 MB is ~7.8 GB a year; the website's archive the import brings across is about 49 GB (see
  [sermons-import.md](sermons-import.md)).

## The app

Designed with impeccable's full process (see *Design*); the POC's Listen and sermon screens are the
starting point (the team liked them).

- **Listen** (a tab): the latest sermon in the masthead with Listen and Watch; *Keep listening* (started, not
  finished, most recent first); the latest sermons; series; speakers; a search field.
- **A sermon** (`/sermon/{id}`): artwork or the video, title, series and part, Bible passage, speaker, day and service,
  Listen and Watch, how far you got, *Mark as played* / *not played*, Share (later, with the owner's yes),
  *About this sermon*, *More in this series*.
- **A series** (`/series/{id}`): its artwork, its sermons in order, how many you've heard.
- **A speaker** (`/speaker/{id}`): their photo, their sermons, newest first.
- **Search**: titles, series, speakers, services and the day ("anchored", "parry", "christmas", "2026"),
  across everything on the phone, offline.
- **The player** is the top of the Listen tab (owner, 2026-11-09): what is playing, or was last playing, with its
  cover and title (tap to open the sermon), a scrubber, back 15 and forward 30 seconds, play/pause and the speed
  (0.8–2×, tap to change); *Up next* (the next in the series) plays on in the background. Until something has been
  played, the top is the latest sermon (or the one to keep going with) with Listen and Watch. The Sermons tab's icon is
  Pause while playing and Play otherwise (paused, or nothing loaded); tapping it still opens Sermons, and when Sermons
  isn't one of the chosen tabs nothing else shows the state. There is no mini-player (below).
  There is no sleep timer: the owner doesn't want the app to encourage falling asleep to a sermon
  (2026-11-06).
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
  an alarm) pause and resume when iOS says to; unplugging headphones or AirPods pauses. Up-next runs natively, so it
  works in the background.
- **Bridge contract** (`IAudioPlayer`, Platform.Client; platform-neutral): `LoadAsync(item, startAt, play)`,
  `Play`, `Pause`, `SeekTo`, `SkipBy`, `SetRate`, `SetUpNext(items)`, `MarkAsync(id, finished |
  watched | cleared)`, `JournalAsync()`, and a `Changed` event with the state (item, playing, buffering,
  position, duration, rate, ended). An item is an id, a URL, a title, an artist, an album and an
  artwork URL. **Android** is stubbed; the browser (the admin panel) uses an HTML audio element behind the same
  interface.
- **Downloads (owner, 2026-11-05):** *Download for when you're offline* on a sermon fetches the audio with a background
  `URLSession` (it carries on with the app closed; the AppDelegate hands iOS's completion to `AudioDownloads`), and the
  player plays the file whenever it's there. Files go to the app's **Documents/Sermons** folder under the sermon's title
  ("Anchored In Hope.mp3"), with file sharing on (`UIFileSharingEnabled`, `LSSupportsOpeningDocumentsInPlace`), so
  iOS's Settings → iPhone Storage → the app lists them (the owner asked for "what they are, not just app storage") and
  the Files app shows them under On My iPhone → Good Shepherd → Sermons; nothing else is kept in Documents. They are
  excluded from iCloud backup (they can be downloaded again). A file deleted from Settings or Files simply stops counting
  as downloaded (`DownloadShelf`, tests in `DownloadShelfTests`). The app shows them as a *Downloaded* shelf on the tab,
  a mark on rows, and *Downloaded sermons* in More → Storage with sizes, Remove and Remove all (a feature adds a
  Storage section through `AddStoragePart`). `IAudioDownloads` (Platform.Client) is the bridge; the browser has none.
  Apple Music's grouped breakdown in Settings isn't open to other apps; the Documents list is what's possible.

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
- **Following (owner, 2026-11-05: "opt in … even for specific series"):** *Tell me when a new sermon is out* on the
  Sermons tab, and *Follow this series* on a series. Built on Access, like following a ministry: a following phone (no
  sign-in needed) or person holds a **Sermon follower** role (`sermons.new.receive`) or a **Series follower** role at the
  series (`sermons.series.receive`, a new scope kind `sermon-series` in Platform.Permissions); both are phone-holdable,
  and signing in moves a phone's follows to the person as for every follow. `Sermons.Follow` and `Sermons.Follows` are
  `[AppInstall]`. When a sermon first appears (preached in the last three days, never on the first read, once) the
  announcer raises `sermons.new` to holders of the first and `sermons.series.new` to holders of the second at that
  series. They are opt-in in the true sense: only holders get them, inbox included. A signed-out phone's inbox shows
  only what it follows: the public rows carry a scope (`sermons`, or the series' id) and Notifications' inbox now
  includes rows whose scope a feature says this phone follows (`IFollowedScopes`, Platform.Client; one filter line in
  Notifications.Client). The two are raised as one (`RaiseOneOfAsync`, series first): someone following both all
  sermons and a series hears once, as "New in <series>: …" (owner, 2026-11-08: two for one sermon is a double up; see
  notifications.md, *One notification per thing*). Roles with every permission (the Owner's `*`) hold these too, so the
  Owner hears of every sermon, once. A sermon is announced once, when it first appears: giving it a series later doesn't
  announce it again (`AnnouncedAt`).

## Permissions

| Node | Scope | Means |
|---|---|---|
| `sermons.edit` | Global | Correct sermons, manage series and speakers, delete them |
| `sermons.upload` | Global | Add sermons by uploading their audio |
| `sermons.new.receive` | Global | Hear when a new sermon is out (phone-holdable) |
| `sermons.series.receive` | Series | Hear when a series has a new sermon (phone-holdable) |

`sermons.import` belongs to the import module ([sermons-import.md](sermons-import.md)).

## Tests

Real rules only: the merge rule's laws, progress saved through the service reaching the person's feed and nobody
else's, the catalogue window, a sermon only once in the notification, a typed speaker landing on the one with that name,
services offered and spelled one way, uploaded audio stored once by its content and held in Media, deletes moving or
releasing what used the deleted thing, the podcast feed, and the app-only migration wiping the website's sermons
(`AppOnlyMigrationTests`).

## Decided while building

- **The phone (owner, 2026-11-05):** the tab is *Sermons*, beside Home; it opens on the sermon you were part-way
  through if you listened in the past week (`KeepGoing`), otherwise the newest; **audio first** (Listen leads, Watch
  is beside it when there's a video). Layout: *Shelves*, chosen on the decision page over a dated timeline with series
  bands and three segmented views (`.impeccable/mocks/decision/sermons/`; direction contract in
  `.impeccable/surfaces/`).
- **The admin panel (owner, 2026-11-05):** the AV team uploads (first pass), the office checks (second pass); an upload
  is on phones at once (later, with audio tooling, it will wait for a check: a held state and a publish permission).
  Layout: the *review queue* (Needs a look beside the sermon being checked, *Looks right · next*), chosen over an upload
  workbench and a two-pass board (`.impeccable/mocks/decision/sermons-admin/`).
- **Needs a look** is a sermon nobody has checked since it was uploaded, with a reason when its title suggests a series
  it isn't in, or its speaker is new. Imported sermons count as checked, so staff don't start with hundreds.
- **Titles in a series** drop the series' name where the series line already says it ("Anchored: Part 4 - Anchored In
  Hope" shows as "Anchored In Hope" under "Anchored · Part 4"); elsewhere the full title shows.
- **No mini-player (owner, 2026-11-09):** the bar above the tab bar and its player sheet were removed ("don't like how
  it works or functions"), with their swipe-away and *Hide the player*. The controls are Platform.Ui's `AudioControls`
  (generic over `IAudioPlayer`) on the Listen tab's masthead; the tab icon is a Platform.Ui `ILiveTabIcon` that
  Sermons.App registers for its route (`SermonsTabIcon`), so the tab bar knows nothing about audio. The native player
  restores the last item at launch, so "what was playing" survives the app being closed. Every `Sheet` can be pulled
  down by its top like a native sheet: it follows the finger and closes past a third of its height or on a quick
  flick, otherwise it springs back.
- **The admin panel plays audio** with the browser's own audio element on the sermon being checked.
- **Uploading** goes from the browser's file straight to R2 (an XHR with progress), not through WebAssembly memory;
  the browser reads an m4a's length, the server an mp3's.
- **An architecture rule** (`NoTwoPagesInTheAppClaimTheSameRoute`) now fails when two app pages claim one route: the
  coming-soon page still claiming `/sermons` stopped the app starting.

## What the owner sets up

Before sermons are used for real:

1. **The media host's cache rule** (Cloudflare → the zone → Caching → Cache Rules) caches everything on
   `<env>-media.baptist.com.au` for a long time, which is right for the content-addressed files but not for
   `podcast/feed.xml`, which changes. Add a rule above it: *URI Path equals `/podcast/feed.xml`* → *Eligible for cache*,
   *Edge TTL: use cache-control header if present* and *Browser TTL: respect origin* (the object carries
   `max-age=300`). Then check through the public host: `curl -sD - -o /dev/null https://<env>-media.baptist.com.au/podcast/feed.xml`
   should show `cache-control: public, max-age=300`.
2. **CORS on the public bucket** must allow `PUT` from the admin panel's host (`https://<env>-app.baptist.com.au`), as
   it already does for Media's uploads to the private bucket; uploads go to the public one.
3. **Moving the podcast:** in Podcasts Connect and Spotify for Creators, change the feed URL to the one shown on the
   Sermons page; ask Tithe.ly whether the old feed can redirect or carry `itunes:new-feed-url`, so subscribers move
   with it.
4. **Optional:** `Sermons:Podcast:ImageUrl` (the 3000 × 3000 podcast cover) and `Sermons:Podcast:Email` in the API's
   configuration; they aren't secrets.
5. **Bringing the website's sermons across:** run *Import from the website* once on each environment
   ([sermons-import.md](sermons-import.md)).

## Status (2026-11-09)

Built: uploads to R2 with a podcast feed, the review queue, Series and speakers, deletes, places for a sermon, a series
and a speaker (tests: `Sermons.Api.Tests`); the phone's Sermons tab, sermon, series, speaker, search and all-sermons
pages, the Home tile, the native player (iOS) with the playback journal (tests: `PlaybackJournalTests`), listening
history merged and sent through the outbox; YouTube inline through the static embed page. Checked in the iOS Simulator
and on `dev-app.baptist.com.au`; installed on the owner's iPhone for the real-phone checks.

## Open

- The real-phone checks (background audio with the screen locked, switching tabs, closing and reopening, a second
  device signed in, picture-in-picture), by the owner.
- A held-for-review step for uploads, with audio tooling.
- Sharing a sermon from the phone (needs the owner's yes as phone UI, destinations.md).
