# Media

Images and files (sermon artwork, leader photos, Life Group notes): how they're uploaded, named,
served, cached on the phone and kept private. The route was decided by the owner on 2026-11-02 (R2,
signed links from gRPC; [offline-and-caching.md](../architecture/offline-and-caching.md)). Built
2026-11-02.

## What people can do (v1)

| Who | Can |
|---|---|
| `media.upload` | Upload an image (JPEG, PNG, WebP, GIF) or a PDF, public or private to an audience they belong to |
| `media.view` | See the media library: every staff folder and the files in it, and where each file is used |
| `media.organise` | Make, rename, move and delete folders, and move files between them (folders, below) |
| `media.system.view` | See inside every system folder, such as the files people share in group chats (sensitive; only the Owner role by default) |
| `media.delete` | Delete a file nothing uses any more |
| `media.upload` | Bring a font in from Google Fonts (free to use in the app), for a theme |
| `media.fonts.upload` | Upload a font file (.woff2, .woff, .ttf, .otf): only fonts the church is licensed to embed in an app |
| Anyone signed in | Get a link to a private file, **if they hold its audience right now** |
| Anyone | See public media: it's a plain URL on the media host |

The **Media editor** system role holds `media.view`, `media.upload`, `media.delete` and
`media.organise`. It doesn't hold `media.system.view` (owner, 2026-11-04), nor `media.fonts.upload`: a
font file's licence is a separate judgement, given on its own (owner, 2026-11-03). The library page is in the admin panel; the upload
and library components live in Media.Client, so a phone page is a page change, not a rewrite.

## Folders (owner, 2026-11-04)

The owner's request: "a folder structure or something similar will be needed... moving things around
in folders should not unlink them from the various pieces they are used in." Decided with the owner:

