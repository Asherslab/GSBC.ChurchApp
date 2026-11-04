# Docs

[SPEC.md](../SPEC.md) is the design. These docs hold the detail.

## Architecture (the backbone)

| Doc | Covers |
|---|---|
| [repo-layout.md](architecture/repo-layout.md) | Folders, projects, naming, a feature's parts, the rules the tests enforce |
| [identity.md](architecture/identity.md) | One passkey sign-in for app and admin; sessions; recovery |
| [permissions.md](architecture/permissions.md) | Nodes, scopes, roles as data, how the server and the UI check |
| [offline-and-caching.md](architecture/offline-and-caching.md) | Sync as the only data path; media that never goes stale; Cloudflare |
| [email.md](architecture/email.md) | Account email through a Microsoft 365 shared mailbox and Graph; the queue |
| [storage.md](architecture/storage.md) | Postgres schemas per feature, R2 over the S3 API, Dropbox |
| [environments.md](architecture/environments.md) | Dev, UAT and production: apps, hosts, what deploys when, TestFlight, the owner's set-up |
| [data-driven.md](architecture/data-driven.md) | What staff can reshape without a release, and how |
| [drafts.md](architecture/drafts.md) | Drafts, the event layer and previewing on your own phone, before anything reaches everyone |
| [notifications.md](architecture/notifications.md) | Notification kinds any feature raises, audiences through Access, the inbox, push, silent sync, announcements |
| [changelog.md](architecture/changelog.md) | Release notes: one file per change, built into the app; What's new after an update; versions; TestFlight's What to Test |
| [rich-text.md](architecture/rich-text.md) | The one formatted-text format: a Markdown subset, checked on save, one renderer and editor |
| [destinations.md](architecture/destinations.md) | Places in the app: pages and items features declare, finding them over gRPC, the picker, shared `/open` links, the phone's one router |

## Features

One doc per feature in [features/](features/), written before its code.

## Where we are (2026-11-03)

All work is on the branch `skeleton/backbone`, **not pushed**, so CI has never run on it. The owner is
keeping it local for now (2026-11-02): don't push without asking.

**Working end to end:** signing in on the phone with a passkey (2026-11-03, proven on the owner's
iPhone; [identity.md](architecture/identity.md)), the skeleton and its rules, the native data layer on iOS (store, live sync,
outbox, background refresh), the dev loop (`tools/ios/deploy.sh`, Rider's iPhone configurations,
`dev-app.baptist.com.au` through a Cloudflare tunnel started by the AppHost), Accounts in the browser
(passkeys, sessions, recovery, invitations, staff tools), and Access (roles, assignments, the
evaluator behind every `[Requires]`, admin screens).

**Analytics and error reporting (2026-11-03, branch `feature/analytics`):** errors from phones (Blazor and
iOS native crashes), the admin panel and the server reach App health in the admin panel, with an email per
new kind; failed calls are counted by the server and the phones; usage totals (phones per day, week and
month, new phones, Hub tile taps) reach Insights for the Insights viewer role. Everything travels as quiet
outbox entries over gRPC; "Help improve the app" under More turns it off
([analytics.md](features/analytics.md)).

**App health from the terminal (2026-11-03, branch `feature/health-from-terminal`):** a developer connects a
tool (such as Claude) on the admin panel's Account page, a session of its own that Access refuses anything
sensitive and that ends after 30 days unused; `tools/health/errors.sh --env dev|uat` then lists and shows
error reports with the secret from the macOS Keychain. Proven on dev with a test account
([analytics.md](features/analytics.md), *Reading App health from the terminal*).

**Home, the Hub the church arranges (2026-11-04, branch `feature/hub`):** Home is a grid of tiles staff
arrange in a draft on a phone canvas in the admin panel: drag to move (the others make room as you drag),
drag a corner to resize, keyboard too, a light/dark preview; tiles take theme tones (never a hex),
built-in or uploaded SVG icons (cleaned on the server), words, pictures (from a picture sheet), a place
or web page, and an audience of everyone, signed in or one or more roles (through Access). Plain
tiles, Notifications for real, and sample tiles for features not built yet, opening *coming soon*
places. The old Hub was rebuilt in dev through the admin panel and published
([home.md](features/home.md)).

**Sermons (2026-11-05, branch `feature/sermons`):** a Sermons tab on the phone (the sermon you're part-way through,
else the newest; series, speakers, search, offline), audio played natively in the background with the lock screen's
controls, listening progress that is never forgotten (kept on the phone, merged into your account when you sign in, on
every device), YouTube inline in the app, and the Home tile made real. Sermons come from the website's feed and series
pages, or, at the flip of a switch on the admin panel's Sermons page, from uploads to R2 with our own podcast feed;
staff check and correct new sermons there ([sermons.md](features/sermons.md)). Checked in the iOS Simulator and on
`dev-app.baptist.com.au`; on the owner's iPhone for the background-audio and second-device checks.

