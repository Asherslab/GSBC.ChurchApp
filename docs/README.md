# Docs

[SPEC.md](../SPEC.md) is the design. These docs hold the detail.

## Architecture (the backbone)

| Doc | Covers |
|---|---|
| [repo-layout.md](architecture/repo-layout.md) | Folders, projects, naming, a feature's parts, the rules the tests enforce |
| [identity.md](architecture/identity.md) | One passkey sign-in for app and admin; sessions; recovery |
| [permissions.md](architecture/permissions.md) | Nodes, scopes, roles as data, how the server and the UI check |
| [offline-and-caching.md](architecture/offline-and-caching.md) | Sync as the only data path; media that never goes stale; Cloudflare |
| [storage.md](architecture/storage.md) | Postgres schemas per feature, R2 over the S3 API, Dropbox |
| [data-driven.md](architecture/data-driven.md) | What staff can reshape without a release, and how |

## Features

One doc per feature in [features/](features/), written before its code.

## Build order

Linchpins first, then outwards, one feature at a time and tested on a phone before the next
(requirement F). iOS first; Android's native side stays stubbed until it is built properly.

1. **Skeleton** (done 2026-11-01): the layout, the AOT rules and baseline, the gRPC-only and tier
   tests, Sync's change log and `NOTIFY` trigger, the gateway routing gRPC and gRPC-Web, one
   end-to-end call (`Accounts.Session.GetCurrent`).
2. **Native data layer** on a real iPhone (built: transport, GRDB store, pull and live `Watch` with
   `LISTEN` fan-out, outbox, the Storage screen and the dev loop; still to do: background refresh,
   per-feed caps, the server's retention and reset horizon, and the soak test. Silent push waits for
   the Apple enrolment and an APNs key), through Cloudflare (SPEC §5's five open points): Transport,
   Store, Sync and Outbox in Swift; the bridging `CallInvoker`; `Watch` fan-out from `LISTEN`.
3. **Accounts:** passkey ceremonies at the gateway (browser and native), sessions, the
   session-to-token swap, invitations and recovery ([identity.md](architecture/identity.md)).
4. **Access:** roles, assignments, the cached evaluator, the admin screens
   ([permissions.md](architecture/permissions.md)).
5. **Configuration:** surfaces, settings descriptors and the generic editor
   ([data-driven.md](architecture/data-driven.md)).
6. **Media**, once its route is approved ([offline-and-caching.md](architecture/offline-and-caching.md)).
7. **Features, page by page:** Home (Hub), Sermons, Events, Life Groups, Ministries, Site map, Prayer,
   Push, Give (link out), About. Each starts from its doc and the matching screens in
   `spikes/app-poc`.
