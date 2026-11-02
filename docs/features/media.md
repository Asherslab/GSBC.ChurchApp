# Media

Images and files (sermon artwork, leader photos, Life Group notes): how they're uploaded, named,
served, cached on the phone and kept private. The route was decided by the owner on 2026-11-02 (R2,
signed links from gRPC; [offline-and-caching.md](../architecture/offline-and-caching.md)). Built
2026-11-02.

## What people can do (v1)

| Who | Can |
|---|---|
| `media.upload` | Upload an image (JPEG, PNG, WebP, GIF) or a PDF, public or private to an audience they belong to |
| `media.view` | See the media library: every uploaded file, newest first, and where each is used |
| `media.delete` | Delete a file nothing uses any more |
| Anyone signed in | Get a link to a private file, **if they hold its audience right now** |
| Anyone | See public media: it's a plain URL on the media host |

The **Media editor** system role holds `media.*`. The library page is in the admin panel; the upload
and library components live in Media.Client, so a phone page is a page change, not a rewrite.

## How an upload works

No endpoint is added to our hosts: bytes go between the device and the bucket, and our API only hands
out links over gRPC (AGENTS.md's hard rule).

1. `Media.BeginUpload` (gRPC) checks the type, and for a private file that the uploader holds its
   audience, then records a pending upload and returns a **presigned PUT** (15 minutes) to a staging
   key `uploads/<id>` in the private bucket.
2. The browser or phone PUTs the bytes straight to R2 with that content type.
3. `Media.CompleteUpload` (gRPC) reads the staged object, **hashes it and checks its first bytes match
   its claimed type** (an HTML page sent as `image/png` is refused), copies it to its final key with
   `Cache-Control: public, max-age=31536000, immutable`, deletes the staged copy, and returns a
   `MediaRef`. An image's **width and height are read from its bytes** (PNG, GIF, WebP and JPEG,
   turned by the JPEG's EXIF orientation so a sideways phone photo gets its displayed shape), so
   placeholders know their shape. The client no longer sends them.

Abandoned uploads: the pending row is pruned after a day, and the staged object by the bucket's
lifecycle rule (below).

**Size:** no cap (owner, 2026-11-02). The upload component warns over 10 MB that everyone who sees it
downloads all of it, and offers "Upload anyway". No resizing for now; review if big photos become a
problem.

## Names: a file's name is its content

The key is `<sha256 of the bytes>.<extension>`, computed by the server, never the client. New bytes
get a new key, the same bytes always get the same key (re-uploads collapse), and a key's bytes never
change, so everything can be cached forever and nothing is ever purged. The extension comes from the
content type, so Cloudflare caches the media host by default.

## What syncs

There is no media feed. A feature's record carries a `MediaRef` (Platform.Contracts: id, key, type,
size, dimensions, alt text, file name, private or not, and the URL for public media) **inside its own
payload**, so it reaches the phone through that feature's feed, with that feed's audience. The media
library itself is staff-only and online-only (`Media.List`).

## Saving a `MediaRef`: the server's, never the client's

A feature that stores a `MediaRef` (or rich text, whose images are `MediaRef`s) calls
`IMediaReferences` (Platform.Api, implemented by Media.Api) on every save:

1. **Before saving,** `VerifyAsync(refs)` returns the library's own `MediaRef` for each: key, type,
   size, dimensions, privacy and URL all come from the media table. An unknown id, or a key that isn't
   that id's, is refused (`InvalidArgument`). Only the alt text may be the feature's own, so the same
   photo can be described differently in two places.
2. **After the save commits,** `UseAsync(owner, refs)` records which files that record uses (the
   owner is a string such as `config.placement/<id>`), replacing what it used before. A record that's
   deleted passes no refs.

Uses are recorded after the commit so that a refused or conflicting save never leaves a phantom use
behind. The cost is a small window: a crash between the commit and `UseAsync` leaves a file looking
unused. A file deleted between the two makes `UseAsync` fail with "choose it again".

Configuration does this for its media and rich-text settings; settings are public, so a setting's
image must be public too.

## Deleting media (built 2026-11-02)