**Waiting on the owner:**

- ~~The Apple enrolment~~: done 2026-11-03. The church is enrolled as an organisation (the church
  is the seller), team `YNT9A4LGVA`. Passkeys in the app are built on it. Next on the owner's side:
  - an **APNs key** for silent push: a secret, so it goes in user secrets or the cluster secret,
    never in chat or the repo;
  - the **UAT app's App Store Connect record** for TestFlight (bundle `au.com.baptist.app.uat`) and the
    rest of UAT's set-up, in order, in [environments.md](architecture/environments.md), *Setting it up*;
  - the **production association file** at the `baptist.com.au` apex through Cloudflare
    ([identity.md](architecture/identity.md), *Apple*).
- For media, now built: two R2 buckets, the public one's custom domain, a cache rule, CORS and a
  lifecycle rule on the private one, and an API token in the cluster secret
  ([media.md](features/media.md), *What the owner sets up*).

**Environments (2026-11-03, branch `feature/uat`):** three apps side by side (GSBC Dev, GSBC UAT,
Good Shepherd), dev renamed to `dev-app.baptist.com.au`, and UAT's backend on the mini cluster following
`main` (`uat-backend.yml`, `gsbc.argo` branch `church-app-uat`, not pushed), with TestFlight uploads from
a button (`testflight-uat.yml`). GSBC Dev on `dev-app` proven on the owner's iPhone (2026-11-03); the
UAT images, caching and health checks tested locally; nothing is on the cluster until the owner's set-up
is done ([environments.md](architecture/environments.md)).

**Done by the owner, 2026-11-02:** the Cloudflare zone is set and checked (Browser Cache TTL respects
our headers, Web Analytics off, app hosts purged), and media goes through R2; see
[offline-and-caching.md](architecture/offline-and-caching.md).

**Before production (not for a while; nothing to do until the API goes to the cluster):**

- Production config through `gsbc.argo`: the token signing key, `Accounts:Passkeys` for
  `baptist.com.au`, and the email secret with the production mailer certificate
  ([email.md](architecture/email.md), *Going to production*).
- Check the admin panel on `app.baptist.com.au` with `tools/dev-proxy/check-caching.sh` once
  production serves it there (the POC preview was retired 2026-11-03).

**Tested by the owner, 2026-11-02:** the owner side of Access (creating a role and giving it to a user)
and inviting, with a real email through the shared mailbox.

**Not tested end to end yet:** the staff-friendly access screens and Configuration's editor (built and
unit-tested on 2026-11-02, not yet tried in the running app); Media on a phone. Live sync on a real iPhone through Cloudflare was proven with Configuration's feed
(2026-11-02: live changes, idle stream, offline catch-up, outbox). Media uploads from the
admin panel work through `dev-media.baptist.com.au` (owner, 2026-11-02); background refresh needs measuring on a real phone over a few days. The dev database holds a few `e2e+…@example.com` test accounts.

**App shell and design system (2026-11-02, branch `feature/design-system`):** tokens, the phone shell
(tab bar, pushed pages, edge-swipe back, safe areas) and the shared building blocks in Platform.Ui
(DESIGN.md, *Implementation*); the theme is staff's choice as data (data-driven.md, *Themes*). On the
owner's iPhone for review.

**Release notes and What's new (2026-11-03, branch `feature/changelog`):** notes are one file per change
in `changelog/`, built into the app by a source generator (staff wording in dev and UAT, the everyone line
in Release); the highlights show once after an update, and More → What's new lists every version; the
TestFlight workflow sets What to Test from the notes since the last upload's tag
([changelog.md](architecture/changelog.md)). The show-once rule proven natively in the iOS Simulator (a fresh
install shows nothing; a staged update shows the highlight once) and deployed to the owner's iPhone; the
What to Test call and the tag push first run with the first TestFlight build.

**Admin panel (2026-11-03, branch `feature/admin-design`):** redesigned for a computer (build order 7); checked on `dev-app.baptist.com.au` at 1280, 1440 and 1920, light and dark.

**Review sign-in codes (2026-11-03, branch `feature/review-code`):** Apple's reviewers sign in to a
demo account (the App reviewer role) with a sign-in code made on the admin panel's App review page,
through the gRPC `Accounts.SignInWithCode`; no passwords and no new API path
([identity.md](architecture/identity.md), *Review sign-in codes*). Tested against Postgres and natively;
what goes in App Store Connect is in [environments.md](architecture/environments.md), *Sign-in for Apple's
reviewers*.

