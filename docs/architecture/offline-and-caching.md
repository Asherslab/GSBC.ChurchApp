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
- **Writes go to the outbox** and show as pending until the server confirms them (SPEC §5).
- **Gone means gone.** A feed the app no longer registers is dropped; a format change resets a feed.

## Personal and private data

- Feeds have an audience ([permissions.md](permissions.md)). Private feeds (a ministry's members-only
  news, your own prayer requests, your permissions) are stored for the signed-in user only and
  cleared on sign-out.
- When someone's permissions change, the affected feeds reset for them, so nothing they can no
  longer see stays on the phone.

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
to set up (media.md lists them).

## Where Cloudflare caches, and where it doesn't

| What | Cached | Why it can't go stale |
|---|---|---|
| gRPC (all data) | Never: they're POSTs | Sync is the cache |
| Public media | Forever | Content-addressed |
| Private media | Never: short-lived signed links | Content-addressed, and the phone checks the hash |
| Admin panel `.wasm`/`.dll`/JS/CSS | Forever | .NET 10 fingerprints file names |
| Admin panel `index.html` and unfingerprinted files | No (`no-cache`) | `index.html` names the fingerprinted files |
| The app itself | Not served from the web | It ships in the store build |

The admin panel will be at `app.baptist.com.au`, which serves the frozen POC's web build until then.

### Cloudflare overrides our headers

Learned the hard way (2026-11-01): Cloudflare's zone defaults replace a shorter origin cache header with
its own **4-hour browser cache** (`max-age=14400`, even on 404s) and cache `.js`/`.css` at the edge. A
file fixed on the server can stay broken in a browser for hours. So:

- **Dev** (`dev.baptist.com.au`) sends `Cache-Control: no-store` on every response, which Cloudflare
  respects. `no-store` doesn't evict what the edge already holds, so after changing what's cached,
  purge the host (Caching → Configuration → Custom Purge → Hostname).
- **Production** relies on fingerprinted names (`immutable`, safe to cache forever) and `index.html`
  never cached.

How the `baptist.com.au` zone is set (owner, 2026-11-02, checked through the public hostnames):

- **Browser Cache TTL: *Respect existing headers*.** Zone-wide, so the church website's own headers
  apply to it too. Before this, the POC's unfingerprinted `.js` and `.css` went out with `max-age=14400`.
- **Web Analytics: disabled** for the whole zone. Its beacon was injected into every HTML page
  (only for browser user agents, so a plain `curl` doesn't show it) and nobody used it.
- **Purged** `dev.` and `app.` once, after the two changes above.
- **No cache rules.** None are needed: the edge respects `no-store`, and fingerprinted files cache
  on their own `immutable` header. The media host will need one (above).
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

Online only. It keeps state in memory, fed by the same `Watch` stream (SPEC §5), so staff see each
other's changes live.
