# Storage

## Postgres

- **One database, a schema per feature** (`accounts`, `access`, `sync`, `events`, …). Each feature's
  Api owns a `DbContext` mapped to its schema, with its own migrations and migrations-history table
  in that schema. Features stay independent (SPEC §8): no foreign keys across schemas; another
  feature's record is referenced by id.
- **Platform tables** live in `sync` (the feed change log and cursors) and in the platform's own
  schemas.
- **Hosts.Migrator applies every feature's migrations** in a fixed order, then exits, before the API
  starts (SPEC §10): in the AppHost, and as an Argo Sync hook on the cluster
  ([environments.md](environments.md)).
- **Each environment has its own database:** the AppHost's container in dev, a Postgres StatefulSet in
  UAT's namespace.
- **Backups** use the owner's existing Postgres backup setup (Databasus on the mini, which UAT's database
  is added to). **A restore gives the database a new sync origin** (`UPDATE sync.origin SET id =
  gen_random_uuid();`), so every phone starts its feeds again ([offline-and-caching.md](offline-and-caching.md)).
- **Every change to a synced record** writes its feed change row and a `NOTIFY` in the same
  transaction (SPEC §5). Platform.Data provides this so a feature can't forget it.

## Blobs: S3-compatible, R2 by default

- **One interface, `IBlobStore`,** over the S3 API (AWSSDK.S3 with a configurable service URL), so the
  provider is configuration.
- **Cloudflare R2** (chosen by the owner, 2026-11-02). Egress is free, and egress is what a church app
  does most (every phone downloading sermon artwork and notes); S3 charges for every byte served. At
  church scale (tens of GB) R2 is likely a dollar or two a month.
- **Two buckets:** public media (behind Cloudflare on a custom domain; see
  [offline-and-caching.md](offline-and-caching.md)) and private files (only through signed URLs).
- **Keys are content hashes** for media (`<sha256>.<extension>`), so a file's URL never changes
  meaning. Uploads go straight to the bucket through presigned URLs; see
  [media.md](../features/media.md).
- **Configuration** is `Storage:Blobs` (service URL, keys, both bucket names, the public base URL), the
  same in every environment. Development runs SeaweedFS from the AppHost; UAT (`gsbc-uat-media`,
  `gsbc-uat-private`) and production point at R2.

## Dropbox

- For things that should stay in the church's existing Dropbox folders (music ministry recordings),
  a feature's Api uses the shared Dropbox client in Platform.Storage. The app never talks to Dropbox
  with credentials; it gets temporary links from a gRPC method.
- Authenticated with a Dropbox app key and a long-lived refresh token, held as a cluster secret.

## Secrets

Database credentials, R2 keys, the Dropbox token, the APNs key and the FCM service account are
cluster secrets, set through `gsbc.argo`. None of them go in this repository: it is public.