**The admin panel is live (2026-11-03, branch `feature/admin-live`):** pages update within a second or
two when anyone changes what they show (drafts, publishing, media, roles, people, your own access), over
the same `Sync.Watch` stream as the phone, and an open stream ends when its sign-in does
([offline-and-caching.md](architecture/offline-and-caching.md), *The admin panel*; two people in one
draft: [drafts.md](architecture/drafts.md), *Live in the admin panel*). Proven through `dev-app.baptist.com.au`: the
stream starts at the latest cursor and a committed change arrives at once; the two-window test with two
signed-in accounts is the owner's to run (it needs their passkeys).

**Notifications and push (2026-11-03, branch `feature/notifications`):** the design is approved
([notifications.md](architecture/notifications.md)) and built: App Attest (the `[AppInstall]` tier), phones as
holders in Access, notification kinds any feature raises, the inbox and the bell, preferences, APNs with pacing
and quiet hours, the silent push for sync, announcements in the admin panel, and drafts as the first source.
Tested against Postgres; waiting on the owner's APNs key and App Attest/Push capabilities before it runs on a
phone (notifications.md, *What the owner sets up*).

**Install proofs in order (2026-11-04, branch `feature/install-proof-order`):** the phone signs only
`[AppInstall]` calls (the list comes from the server, `Installs.ListProofMethods`), sends them one at a
time, and retries signed if a backend updated mid-session starts asking for proof; push registration
is single-flight. Fixes UAT's refused `RegisterDevice` calls ([notifications.md](architecture/notifications.md),
*Signing rule*).

**Design review of this round (2026-11-03, branch `feature/design-review`):** an impeccable critique of
every screen the five merges added (23/40, snapshot in `.impeccable/critique/`). The inbox and
Announcements were reshaped with the owner on the decision page: the inbox opens on *Waiting* (things
that need you, which leave once they're done, wherever: notifications can wait, settle and say who they're
for, through Access), and Announcements is the office's fortnight with a writing desk, a lock-screen proof,
autosave, a confirm sheet and read counts. Also: App review's code has a Copy button and leads with
the demo account, Insights' chart and private counts read calmly, App health is the Developer role's,
and the sidebar has a group *The app*. Checked on `dev-app.baptist.com.au` and natively in the iOS
Simulator (signed in with a review code). Shared components added in Platform.Ui: `DayRail`,
`LockScreenProof`, `CopyField`; `ListRow` gained `Unread`, `Segmented` a count, `Pill` a Quiet tone.

