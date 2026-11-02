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

## Features

One doc per feature in [features/](features/), written before its code.

## Where we are (2026-11-02)

All work is on the branch `skeleton/backbone`, **not pushed yet**, so CI has never run on it. Pushing it
and opening a PR is the first step when picking up.

**Working end to end:** the skeleton and its rules, the native data layer on iOS (store, live sync,
outbox, background refresh), the dev loop (`tools/ios/deploy.sh`, Rider's iPhone configurations,
`dev.baptist.com.au` through a Cloudflare tunnel started by the AppHost), Accounts in the browser
(passkeys, sessions, recovery, invitations, staff tools), and Access (roles, assignments, the
evaluator behind every `[Requires]`, admin screens).

**Waiting on the owner:**

- Run `tools/access/make-owner.sh <your email>` once to become the first owner (the agent may not grant
  Owner on a real account).
- The Apple enrolment: passkeys in the app (Associated Domains), silent push, TestFlight.
- When media is built: the R2 bucket, its custom domain and a cache rule for it
  ([offline-and-caching.md](architecture/offline-and-caching.md)).

**Done by the owner, 2026-11-02:** the Cloudflare zone is set and checked (Browser Cache TTL respects
our headers, Web Analytics off, app hosts purged), and media goes through R2; see
[offline-and-caching.md](architecture/offline-and-caching.md).

**Before production (not for a while; nothing to do until the API goes to the cluster):**

- Production config through `gsbc.argo`: the token signing key, `Accounts:Passkeys` for
  `baptist.com.au`, and the email secret with the production mailer certificate
  ([email.md](architecture/email.md), *Going to production*).
- Check the admin panel on `app.baptist.com.au` with `tools/dev-proxy/check-caching.sh` once it
  replaces the POC there.

**Not tested end to end yet:** the owner side of Access (creating and giving roles) needs an owner
account (inviting was tested on 2026-11-02, with a real email through the shared mailbox); the last-Owner guard has no automated test; background refresh needs measuring on a
real phone over a few days. The dev database holds a few `e2e+…@example.com` test accounts.

**Next, in order:** push and get CI green; the Access gaps and permissions UX
([access.md](features/access.md), *Usability comes first* and *Open*), designed alongside Ministries,
the first feature whose people assign roles; then features page by page from the build order below.

**Working on this repo:** start the backend from Rider's AppHost configuration (stop a running one
first: its ports are fixed); build with Homebrew's `dotnet` (`global.json`); every new project goes in
the project map and the picture (`ProjectMapRules` enforces it); check anything web-facing through
`dev.baptist.com.au`, not localhost (`tools/dev-proxy/check-caching.sh`). The repo is public: no keys,
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
   staff actions), the shared mailbox (real email) and the enrolment (passkeys in the app).
4. **Access** ([access.md](features/access.md)). Built: roles, assignments, the cached evaluator,
   escalation guards and the admin screens. Open: a private grants feed for offline use in the app.
5. **Configuration:** surfaces, settings descriptors and the generic editor
   ([data-driven.md](architecture/data-driven.md)).
6. **Media**, once its route is approved ([offline-and-caching.md](architecture/offline-and-caching.md)).
7. **Features, page by page:** Home (Hub), Sermons, Events, Life Groups, Ministries, Site map, Prayer,
   Push, Give (link out), About. Each starts from its doc and the matching screens in
   `spikes/app-poc`.
