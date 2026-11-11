# Offline first, without caching problems

The app works with no signal (requirement E), and nobody should ever need to clear a cache or wait
for one to expire. The way to have no caching problems is to have no caches that can be wrong. This
builds on SPEC §5.

## One route for data: sync

- **Records reach the phone only through Sync,** into the native store. There is no HTTP cache, no
  service worker, no ETag and no "cache for 5 minutes" anywhere for data.
- **Freshness is a cursor, not a timer.** The server owns what each feed holds (its retention
  window), and every change, including a deletion or a record leaving the window, is sent. A phone
  is either up to date or knows exactly where it stopped.
- **Screens read the store, never the network.** A screen renders what is stored at once and
  re-renders when Sync says its feed changed. There is no fetch-then-show path to go stale or fail.
  **This is a hard rule** (owner, 2026-11-10, after a Life Group opened as a blank "can't load" page while the
  server restarted; see AGENTS.md, *HARD RULE: the app reads data only through Sync*). Code that ships in the app
  makes no direct call to the server except an owner-approved exception listed there: no gRPC proxy, no call
  invoker, no raw native call. Sync and the outbox worker are the only transport. `Architecture.Tests`
  (`SyncOnlyRules`) enforces it. Prefetching is just Sync: everything a person may see is synced in the background.
- **Shared screens, two sources.** A `*.Client` component the admin panel also hosts depends on an interface (for
  example `IMinistryReads` and `IMinistryWrites`): the phone fills it from the store and the outbox
  (`StoredMinistries`, `QueuedMinistryWrites`), the admin panel online (`OnlineMinistries`, in the feature's Admin
  project). `IWrites` (Platform.Client) does the same for single writes: queued on the phone, direct in the admin
  panel. Client code only the admin panel uses is `[AdminOnly]`, and app code may never reach it.