**Notifications asked on the first open (2026-11-03, branch `feature/notification-prompt`):** a covered
sheet over Home, while iOS's permission is undecided, with Turn on notifications and Not now, then the
per-kind choices in the same sheet; it comes before What's new, which waits for the next open
(`IOpeningSheet`, [notifications.md](architecture/notifications.md), *The first-open sheet*). Insights counts
phones with notifications on, turned off or not chosen yet, once a week and anonymously (a measure's
`States`, [analytics.md](features/analytics.md)). Shared: `SheetHeading` in Platform.Ui (What's new uses it too). Fixed from UAT on the same branch: the "notifications are off" notice clears on return from Settings
(the permission is re-read when the app becomes active), a tapped notification opens its link (Capacitor had
taken the notification delegate; `ios.handleApplicationNotifications: false`), and a moment on the day rail
stays in its own day.

**Places in the app (2026-11-03, branch `feature/app-destinations`):** every page and item a feature declares
is a place: the admin panel's Places in the app lists them (search and areas) beside a phone showing the
chosen one, with Copy link, Announce it, Change it in a draft and Open in Media; Announcements → What it opens
picks from the same list, offering only what the announcement's audience can open; links are
`https://<app host>/open/…` and open the app (the association file and `applinks:` on the app host), or an
"Open in the app" page elsewhere; on the phone one router opens every link and says kindly when a place is gone
or not for you ([destinations.md](architecture/destinations.md)). Files are the first items; Sermons and Events
plug in with an `IDestinationSource`. Checked on `dev-app.baptist.com.au`; the universal link on a phone
waits for the next install of GSBC Dev.

**Admin leftovers (2026-11-04, branch `feature/admin-leftovers`):** Announcements say when quiet hours
hold a send and what Urgent does ([notifications.md](architecture/notifications.md)); People lists
invitations not yet accepted, with Send it again and Withdraw, and Needs you shows ones waiting two days
or expired ([accounts.md](features/accounts.md)); Roles and Who can do what name a draft scope ("For the
draft “Christmas Home”") through per-feature `IScopeNamer`s ([access.md](features/access.md)). Checked on
`dev-app.baptist.com.au` with a test account.

**Next, in order:** features page by page from the build order below, starting with Home (Hub) on
Configuration's surfaces.
The staff-friendly permissions are designed and built (2026-11-02, [access.md](features/access.md),
*How staff see access*); Ministries plugs into them as described there.

**Working on this repo:** start the backend from Rider's AppHost configuration (stop a running one
first: its ports are fixed); build with Homebrew's `dotnet` (`global.json`); every new project goes in
the project map and the picture (`ProjectMapRules` enforces it); check anything web-facing through
`dev-app.baptist.com.au`, not localhost (`tools/dev-proxy/check-caching.sh`). The repo is public: no keys,
personal data, or names of the owner's employer or its projects.

## Build order

Linchpins first, then outwards, one feature at a time and tested on a phone before the next
(requirement F). iOS first; Android's native side stays stubbed until it is built properly.

1. **Skeleton** (done 2026-11-01): the layout, the AOT rules and baseline, the gRPC-only and tier
   tests, Sync's change log and `NOTIFY` trigger, the gateway routing gRPC and gRPC-Web, one
   end-to-end call (`Accounts.Session.GetCurrent`).
2. **Native data layer** on a real iPhone. Built: gRPC-Web transport, GRDB store with per-feed caps,
   pull and live `Watch` with `LISTEN` fan-out, outbox, background refresh, server-side compaction,
   pruning and reset horizon, the Storage screen and the dev loop; tested by the wire-format pair, the
   soak test and a Postgres retention test. Still to do: measure background refresh on a real phone,
   and test through Cloudflare and Traefik (needs the dev proxy). Silent push waits for the Apple
   enrolment and an APNs key.
3. **Accounts** ([accounts.md](features/accounts.md)). Built in the browser: sign-up, sign-in,
   passkeys, sessions, recovery, invitations and the session-to-token swap. Waiting: Access (for
   staff actions), the shared mailbox (real email) and the enrolment (passkeys in the app). All three
   done; passkeys and the Keychain session in the app built 2026-11-03 (branch `feature/phone-sign-in`).
4. **Access** ([access.md](features/access.md)). Built: roles, assignments, the cached evaluator,
   escalation guards and the admin screens. The private grants feed for the phone built 2026-11-03.
5. **Configuration** ([configuration.md](features/configuration.md)). Built: surfaces and settings
   descriptors in Platform, the `config` schema and service (validated, permission per area, audited,
   rolled back), the placements feed, `ISurfaceLayout` for the phone, the generic editor in the app and
   the admin panel, and the Hub area with its link tile. Open: Home rendering it, grants on the phone.
6. **Media** ([media.md](features/media.md)). Built 2026-11-02: uploads through signed R2 links named
   by their SHA-256, private media linked only while you hold its audience, the phone's
   hash-checked file cache, the admin library page; SeaweedFS in development. Waiting: the owner's R2
   setup. Hardened 2026-11-02 (branch `feature/content-foundations`): every saved `MediaRef` is
   verified by the server, dimensions come from the bytes, unused files can be deleted, feeds can
   prefetch their images for offline, and settings can hold an image or rich text
   ([rich-text.md](architecture/rich-text.md)). Not yet run against the API.
   **Folders** (2026-11-04, branch `feature/media-folders`, an Impeccable run; brief in
   `.impeccable/surfaces/edia-gsbc-churchapp-features-media-client-75160210.md`): staff folders that
   nest, move by drag and drop or Move to, and never change a file's id, key or uses; system folders
   that features fill through `IMediaSystemFolders` (`Chats / <group>`), locked to staff and seen by
   their audience or `media.system.view`; the library as two shelves (Library, Made by the app); the
   picture sheet browses folders and remembers the last one. Open: the first feature to file into a
   system folder (chats).
7. **Drafts and previews** ([drafts.md](architecture/drafts.md)). Built 2026-11-02 (branch
   `feature/drafts`): every change staff arrange is made in a draft, previewed live on their own phone
   (proven on the owner's iPhone: a save shows within a second), shared through Access, then published
   or turned on for an event. Home must be built on `HomeScreen` and `SurfaceViews` so the admin
   preview stays true.
   - **The admin panel for the desk** (2026-11-03, branch `feature/admin-design`, an Impeccable run;
     brief in `.impeccable/surfaces/src-hosts-gsbc-churchapp-hosts-admin.md`): a navy sidebar, a home
     page for this week (dated changes on a seven-day rail and what needs you, from features'
     `IOfficeWeekSource`), sign-in, not-found and error states, and People, Roles, Who can do what and
     Media as sortable, filterable tables with the chosen row beside them (`DataTable`, `SplitView`,
     `SearchField`, `FilterChips` in Platform.Ui). Every admin page loads through `LiveData`. Since
     2026-11-04 (branch `feature/admin-leftovers`) invitations not yet accepted are listed and reach
     Needs you, and a role given for one draft names it.
8. **Features, page by page:** Home (Hub), Sermons, Events, Life Groups, Ministries, Site map, Prayer,
   Push, Give (link out), About. Each starts from its doc and the matching screens in
   `spikes/app-poc`.