- **One folder per file**, like a computer's folders; moving takes it out of where it was.
- **Staff folders are purely for organising.** They never change who sees a file (that stays the
  file's own audience) or what the app shows. Anyone with `media.view` sees every staff folder.
- **A folder is metadata, not part of the storage key.** `media.folders` holds the tree;
  `media.items.folder_id` says where a file is. A file's id, key, bytes and `media.uses` never change
  when it or a folder above it moves, so drafts, tiles, themes and rich text keep working
  (`MovingFilesAndFoldersAroundNeverChangesAFilesIdentityOrWhereItIsUsed`).
- **Folders nest, and are created, renamed, moved and deleted** (`gsbc.media.Folders`, gRPC, all
  `media.organise` except `List`). Names are unique among siblings, ignoring case, up to 80
  characters; a folder can't go inside itself.
- **Deleting a folder never deletes files.** An empty folder is deleted after a confirmation; one with
  files or folders in it asks where they go (its parent by default) and moves them first, in one
  transaction (`DeletingAFolderNeverDeletesTheFilesInIt`).
- **Moving:** drag files or folders onto a folder tile or a breadcrumb, or choose files (click, Ctrl
  or ⌘-click, Shift-click, or the round tick on a picture) and press **Move to…** in the tray, which
  lists every staff folder for keyboard users. `Media.Move` takes many files and folders at once.
- **Uploads go into the open folder** (`BeginUploadRequest.FolderId`); search, from the title band,
  covers every folder the viewer may see and shows each result's folder. The type filters and
  "Not used" work inside a folder, counted by the server (`MediaList.Counts`).
- **Live:** folder and file changes mark the `media.library` feed, so two admins see each other's
  moves through `LiveData`.

### System folders: made by the app

Files people upload through the app (group chats, Life Group notes, later features) are kept in
**system folders**, apart from what staff upload. Shown on their own shelf, **Made by the app**, with
a lock and the area's caption ("Made by the app: files people share in group chats").

- **Defined in code by the owning feature**: it registers a `MediaSystemArea` (key, name, caption,
  audience permission) with `services.AddMediaSystemArea(...)` (Platform.Api), and after saving a
  record that holds uploads it calls `IMediaSystemFolders.FileAsync(area, scopeId, scopeName, refs)`.
  The area's folder and the scope's subfolder (`Chats / Youth group`) are made on demand, and the
  subfolder is renamed when the scope's name changes.
- **Staff can't rename, move or delete them, move files in or out, or delete the files in them** (`FailedPrecondition`): the app keeps them where it put them, and the feature that filed a file removes it.
- **Who sees inside goes through Access** (owner, 2026-11-04): people holding the area's audience
  permission at that scope (the same audience as the files, e.g. members of that chat), plus anyone
  holding `media.system.view`, a sensitive permission only the Owner role has by default, for
  oversight. Other staff see that the area exists ("Shared with others") but not its subfolders or
  files, and search leaves those files out
  (`SystemFoldersAreKeptByTheAppAndOnlyTheirAudienceOrOversightSeesInside`).
- No feature files anything yet: the first is group chats. A feature's uploads still need
  `media.upload` today; a phone upload path for members is part of that feature's work.

### The picture sheet

The sheet that chooses a picture (`MediaChoice`, for tiles, settings and rich text) browses the
Library's folders with the same breadcrumbs and folder tiles, searches every folder, uploads into the
open folder, and opens on the last folder used in that browser (`MediaFolderMemory`, `localStorage`).
System folders aren't offered: their files are people's own uploads, usually private.

## Fonts (owner, 2026-11-03)

Staff can use any font in a theme they make ([data-driven.md](../architecture/data-driven.md), *Themes*),
for the masthead and for body text. A font is media like any other: public, content-addressed, cached
on the phone, and held by the draft or theme that uses it, so it can't be deleted from under one.

- **From Google Fonts** (`gsbc.media.Fonts`, gRPC): `Search` matches names in Google's public catalogue
  (`fonts.google.com/metadata/fonts`, kept for a day); `Import` asks Google's CSS API for the family's
  regular and bold Latin files, downloads them on the server, checks they're really WOFF2 and stores
  them in the public bucket. A variable font's one file covers both weights. These are outbound calls,
  not endpoints. If Google changes that catalogue's format, search breaks and nothing else does.
- **Uploaded**, through the usual presigned upload, only with `media.fonts.upload`; a font is never
  private, since everyone using the app downloads it.
- **On screen**, a theme's font files become `@font-face` rules with a family name made from the
  file's hash, never from the name staff typed.
- **Production needs CORS on the public media host** for fonts, since browsers fetch `@font-face`
  across origins with CORS (images don't need it). The phone is unaffected: it serves the stored copy
  from its own origin. Dev's store already allows every origin.

## How an upload works

No endpoint is added to our hosts: bytes go between the device and the bucket, and our API only hands
out links over gRPC (AGENTS.md's hard rule).

0. **Is it already here?** As soon as a file is chosen, the browser hashes it (SHA-256, with
   `crypto.subtle` on the file itself, so no bytes go anywhere) and asks `Media.FindByHash` (gRPC). If the
   library has it, the upload says so in plain words and shows the file it has (name, folder, how often
   it's used, a thumbnail), with **Use the existing one** (the default) or **Add it again anyway**. Using
   the existing one sends nothing: no ticket, no bytes. Duplicates, below.
1. `Media.BeginUpload` (gRPC) checks the type, and for a private file that the uploader holds its
   audience, then records a pending upload and returns a **presigned PUT** (15 minutes) to a staging
   key `uploads/<id>` in the private bucket.
2. The browser or phone PUTs the bytes straight to R2 with that content type.
3. `Media.CompleteUpload` (gRPC) reads the staged object, **hashes it and checks its first bytes match
   its claimed type** (an HTML page sent as `image/png` is refused). **It checks for a duplicate again**
   with its own hash, since it can't trust the client's: if the caller can see a file with the same
   bytes and the same audience, and didn't ask to add it again (`AddAgain`), it discards the staged
   object and returns that file (`UploadedMedia.AlreadyInLibrary`). Otherwise it copies it to its final key with
   `Cache-Control: public, max-age=31536000, immutable`, deletes the staged copy, and returns the new
   library entry. An image's **width and height are read from its bytes** (PNG, GIF, WebP and JPEG,
   turned by the JPEG's EXIF orientation so a sideways phone photo gets its displayed shape), so
   placeholders know their shape. The client no longer sends them.

Abandoned uploads: the pending row is pruned after a day, and the staged object by the bucket's
lifecycle rule (below).

**Size:** no cap (owner, 2026-11-02). The upload component warns over 10 MB that everyone who sees it
downloads all of it, and offers "Upload anyway". No resizing for now; review if big photos become a
problem.

## Names: a file's name is its content

The key is `<sha256 of the bytes>.<extension>`, computed by the server, never the client. New bytes
get a new key, the same bytes always get the same key, and a key's bytes never change, so everything
can be cached forever and nothing is ever purged. The extension comes from the content type, so
Cloudflare caches the media host by default.

Each library entry also keeps its hash as a column of its own (`media.items.sha256`, indexed), so
finding a duplicate never depends on how keys are spelled. It was backfilled from the keys on
2026-11-05; before that, every object in the dev bucket was checked to hash to its name (174 of 174).

## Duplicates: one stored copy, and one library entry unless asked (owner, 2026-11-05)

The owner's request: "make sure media in R2 is deduped... Most of our messages use the same default
graphic, or all in a series will." Sermon artwork, a series graphic and a default picture come up again
and again.

- **Storage is always deduplicated.** The same bytes are one object per bucket, whoever uploads them and
  however many library entries list them.
- **The library is deduplicated by default.** An upload of a file the library already has returns the
  entry it has: checked before the upload (`FindByHash`) and again on completion. Server-side imports
  (`StoreAsync`: Google Fonts, downloaded artwork) reuse an existing public entry the same way.
- **"Add it again anyway"** makes a second entry for the same object, with its own name, description and
  folder. Deleting one entry leaves the object while another lists it.
- **A match must be one the caller may see** (owner, 2026-11-05: a private file's audience must not leak
  through "this already exists"). It counts only with the same audience as the upload (public, or the
  same permission and scope, which the uploader must hold right now), never from a system folder, and,
  for someone without `media.view`, only among files they uploaded themselves. Otherwise the upload goes
  ahead as a new entry for the same stored object, and nothing is said.
- Matching is on the hash and the content type, so the same bytes declared as two types stay apart, as
  their keys do.

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
library entry has the same content (an entry added again shares its key).

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
  (which is what a permission change does). They're also dropped when the session changes: by the
  native layer when someone signs in or out on the phone or the session ends elsewhere, and by
  `PrivateMediaGuard`, a client startup task the app and the admin panel both run.
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

The admin panel registers `MediaChoice` as the `MediaPicker`, so "choose a picture" in rich text, in
settings and on a Home tile opens a sheet (`MediaChooser`, Platform.Ui): search, Upload, a grid of
thumbnails with their names and where they're used, one click to select (an orange ring and a tick),
double-click or **Choose** to use it, **Cancel** or Escape to leave. A picture uploaded there is selected,
ready to choose. The Media page's own library (`MediaLibrary`) is for managing files only (owner, 2026-11-04:
the table squeezed into a 380 px settings column had two scrollbars and a detail panel on top of it).

## Configuration

`Storage:Blobs` (Platform.Storage), the same keys in every environment:

| Key | Production (R2) | UAT (R2) | Development |
|---|---|---|---|
| `ServiceUrl` | `https://<account-id>.r2.cloudflarestorage.com` | The same | Set by the AppHost: SeaweedFS on `http://localhost:5310` |
| `PublicServiceUrl` | Not set: signed links use `ServiceUrl` | Not set | `https://dev-media.baptist.com.au` when the dev tunnel serves it, else `ServiceUrl` |
| `Region` | `auto` (the default) | `auto` | `auto` |
| `AccessKeyId`, `SecretAccessKey` | The R2 API token's (cluster secret) | UAT's own token (`storage-secret`) | Random per AppHost run |
| `PublicBucket`, `PrivateBucket` | The two bucket names | `gsbc-uat-media`, `gsbc-uat-private` | `gsbc-media`, `gsbc-private` |
| `PublicBaseUrl` | `https://media.baptist.com.au` | `https://uat-media.baptist.com.au` | `<PublicServiceUrl>/gsbc-media` |
| `CreateBuckets` | Never set | Never set | `true`: the API creates the buckets |

As environment variables: `Storage__Blobs__ServiceUrl` and so on. In development the AppHost runs
SeaweedFS (MinIO no longer publishes images) unless `Storage:Blobs` is set in the AppHost's user
secrets, in which case it passes those to the API instead.

Signed links go to browsers and phones, so they carry `PublicServiceUrl`, while the API reaches the
store at `ServiceUrl`. In development the admin panel is on `https://dev-app.baptist.com.au`, and a browser
refuses an upload from that page to `http://localhost` (mixed content), and a phone can't reach
localhost at all. So the dev tunnel also serves `dev-media.baptist.com.au` → SeaweedFS
([tools/dev-proxy](../../tools/dev-proxy/README.md)), and the AppHost signs links for it whenever
`~/.cloudflared/config.yml` names that hostname. It's `dev-media`, not `media.dev`: Cloudflare's
free certificate covers one level of subdomain only.

## What the owner sets up in Cloudflare (not done yet)

For production. UAT has its own two buckets on `uat-media.baptist.com.au`, set up the same way; the
clicks are in [environments.md](../architecture/environments.md), *Setting it up*.

1. **Two R2 buckets**, for example `gsbc-media` (public) and `gsbc-private`. Leave the `r2.dev`
   public URL off on both.
2. **A custom domain on the public bucket:** `media.baptist.com.au` (bucket → Settings → Custom
   Domains). Nothing on the private bucket.
3. **A cache rule** for `media.baptist.com.au`: *Eligible for cache*, edge TTL *use cache-control
   header if present*. Recommended, not strictly needed: our extensions (jpg, png, webp, gif, pdf)
   are cached by default, but the rule keeps it true for any type added later.
4. **CORS on the private bucket**, for uploads: methods `PUT`; header `content-type`; origins
   `https://app.baptist.com.au`, `capacitor://localhost` (the iOS app) and `http://localhost:5300` (the
   local admin panel). **And on the public bucket**, for fonts (above): `GET` and `HEAD` from
   `https://app.baptist.com.au`.
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
- Duplicates (`Features.Media.Api.Tests`): uploading a file the library has gives back the entry it has
  and keeps one object; adding it again anyway makes a second entry for the same object; a match the
  caller can't see (another audience, or the whole library without `media.view`) is never revealed.
- Folders (`Features.Media.Api.Tests`): moving never changes a file's identity or uses; deleting a
  folder never deletes files; system folders are locked and only their audience or
  `media.system.view` sees inside.

## Open

- **The server changes aren't running yet:** the `media.uses` migration, verification and Delete need
  the API restarted from a checkout that has them.
- **Android's media cache:** Android has no `MediaCache` yet, so prefetching (and caching) are iOS only.
- **Background prefetch:** records synced in the background get their images on the next open (above).
- **Media in the Storage screen's total.**
- **A phone page** for uploading (Life Group leaders), when a feature needs it.
- **Existing items' dimensions:** files uploaded before 2026-11-02 keep the dimensions the client sent
  (none, from the admin panel). Development data only; re-upload if it matters.
