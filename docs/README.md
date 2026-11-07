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

| Doc | Status |
|---|---|
| [accounts.md](features/accounts.md) | Built: sign-up, passkeys (browser and phone), sessions, invitations, recovery, review sign-in codes, connected tools |
| [access.md](features/access.md) | Built: roles, assignments, the evaluator, staff-friendly screens, the grants feed on the phone |
| [configuration.md](features/configuration.md) | Built: surfaces, settings, placements, drafts, themes |
| [home.md](features/home.md) | Built: the Hub the church arranges |
| [media.md](features/media.md) | Built: uploads, folders, private media, the phone's file cache, fonts |
| [analytics.md](features/analytics.md) | Built: errors, App health, usage and Insights; Sermons, Events and the rest add their measures as they arrive |
| [events.md](features/events.md) | Built: calendars, regular events, specials and breaks, feeds, every screen on the phone and in the admin panel, iCal links, copying iCal calendars in, and reminders; the digest proof of concept next |
| [ministries.md](features/ministries.md), [life-groups.md](features/life-groups.md), [site-map.md](features/site-map.md), [outreach.md](features/outreach.md) | Designed, not built |

## Where we are (2026-11-04)

`skeleton/backbone` is merged to `main` routinely. CI (build and test, the AOT publish check, and the
native data layer's tests) runs on every push to `main`, and once it passes the UAT backend deploys
by itself ([environments.md](architecture/environments.md)). TestFlight builds are started by hand when
the app changed; builds 1 to 5 have shipped. A push that changes only docs builds nothing.

**Built and working:**

- **The backbone:** the skeleton and its rules; the native data layer on iOS (store, live sync, outbox,
  background refresh); Accounts and Access in the browser and on the phone; Configuration, drafts and
  previews; Media (R2, folders); release notes and What's new; Analytics, error reporting and App
  health (also readable from the terminal); notifications with push, the inbox, announcements, quiet
  hours and the prompt on first install; places in the app and `/open/…` links; the install-proof fix
  for App Attest ordering; App review sign-in codes for Apple's reviewers.
- **The admin panel**, redesigned for a computer and live: pages update when anyone changes what they
  show.
- **Home, the Hub** ([home.md](features/home.md)): the tile library, picture layouts, live drag on a
  phone canvas, draft undo and redo, and a publish undo that follows what's live.
- **Environments:** dev (the AppHost through a Cloudflare tunnel), UAT (backend on the mini cluster,
  R2 media, TestFlight) and the Cloudflare zone's caching, all checked through the public hostnames.
- **The tooling:** `.editorconfig` and `GSBC.ChurchApp.slnx.DotSettings` keep the IDE's warnings clean;
  `tools/docs/check-links.py` checks the docs' links and paths.

**Sermons (2026-11-05):** a Sermons tab on the phone (the sermon you're part-way through,
else the newest; series, speakers, search, offline), audio played natively in the background with the lock screen's
controls, listening progress that is never forgotten (kept on the phone, merged into your account when you sign in, on
every device), YouTube inline in the app, and the Home tile made real. Sermons come from the website's feed and series
pages, or, at the flip of a switch on the admin panel's Sermons page, from uploads to R2 with our own podcast feed;
staff check and correct new sermons there ([sermons.md](features/sermons.md)). Checked in the iOS Simulator and on
`dev-app.baptist.com.au`; on the owner's iPhone for the background-audio and second-device checks.

**Next:** Events, per the build order below.

**Waiting on the owner:** only checks that need their hands and phone:

- the two-window draft test in the admin panel with two signed-in accounts (it needs two passkeys);
- background refresh measured on a real phone over a few days.

**Production is future work, not waiting.** It doesn't exist yet. When it does: the cluster secrets and
config through `gsbc.argo` (token signing key, `Accounts:Passkeys` for `baptist.com.au`, the email secret
with the production mailer certificate, the APNs key, R2 buckets and token), the association file for
`baptist.com.au`'s apex, the App Store Connect record, and a `check-caching.sh` run against
`app.baptist.com.au` ([environments.md](architecture/environments.md), *Setting it up*;
[email.md](architecture/email.md), *Going to production*).

**Working on this repo:** start the backend from Rider's AppHost configuration (stop a running one
first: its ports are fixed); build with Homebrew's `dotnet` (`global.json`); every new project goes in
the project map and the picture (`ProjectMapRules` enforces it); check anything web-facing through
`dev-app.baptist.com.au`, not localhost (`tools/dev-proxy/check-caching.sh`); a new worktree copies the
main checkout's database first (`tools/dev/copy-main-db.sh`). The repo is public: no keys, personal data,
or names of the owner's employer or its projects.

## Build order

Linchpins first, then outwards, one feature at a time and tested on a phone before the next
(requirement F). iOS first; Android's native side stays stubbed until it is built properly.

1. **Skeleton** (done): the layout, the AOT rules and baseline, the gRPC-only and tier tests, Sync's
   change log and `NOTIFY` trigger, the gateway routing gRPC and gRPC-Web, one end-to-end call.
2. **Native data layer** (done on iOS): gRPC-Web transport, GRDB store with per-feed caps, pull and live
   `Watch` with `LISTEN` fan-out, outbox, background refresh, silent push, server-side compaction,
   pruning and reset horizon, the Storage screen and the dev loop. Open: measure background refresh on a
   real phone, and prove a long-held `Watch` through the cluster's Traefik.
3. **Accounts** ([accounts.md](features/accounts.md)) (done): sign-up, sign-in, passkeys in the browser
   and the app, sessions, recovery, invitations (and the ones not yet accepted), review sign-in codes.
4. **Access** ([access.md](features/access.md)) (done): roles, assignments, the cached evaluator,
   escalation guards, the admin screens and the grants feed for the phone.
5. **Configuration** ([configuration.md](features/configuration.md)) (done): surfaces and settings
   descriptors, the `config` schema and service, the placements feed, `ISurfaceLayout`, themes.
6. **Media** ([media.md](features/media.md)) (done): signed R2 uploads named by their SHA-256, private
   media, the phone's hash-checked cache, prefetching, fonts, and folders (staff folders, system folders
   that features fill through `IMediaSystemFolders`). Open: the first feature to file into a system
   folder (chats).
7. **Drafts and previews** ([drafts.md](architecture/drafts.md)) (done): every change is made in a
   draft, previewed live on your own phone, shared through Access, then published or turned on for an
   event; undo and redo inside a draft and undo of a publish. The admin panel for the desk is built on
   it.
8. **Also built on the backbone:** release notes ([changelog.md](architecture/changelog.md)), analytics
   ([analytics.md](features/analytics.md)), notifications and push
   ([notifications.md](architecture/notifications.md)), places and links
   ([destinations.md](architecture/destinations.md)), environments and CI
   ([environments.md](architecture/environments.md)).
9. **Features, page by page:** Home (Hub) is done; **Sermons next, then Events**; then Life Groups,
   Ministries, Site map, Prayer, Give (link out) and About. Push is built as the notifications backbone;
   Announcements are the first thing that uses it. Each starts from its doc and the matching screens in
   `spikes/app-poc`. Outreach has a design ([outreach.md](features/outreach.md)) but no place in this
   order yet.
