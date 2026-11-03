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
| [data-driven.md](architecture/data-driven.md) | What staff can reshape without a release, and how |
| [drafts.md](architecture/drafts.md) | Drafts, the event layer and previewing on your own phone, before anything reaches everyone |
| [rich-text.md](architecture/rich-text.md) | The one formatted-text format: a Markdown subset, checked on save, one renderer and editor |

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

**Waiting on the owner:**

- ~~The Apple enrolment~~: done 2026-11-03. The church is enrolled as an organisation (the church
  is the seller), team `YNT9A4LGVA`. Passkeys in the app are built on it. Next on the owner's side:
  - an **APNs key** for silent push: a secret, so it goes in user secrets or the cluster secret,
    never in chat or the repo;
  - the **App Store Connect record** for TestFlight (bundle `au.com.baptist.app`);
  - the **production association file** at the `baptist.com.au` apex through Cloudflare
    ([identity.md](architecture/identity.md), *Apple*).
- For media, now built: two R2 buckets, the public one's custom domain, a cache rule, CORS and a
  lifecycle rule on the private one, and an API token in the cluster secret
  ([media.md](features/media.md), *What the owner sets up*).

**Done by the owner, 2026-11-02:** the Cloudflare zone is set and checked (Browser Cache TTL respects
our headers, Web Analytics off, app hosts purged), and media goes through R2; see
[offline-and-caching.md](architecture/offline-and-caching.md).

**Before production (not for a while; nothing to do until the API goes to the cluster):**

- Production config through `gsbc.argo`: the token signing key, `Accounts:Passkeys` for
  `baptist.com.au`, and the email secret with the production mailer certificate
  ([email.md](architecture/email.md), *Going to production*).
- Check the admin panel on `app.baptist.com.au` with `tools/dev-proxy/check-caching.sh` once it
  replaces the POC there.

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
7. **Drafts and previews** ([drafts.md](architecture/drafts.md)). Built 2026-11-02 (branch
   `feature/drafts`): every change staff arrange is made in a draft, previewed live on their own phone
   (proven on the owner's iPhone: a save shows within a second), shared through Access, then published
   or turned on for an event. Home must be built on `HomeScreen` and `SurfaceViews` so the admin
   preview stays true. Next: a design pass over the admin panel's pages, designed for desktop.
8. **Features, page by page:** Home (Hub), Sermons, Events, Life Groups, Ministries, Site map, Prayer,
   Push, Give (link out), About. Each starts from its doc and the matching screens in
   `spikes/app-poc`.