`Media.Delete` (`media.delete`) refuses a file that any record uses ("still used in 2 places"); the
library shows "Used in N places" in place of the Delete button. A foreign key from `media.uses` to
`media.items` closes the race with a save. The bytes are removed from the bucket only when no other
library entry has the same content (re-uploads share a key).

What deleting can't do: a deleted public file may still be at Cloudflare's edge, under its
`immutable` header, until the edge evicts it, and it stays on phones that cached it until their cache
evicts it. Nothing references it by then, so nobody is shown it. Purging the edge would need a
Cloudflare API token and isn't built.

## Private media (owner, 2026-11-02: check on every link)

- A private item is stored in the private bucket and records its **audience**: a permission and
  optional scope (for example `ministries.files.read` in ministry 42), set by the feature that uploads
  it. Nobody can make a file private to an audience they aren't in.
- `Media.GetLink` checks the caller's grants **on every call** and returns a signed GET valid for
  5 minutes. Someone removed from a ministry is refused from their next request; keys never rotate.
- On the phone, private files are cached apart from public ones and dropped whenever a feed resets
  (which is what a permission change does). `PrivateMediaGuard` also drops them when the session
  changes, but only in hosts that run client startup tasks; the app doesn't yet (sign-in on the phone
  waits for the Apple enrolment), so wire it in when it does.
- Once a file is on someone's phone, or they've saved it, we can't take it back. Short links and
  dropping the cache are the most a server can do.

## The phone's cache

`MediaImage` (Platform.Ui) asks `IMediaFiles` for a source. In the app, the native data layer's
`MediaCache` (Swift) returns the cached file, or downloads it once, **checks the bytes hash to the key**
(so it can never show the wrong or a stale file), and serves it through Capacitor's file URL. It's
keyed by hash, capped at 200 MB and evicts the least recently used. In a browser, `IMediaFiles`
returns the URL (public) or a signed link (private), and the browser's cache does the rest.

### Prefetching, so images are there offline (built 2026-11-02)

A feature registers a `MediaPrefetch` rule in its app-side registration: which of its feed's records
(a `FeedPageQuery`, usually its newest) and how to find their `MediaRef`s. Whenever that feed changes,
and when the app starts, `MediaPrefetcher` (Platform.Client) collects the images and asks the native
cache to fetch the ones it lacks. Private images are included while the person can get a link to them.

The cache enforces the limits (`MediaCache.prefetch`):

