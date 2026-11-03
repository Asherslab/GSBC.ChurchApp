# Places in the app (destinations)

Decided and built 2026-11-03 (branch `feature/app-destinations`), after the owner's note on Announcements →
What it opens: "we need a way for them to discover / get links for pages in the app easily. even link
directly to specific sermons, calendar events, whatever."

Staff never type an address. Every place people can be sent to (a page, or one thing inside a feature) is
declared by the feature that owns it, listed in the admin panel's **Places in the app**, picked from the same
list wherever something opens a place, and shared as a link that opens the app.

## What a place is

| | Declared as | Example | Path |
|---|---|---|---|
| **A page** | `DestinationPage` in the feature's Contracts | Notification settings | `/notifications/settings` |
| **One thing** | `DestinationKind` in Contracts, plus an `IDestinationSource` in the feature's Api | A file in Media | `/files/{id}` |

Both live in `Platform.Contracts/Destinations`, so the API, the app and the admin panel read the same
declarations. Each feature registers its own set (`AddDestinations(XxxDestinations.All)`) in its Api and its
Client, as it does for permissions, surfaces and notification kinds. Features never reference each other.

- **A page** has a plain name, an icon, an area (Pages), *where it is* in the app's words ("More → What's
  new") and who can open it (an `Audience`: everyone, signed in, or holding a permission).
- **A kind** names its area (Files, and later Events and Sermons), a noun, an icon, its path prefix, the feeds
  whose changes should refresh lists of it, and whether it **opens outside** the app (a file opens in the
  phone's browser).
- **The shell's own pages** (Home, More, What's new, Appearance, Storage) are in `ShellDestinations`.
  What's new belongs to ReleaseNotes, which has no Contracts project, so it is declared with the shell.
- `DestinationCatalog` resolves any link to its page or its kind and id, however it is written: `/x`,
  `/x?y`, `/open/x`, or `https://<app host>/open/x`. Two features claiming the same path fail at start-up.
- `DestinationRules` (Architecture tests) fails when a declared page isn't a route in the app.

## Finding places: one gRPC service

`gsbc.destinations.Destinations` (Platform.Api, `DestinationsService` over `DestinationDirectory`):

- **`Search`** `[SignedIn]`: text and an optional area. Returns places grouped by area: pages matching the
  text, and each kind's own `IDestinationSource.SearchAsync` (newest files first; upcoming events first when
  Events is built). Empty text is the browse list: the first few of each area, then "Show all" per area.