- **Views built from feeds match the server's.** Where a page combines several feeds with the viewer's permissions
  (a ministry's page, the directory, Life Groups), the phone builds it from its store and its own grants, and a test
  checks the result equals what the server's online method returns for the same person.
- **Writes go to the outbox** and show as pending until the server confirms them (SPEC §5;
  `WaitingToSend`, Platform.Ui, shows a screen's own waiting and failed changes with *Try again* and *Discard*).
  Anything a write creates gets its id on the phone, and the server treats a repeated id as already done.
  **Quiet entries** (built 2026-11-03 for [analytics](../features/analytics.md)) are the exception: usage
  and error reports never show in Storage, never raise "couldn't send", and are deleted silently when
  they give up (three days). `IOutbox.EnqueueQuietlyAsync`, and `DiscardQuietAsync` when someone turns
  "Help improve the app" off.
- **Gone means gone.** A feed the app no longer registers is dropped; a format change resets a feed.
- **A cursor only means something to the database that issued it.** Each database has a **sync
  origin**, a random id made once by the migration that creates `sync.origin` (built 2026-11-06,
  after a phone that had synced against three dev databases showed every sermon series three times).
  The phone sends the origin its cursors came from with every `Pull` and `Watch`, and every reply
  carries the server's. When they differ, the server resets every feed the phone asked for, known or
  not: its rows are dropped and the feed re-reads from 0. The phone stores the new origin with the
  reply that resets it. A phone that has never stored one sends `unknown`, so a phone from before
  origins starts again once. A client that sends no origin at all (an old build) is not reset by it.
- **A cursor ahead of the server resets too.** If nothing is newer than a phone's cursor and the
  cursor is past both the feed's newest row and its horizon, that feed resets. This catches a
  database restored from its own backup, which keeps its origin but goes back in time, as long as the
  phone syncs before the restored database has written past the phone's cursor. After that, cursor
  numbers are reused for different changes and nothing can tell, so **restoring a backup also gives
  the database a new origin**: `UPDATE sync.origin SET id = gen_random_uuid();` as part of the
  restore. `tools/dev/copy-main-db.sh` does the same for a worktree's copy of the main database.
- **The sync stream is the phone's connection.** Its state (live, catching up, offline) is the one
  signal for "online", and a retry while offline still reads as offline, so nothing flickers.

## Screens update themselves

Nothing a component shows may be the answer from when it opened. Every change the client knows
about goes through one hub, `ClientSignals` (Platform.Client): a feed changed (and which), the phone
reconnected or went offline (settled, not every retry), someone signed in or out, permissions
changed, the outbox changed. Components say what they depend on and the platform does the rest:

- **`LiveData<T>`** (Platform.Ui) is the default for anything loaded: it takes the load, the feeds it
  reads and the kinds of change that matter (by default reconnecting, the session and access), and
  reloads by itself, coalescing bursts. Offline it keeps the last answer and marks it stale; with no
  answer yet it shows a failure that retries by itself once back online. With `Remember` (a key, such
  as `CalendarDiary.Remembered`) a screen opened again draws its last answer at once and reloads in
  place, so moving between screens never flashes a skeleton (`RememberedValues`, Platform.Client). The
  remembered answers are forgotten whenever someone signs in or out or their access changes.
- **`RefreshOn`** gives an imperative screen (an editor holding drafts) the same triggers.
- Services keep their own `Changed` events (`ISurfaceLayout`, `IAccess`); a component may subscribe
  to those directly.

`Architecture.Tests` (`LiveDataRules`) fails on a component that injects a data source and loads on
start without one of these. Every component follows it; the rule's exemption list (`NotYetMigrated`) is
empty and stays that way.

## Editing offline

Staff editing on the phone follows the same rule (built 2026-11-10 for [events](../features/events.md), *Editing on
the phone through Sync*): a feed for editors holds what the editors need, writes queue in the outbox with a
`gsbc-request-id` the server treats as done when it repeats (`QueuedRequest`, Platform.Api), new records get their id
on the phone, and each save carries what the editor started from so the server changes only what that person changed
(different things both stay; the same thing: the later save wins). A screen shows a waiting or failed save with
`SaveStatus` (*Waiting to send*, *Couldn't save* and *Try now*).

## Files in the outbox

Built 2026-11-11 (owner: "uploads can work offline? keep it and send it when online?"). A picture or document chosen
on the phone is kept on the phone and sent by the outbox with the save that uses it, offline or not. Screens never
call the upload methods themselves (AGENTS.md, *Uploads are not an exception*).

- **Choosing keeps the file.** `FilePick` (Platform.Ui) reads the chosen file into the native layer's upload shelf
  (`Application Support/DataLayer/Uploads`, not a cache the OS may clear), hashes it there (SHA-256) and hands back a
  placeholder `MediaRef`: no id, its `Url` the file on the phone, so `MediaImage` shows it at once. `FileUploads`
  (Platform.Client) is the one service features use.
- **A save waits for its file.** The feature queues the save with `IOutbox.EnqueueAsync(method, bytes, uploads)`,
  where each upload is `FileUploads.For(placeholder, slot, target)`. The slot says where the media id goes in the
  save (`MediaSlot.Ref(...)` for a `MediaRef`'s id, `MediaSlot.Id(...)` for a string), by protobuf field numbers;
  the target says who may see it and which feature area it's for (Media, *Uploads from features*). The upload is an
  outbox entry of its own; the save isn't sent until it's done.
- **The native worker does the upload** (`Outbox.swift`): the duplicate check (`Media.FindByHash`), then
  `BeginUpload`, the PUT to the signed address and `CompleteUpload`, one stage at a time and remembered, so a closed
  app or a lost reply resumes where it was. A file the library already has is used rather than sent again. A
  completion whose reply was lost goes back to the duplicate check, which finds what it made. The media id is then
  added to the end of each waiting save's bytes at its slot. Protobuf merges a repeated field, so the native side
  needs no knowledge of the message (`StoredEditingTests`, `QueuedFileTests` hold protobuf-net to it).
- **Pending, sending, couldn't send.** A waiting save shows as waiting like any other (`SaveStatus`,
  `WaitingToSend`). If the file can't be sent (refused, or three days without success), its saves fail with the
  reason, and **Try again** on the save tries the file too. More → Storage lists files waiting to go.
- **Discarding drops both.** Discarding a file discards the saves waiting for it. Discarding a save discards its file
  unless another save waits for it. A record deleted while its picture still waits drops those saves and the file;
  if the record was only ever made in the queue, the delete isn't sent either (Events).
- **Files are cleaned up.** A file leaves the shelf once every save that used it is sent or discarded. Files chosen
  but never saved are cleared the next time the app starts.
- **Two saves of one file share one upload.**
- **Not yet:** a background transfer session for the PUT (iOS `URLSession` background uploads). Today the PUT runs
  while the app or a background sync wakes it, and resumes from the stage it reached. Android's data layer is still a
  stub, so none of this exists there yet.

## Personal and private data

- Feeds have an audience ([permissions.md](permissions.md)). Private feeds (a ministry's members-only
  news, your own prayer requests, your permissions) are stored for the signed-in user only and
  cleared on sign-out.
- When someone's permissions change, the affected feeds reset for them, so nothing they can no
  longer see stays on the phone. A feed whose rows each carry their own audience (`FeedAudience.EachRow`) resets on
  any change to the person's grants (2026-11-10; it used to watch only role membership, so leaving a ministry team
  left its team posts on the phone).

## Configuration is data, so it syncs too

What staff configure ([data-driven.md](data-driven.md)) arrives as feeds, so the app's tiles, ministry
modules and labels work offline exactly like content.

## Media: never stale, because it never changes

Images and files are outside the store (SPEC §5). They can be cached forever without risk because a
file's address is its content:

- **Content-addressed.** A file's key is the SHA-256 of its bytes. Editing an image makes a new key,
  and the record points at the new one. A key's bytes never change.
- **Cached once, everywhere.** `Cache-Control: public, max-age=31536000, immutable`. Cloudflare and
  the phone's file cache keep it forever, and nothing is ever purged.
- **The phone's file cache** is keyed by hash, size-limited and least-recently-used, so it can never
  show an old version.
- **Prefetched for offline:** a feed can name the images its records show, and the app fetches them
  ahead of time within the cache's limits ([media.md](../features/media.md), *Prefetching*).

**Decided (owner, 2026-11-02): media lives in R2.** Public media is served from an R2 bucket on a
custom domain (e.g. `media.baptist.com.au`) behind Cloudflare and fetched by the phone directly;
private media and uploads go through short-lived signed R2 URLs handed out by gRPC methods
(`Media.GetLink`, `Media.BeginUpload`). Neither adds an endpoint to our hosts, since the bytes go
between the device and R2 and our API only hands out links over gRPC. A gRPC `Media` service
streaming the bytes was the alternative, turned down because Cloudflare can't cache it.

Built 2026-11-02; see [media.md](../features/media.md). Keys are `<sha256>.<extension>`, so
Cloudflare caches them by default; each object is copied into place with the `immutable` header above.
Private media is linked only while the caller holds its audience, checked on every link (owner,
2026-11-02). The buckets, the custom domain, a cache rule, CORS and a lifecycle rule are the owner's
to set up (media.md lists them): done for UAT, and for production when it exists.

## Where Cloudflare caches, and where it doesn't

| What | Cached | Why it can't go stale |
|---|---|---|
| gRPC (all data) | Never: they're POSTs | Sync is the cache |
| Public media | Forever | Content-addressed |
| Private media | Never: short-lived signed links | Content-addressed, and the phone checks the hash |
| Admin panel `.wasm`/`.dll` and other fingerprinted files | Forever | .NET 10 fingerprints file names |
| Admin panel `index.html`, client routes and unfingerprinted files (`blazor.webassembly.js`, `dotnet.js`, the stylesheets) | No (`no-cache`, revalidated) | They name the fingerprinted files |
| The app itself | Not served from the web | It ships in the store build |

The admin panel is at `<env>-app.baptist.com.au`, and in production at `app.baptist.com.au` ([environments.md](environments.md)).

### Cloudflare overrides our headers

Learned the hard way (2026-11-01): Cloudflare's zone defaults replace a shorter origin cache header with
its own **4-hour browser cache** (`max-age=14400`, even on 404s) and cache `.js`/`.css` at the edge. A
file fixed on the server can stay broken in a browser for hours. So:

- **Dev** (`dev-app.baptist.com.au`) sends `Cache-Control: no-store` on every response, which Cloudflare
  respects. `no-store` doesn't evict what the edge already holds, so after changing what's cached,
  purge the host (Caching → Configuration → Custom Purge → Hostname).
- **UAT and production** rely on fingerprinted names (`immutable`, safe to cache forever) and send
  `no-cache` on everything else, `index.html` included. The admin page links its scripts and styles
  unfingerprinted, because a hosted publish leaves `#[.{fingerprint}]` placeholders unrewritten
  ([environments.md](environments.md), *Caching*).

How the `baptist.com.au` zone is set (owner, 2026-11-02, checked through the public hostnames):

- **Browser Cache TTL: *Respect existing headers*.** Zone-wide, so the church website's own headers
  apply to it too. Before this, the POC's unfingerprinted `.js` and `.css` went out with `max-age=14400`.
- **Web Analytics: disabled** for the whole zone. Its beacon was injected into every HTML page
  (only for browser user agents, so a plain `curl` doesn't show it) and nobody used it.
- **Purged** `dev.` and `app.` once, after the two changes above.
- **No cache rules.** None are needed: the edge respects `no-store`, and fingerprinted files cache
  on their own `immutable` header. The media hosts have one (above).
- **Nothing may rewrite our HTML or scripts:** Rocket Loader and Email Obfuscation must stay off,
  since they would break the integrity hashes Blazor checks. Neither had touched our pages when
  checked.
- **Every web deploy is checked through the public hostname**, not localhost:
  `tools/dev-proxy/check-caching.sh [site]` reads `cache-control` and `cf-cache-status` as Cloudflare
  serves them and fails on anything unexpected. A 200 from curl doesn't prove a browser isn't holding
  an earlier response.

## Dropbox

Files that stay in the church's Dropbox (music recordings) are fetched through Dropbox temporary
links (valid for hours) that a gRPC method returns. To keep one offline, the person saves it, and
it goes into the file cache like any other media.

## The admin panel

Online only, with no store. Its pages stay current the way the phone's do (`ClientSignals`, `LiveData`,
`RefreshOn`), fed by the same `Sync.Watch` stream over gRPC-Web (built 2026-11-03), so staff see each
other's changes within a second or two.

- **One stream, generic.** `FeedWatch` (Platform.Client, registered by `AddGrpcWebTransport`) holds one
  `Watch` call for every feed the open pages name. `LiveData` and `RefreshOn` follow the feeds they're
  given, so naming a feed is all a page does; no feature wires anything up.
- **Signals, not data.** The browser keeps a cursor per feed and the sync origin, nothing else, so a
  server moved to another database resets its feeds as a phone's would. A change becomes a feed
  change in `ClientSignals`, and the page reloads through its own gRPC methods.
- **It starts at now.** A feed the browser hasn't watched yet is asked for with `FeedCursor.Latest`, so
  no history is replayed. When the stream is up those feeds get one reload, which closes the gap between
  a page's first load and the stream starting.
- **Admin records need a feed too.** Records that never go to phones get a feed of *markers*
  (`FeedChangeRecord.Marker`: no payload, just which record changed): `access.roles` (any change to
  roles or who holds them; audience `access.view`), `media.library` (an item added, deleted, or used
  or no longer used; `media.view`) and `accounts.people` (written with every audit entry;
  `accounts.view`). The audience is checked per row on the server like every feed, so nobody hears of a
  change they couldn't see. A new admin screen showing records phones don't sync adds one.
- **Your own access** follows `access.grants`, so a role given or taken away by someone else changes
  the sidebar and the pages at once.
- **Reconnects** resume from the cursors, so nothing is missed. A dropped stream is retried at once,
  then with backoff and jitter up to 30 seconds; the panel only counts as offline after the second
  failure, and coming back online reloads every page.
- **A tab in the background:** a stream with no heartbeat for 75 seconds is treated as dead. Coming back
  to the tab, or the network coming back, reconnects at once (`feed-watch.js`).
- **A session that ends** (signed out everywhere, ended from another device, expired): the server checks
  a stream's sign-in each time it wakes, on every change and every heartbeat, and ends the stream with
  `Unauthenticated` once the session is gone (`ISignedInSessions`, answered by Accounts). The browser
  reconnects without it, its private feeds reset, and the panel asks to sign in. Phones' streams get the
  same check.
- **Editing something that changes underneath you:** see [drafts.md](drafts.md), *Live in the admin
  panel*.