- a file over **5 MB** is skipped (it's fetched when first shown instead);
- one prefetch fetches at most **50 MB**, newest records first, so the 200 MB cache never evicts what
  it just prefetched;
- a file already cached is never fetched again, and one that turns out larger than declared is dropped;
- prefetching uses a session that respects iOS **Low Data Mode**.

Decided while building it: the rule runs in the app, not in the native sync engine. That needs no new
column on `sync.feed_changes` and no change to the wire format, and it can prefetch private images. The
cost is that records arriving by **background** sync have their images prefetched the next time the app
is opened online, not in the background. Hub tile pictures (the placements feed) are the first rule.
In a browser, prefetching does nothing.

The admin panel registers `MediaLibrary` as the `MediaPicker`, so "choose an image" in rich text and in
settings opens the library in pick mode, uploading included.

## Configuration

`Storage:Blobs` (Platform.Storage), the same keys in every environment:

| Key | Production (R2) | Development |
|---|---|---|
| `ServiceUrl` | `https://<account-id>.r2.cloudflarestorage.com` | Set by the AppHost: SeaweedFS on `http://localhost:5310` |
| `PublicServiceUrl` | Not set: signed links use `ServiceUrl` | `https://dev-media.baptist.com.au` when the dev tunnel serves it, else `ServiceUrl` |
| `Region` | `auto` (the default) | `auto` |
| `AccessKeyId`, `SecretAccessKey` | The R2 API token's (cluster secret) | Random per AppHost run |
| `PublicBucket`, `PrivateBucket` | The two bucket names | `gsbc-media`, `gsbc-private` |
| `PublicBaseUrl` | `https://media.baptist.com.au` | `<PublicServiceUrl>/gsbc-media` |
| `CreateBuckets` | Never set | `true`: the API creates the buckets |

As environment variables: `Storage__Blobs__ServiceUrl` and so on. In development the AppHost runs
SeaweedFS (MinIO no longer publishes images) unless `Storage:Blobs` is set in the AppHost's user
secrets, in which case it passes those to the API instead.

Signed links go to browsers and phones, so they carry `PublicServiceUrl`, while the API reaches the
store at `ServiceUrl`. In development the admin panel is on `https://dev.baptist.com.au`, and a browser
refuses an upload from that page to `http://localhost` (mixed content), and a phone can't reach
localhost at all. So the dev tunnel also serves `dev-media.baptist.com.au` → SeaweedFS
([tools/dev-proxy](../../tools/dev-proxy/README.md)), and the AppHost signs links for it whenever
`~/.cloudflared/config.yml` names that hostname. It's `dev-media`, not `media.dev`: Cloudflare's
free certificate covers one level of subdomain only.

## What the owner sets up in Cloudflare (not done yet)

1. **Two R2 buckets**, for example `gsbc-media` (public) and `gsbc-private`. Leave the `r2.dev`
   public URL off on both.
2. **A custom domain on the public bucket:** `media.baptist.com.au` (bucket → Settings → Custom
   Domains). Nothing on the private bucket.
3. **A cache rule** for `media.baptist.com.au`: *Eligible for cache*, edge TTL *use cache-control
   header if present*. Recommended, not strictly needed: our extensions (jpg, png, webp, gif, pdf)
   are cached by default, but the rule keeps it true for any type added later.
4. **CORS on the private bucket**, for uploads: methods `PUT`; header `content-type`; origins
   `https://app.baptist.com.au`, `https://dev.baptist.com.au`, `capacitor://localhost` (the iOS
   app) and `http://localhost:5300` (the local admin panel). The public bucket needs none.
5. **A lifecycle rule on the private bucket:** delete objects with prefix `uploads/` after 1 day.
6. **An R2 API token**, *Object Read & Write*, limited to the two buckets. Its access key id and
   secret go into the cluster secret as `Storage__Blobs__AccessKeyId` and
   `Storage__Blobs__SecretAccessKey`, with the other keys above, through `gsbc.argo`. Never in this
   repo.
7. **Check it** after the first upload: `tools/dev-proxy/check-caching.sh https://media.baptist.com.au
   <key>` expects `immutable` and should show `HIT` the second time.

## Tests

- `Features.Media.Api.Tests` (Postgres and SeaweedFS in containers): a file's name is its content;
  bytes that aren't the type they claim are refused and not kept; private media is only linked while
  the caller holds its audience; nobody can make a file private to people they aren't among.
- `MediaCacheTests` (Swift): bytes that don't hash to their key are never cached; the least recently
  used file goes first; forgetting private files keeps public ones.
- Also in `Features.Media.Api.Tests`: a saved `MediaRef` is the library's (forged keys and unknown ids
  refused, forged type, size, URL and privacy replaced); dimensions come from the bytes, turned by EXIF
  orientation; a file in use can't be deleted, and shared bytes stay while another entry lists them.
- `MediaCacheTests` also: prefetching skips large files, stops at its budget, drops files larger than
  declared and never fetches twice.
- `PlacementEditorTests` (Configuration): images in settings are stored as the library's; forged and
  private ones are refused.
- Tiers and permissions on every `Media` method: `TierRules` in Architecture.Tests.

## Open

- **The server changes aren't running yet:** the `media.uses` migration, verification and Delete need
  the API restarted from a checkout that has them.
- **Android's media cache:** Android has no `MediaCache` yet, so prefetching (and caching) are iOS only.
- **Background prefetch:** records synced in the background get their images on the next open (above).
- **Media in the Storage screen's total.**
- **A phone page** for uploading (Life Group leaders), when a feature needs it.
- **Existing items' dimensions:** files uploaded before 2026-11-02 keep the dimensions the client sent
  (none, from the admin panel). Development data only; re-upload if it matters.