- **`Describe`** `[Public]`: one path → its name, where, icon, who can open it, its date, and a state:
  *Available*, *NotForYou*, *Gone* or *Unknown*. For a kind that opens outside, an available answer carries
  the URL to open (a private file's is a five-minute signed link).

**Who may see a place is always Access.** The directory checks every place against the caller with
`IAccessEvaluator`, the same check as everything else ([permissions.md](permissions.md)); a source only says
who a thing is *for* (an `Audience` and an optional scope). A place you can't open isn't listed, and
describing it returns *NotForYou* with no name, so a private file's name never leaks.

**For an audience:** `Search` takes `OpenableBy` (an audience, such as an announcement's). It then lists only
places *everyone* in that audience can open (`DestinationAudiences.OpenableByAll`): a signed-in page is not
offered for an announcement to everyone with the app; a permission-gated file only for an audience holding
that same permission, globally. Announcements' server also refuses an in-app link that isn't a place.

Adding a feature's items (Sermons, Events, Ministries) means: a `DestinationKind` in its Contracts, an
`IDestinationSource` in its Api (`AddDestinationSource<T>()`), and its page for `/{prefix}/{id}` in its App,
which shows the shared not-found state when the item is gone.

## In the admin panel

- **Places in the app** (`/places`, the sidebar's *The app* group; any signed-in staff member): the directory
  (search, area chips, rows grouped by area) beside a phone showing the chosen place, who can open it, its
  actions and its link to copy. The page was chosen by the owner on the decision page (2026-11-03, after a
  steer toward "a good overview for exploring and modifying specific things").
- **The phone** draws the place live where its feature registers a `PlacePreview` (Configuration draws Home
  through `HomeScreen`, as published); otherwise a summary in the app's own style.
- **Actions** are `PlaceAction`s registered by features, each gated by a permission: *Announce it*
  (Notifications: a new announcement that already opens the place), *Change it in a draft* (Configuration,
  for Home), *Open in Media* (files).
- **The picker** is `PlaceField` (Platform.Ui): the chosen place as a token (icon, name, where, Change, clear),
  and a wide sheet with the same `PlaceDirectory`. Announcements → What it opens → *A place in the app* uses
  it with the announcement's audience. Home tiles and links in rich text can use the same field.
- The components are in `Platform.Ui/Destinations`: `PlaceDirectory`, `PlaceExplorer`, `PlacePhone`,
  `PlaceSummaryPhone`, `PlaceField`. They load through `LiveData` with the kinds' feeds.

## Links

- **A shared link is `https://<app host>/open/<path>`**: `dev-app.baptist.com.au` in development,
  `uat-app.baptist.com.au` in UAT, `app.baptist.com.au` in production (owner, 2026-11-03: the app host, not
  the website). `DestinationPaths.Shareable` builds it. The `/open` prefix keeps app paths apart from the
  admin panel's own pages on the same host (an app's `/events` and an admin `/events` can both exist).
- **On a phone with the app**, the link is a universal link: the association file lists `/open` and
  `/open/*` beside `/welcome` (`AppAssociation.LinksIntoTheApp`), served as the same static file as before.
  The app's `applinks:` entitlement now names `GSBC_LINKS_DOMAIN`, the app host, in every environment
  (`webcredentials:` still names `GSBC_ASSOCIATED_DOMAIN`, which is the apex in production for passkeys).
- **Anywhere else**, the gateway serves the admin panel's static files as for any client route, and its
  `/open/{*path}` page (no sidebar, no sign-in) names the place if it's public and says how to get the app.
  It varies nothing per request on the server, so no new API path (AGENTS.md); the name comes from
  `Describe` over gRPC-Web.

## On the phone: one router

`LinkOpener` (Platform.Ui) opens every link: a notification tap and a universal link (both arrive through the
native `linkOpened` event), a row in the inbox, What's new's *Show me*, and an in-app link followed to an
unknown route.

- Web, email and phone links go to the system (`Shell.OpenExternalAsync`); a link to the app's own host is
  treated as in-app.
- A page you may open is navigated to; one you may not shows *Sign in to see this* (signed out) or *This
  isn't available to you*.
- A kind that opens outside is described and opened in the browser; *Gone* shows *This has been taken down*.
- Anything else shows *There's nothing here*, which also covers a link made by a newer version of the app.

Those states are the app's `OpenPlace` page (`/unavailable`, `/open/{*path}`, and the catch-all route).
Showing staff tools on the phone is not part of this (AGENTS.md, *Admin work stays in the admin panel*).

## Members sharing (later)

The owner chose "staff now, members later" (2026-11-03). A Share action on items in the app (the system share
sheet, with `DestinationPaths.Shareable`) waits until Sermons and Events exist, and needs the owner's yes as
phone UI.

## What the owner sets up

Nothing for dev or UAT: the association file and the entitlement are in this repo, and Apple's portal doesn't
list domains (the Associated Domains capability is already on). Apple's CDN caches association files, so a
phone picks up the new `/open/*` entry when the app is next installed or updated (or within a day or so).

For production, when it is set up:

1. Nothing new at the apex: `https://baptist.com.au/.well-known/apple-app-site-association` still serves
   passkeys' `webcredentials` ([identity.md](identity.md), *Apple*). Links now use `app.baptist.com.au`,
   whose gateway serves its own association file.
2. Check through Cloudflare that `https://app.baptist.com.au/.well-known/apple-app-site-association` is
   `200`, `application/json`, no redirect, `cache-control: no-cache`, and that `/open/…` serves the admin
   panel's `index.html` uncached (`tools/dev-proxy/check-caching.sh`).
