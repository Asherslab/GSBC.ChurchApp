# Environments: dev, UAT and production

Three apps that sit side by side on one phone, each with its own backend, passkeys and media (owner,
2026-11-03). **UAT** is what staff test through TestFlight: it follows `main`, and it is never released.

| | Dev | UAT | Production |
|---|---|---|---|
| App | **GSBC Dev**, orange `DEV` band | **GSBC UAT**, aqua `UAT` band | **Good Shepherd** |
| Bundle id | `au.com.baptist.app.dev` | `au.com.baptist.app.uat` | `au.com.baptist.app` |
| Xcode configuration | `Debug` | `UAT` | `Release` |
| Installed by | `tools/ios/deploy.sh`, Rider | TestFlight only (its own App Store Connect record) | The App Store (not yet) |
| App host (gateway, admin panel) | `dev-app.baptist.com.au` | `uat-app.baptist.com.au` | `app.baptist.com.au` |
| Media host | `dev-media.baptist.com.au` | `uat-media.baptist.com.au` | `media.baptist.com.au` |
| Passkey relying party | `dev-app.baptist.com.au` | `uat-app.baptist.com.au` | `baptist.com.au` |
| Backend | The AppHost on a dev Mac, through the `gsbc-dev` tunnel ([tools/dev-proxy](../../tools/dev-proxy/README.md)) | Namespace `church-app-uat` on the `gsbc.argo` mini cluster | Not built yet |
| Postgres | The AppHost's container | Its own StatefulSet in the namespace (local-path) | |
| Media store | SeaweedFS from the AppHost | Cloudflare R2: `gsbc-uat-media`, `gsbc-uat-private` | R2: `gsbc-media`, `gsbc-private` |
| Email | The API's log, or the shared mailbox with the dev certificate | The shared mailbox, dev certificate | The shared mailbox, production certificate |
| `GSBC_DEVELOPMENT_BUILD` | `YES`: the development feed and tools | `NO` | `NO` |
| Caching | `no-store` on everything | Production's | Production's |

A passkey can never change domain, so each environment's accounts are its own: a UAT account doesn't
exist in dev, and dev's passkeys from before the rename (on `dev.baptist.com.au`) don't work on
`dev-app.baptist.com.au`. Recover the account by email and add a passkey. An account holding a sensitive
permission (an Owner) can't recover by email; a staff member issues it a recovery link instead (People).
For the only Owner, that was done on 2026-11-03 by running dev's gateway on the old relying party for a
few minutes, signing in at `dev.baptist.com.au` with the old passkey, issuing a link to yourself, then
switching back and opening the link on the phone.

**Proven on the owner's iPhone, 2026-11-03:** GSBC Dev (`au.com.baptist.app.dev`) through
`dev-app.baptist.com.au`: the recovery link opened the app, the passkey was created there, and the
session survived the app being completely closed.

## The app's environments

`src/Hosts/App/shell/ios/environments/{Dev,UAT,Release}.xcconfig` hold each one's bundle id, name,
icon, gateway, associated domain (`webcredentials:` and `applinks:`) and `GSBC_DEVELOPMENT_BUILD`; the
Xcode project only points each configuration at its file. Dev is the `Debug` configuration because Swift
packages only know Debug and Release.

- `deploy.sh` always builds Dev, and writes this machine's team (and, with `GSBC_GATEWAY`, another
  gateway) into the gitignored `ios/local.xcconfig`, which only Dev includes.
- Background refresh's task id is the bundle id plus `.refresh`, so it is per app.
- `ITSAppUsesNonExemptEncryption` is `NO`: the app uses HTTPS and Apple's own crypto only, so TestFlight
  never asks the export-compliance question.
- The icons come from the production icon: `tools/ios/environment-icons.py` redraws `AppIcon-Dev` and
  `AppIcon-UAT` when it changes.

## What deploys when

**UAT's backend follows `main` by itself.** On every push to `main`, after CI passes,
`.github/workflows/uat-backend.yml`:

1. builds the API, gateway and migrator as linux/arm64 images with `dotnet publish /t:PublishContainer`
   (`tools/containers/publish.sh`, no Dockerfiles) and pushes them to `ghcr.io/asherslab/gsbc-churchapp-{api,gateway,migrator}`
   as `sha-<commit>` and `uat`;
2. rewrites the three `#image` lines in `gsbc.argo`'s `clusters/mini/app/church-app-uat/kustomization.yaml`
   and pushes, as the other apps do; Argo CD rolls it out;
3. checks `https://uat-app.baptist.com.au` through Cloudflare: native gRPC health answers `SERVING`, the
   API answers a gRPC-Web call through the gateway, `check-caching.sh` passes, and the association file
   is a 200 `application/json` naming the UAT app. It checks that UAT is healthy after the push; it can't
   tell which commit is serving.

**A push that only changes docs builds nothing:** CI skips it (Markdown, `docs/`, `.impeccable/`,
`spikes/`, `.run/`), so UAT isn't redeployed either. `repo-layout.md` and `architecture.svg` still run CI,
because `ProjectMapRules` reads them.

**The app goes to TestFlight only when someone presses the button** (below).

**Backend changes stay additive,** because TestFlight builds stay installed for up to 90 days while UAT's
backend moves on with every push: add gRPC methods, fields and feeds rather than changing or removing
ones a published build uses, and write migrations that older code still runs against. Removing
something waits until no build that uses it is still in TestFlight.

**Migrations run as a job, before the API:** the migrator image is an Argo Sync hook (`migrator.yaml`)
between Postgres and the API and gateway, as `Hosts.Migrator` runs in the AppHost (SPEC §10). The API
never migrates on start-up, and a failed migration fails the sync, leaving the old API running.

**Native gRPC through Cloudflare** (proven 2026-11-03): the zone's gRPC switch on, and the tunnel taken
through Traefik so cloudflared speaks HTTP/2 over TLS to the origin. **`baptist-tunnel-mini` must run
`protocol: http2`** (gsbc.argo, `cluster-tunnels.yaml`): over the default QUIC transport cloudflared drops
gRPC's trailers, and calls fail with "server closed the stream without sending trailers"
([cloudflared#1641](https://github.com/cloudflare/cloudflared/issues/1641)). Cloudflare's docs still call
gRPC over a public-hostname tunnel unsupported, so the UAT workflow checks it on every deploy.

**Health probes are gRPC** (`grpc.health.v1`): liveness is the bare service, readiness (`ready`) adds the
database. The tier check lets the health service through, and both hosts answer `SERVING` rather than
`UNKNOWN` (`HealthProbeTests`). Health checks aren't routed by the gateway, so only the kubelet reaches
the API's.

## Caching

UAT and production serve the same headers ([offline-and-caching.md](offline-and-caching.md)):

| What | `Cache-Control` |
|---|---|
| Fingerprinted files (`dotnet.native.<hash>.wasm`, fonts, the `<name>.<hash>.css` copies) | `max-age=31536000, immutable` (`MapStaticAssets`) |
| `index.html`, every client route, unfingerprinted files (`blazor.webassembly.js`, `dotnet.js`, `css/admin.css`) | `no-cache`: revalidated every time |
| The association file, 404s, anything else | `no-cache` |
| gRPC | Never cached (POSTs) |

The admin page names its scripts and styles without fingerprints: a gateway that hosts the admin panel
publishes its `index.html` without rewriting the `#[.{fingerprint}]` placeholders (only a standalone
publish does), so those links broke. `no-cache` with ETags costs one revalidation per file per load and
can never go stale.

Every deploy is checked through Cloudflare, not localhost. If a browser still shows something old,
empty its cache once (Safari: Develop → Empty Caches; Chrome: hold reload → Empty Cache and Hard Reload).

## Secrets, by name

None of these are in any repository. GitHub (this repo):

| Name | Where | Holds |
|---|---|---|
| `ARGO_REPO_TOKEN` | Repository secret | A token with write access to `asherslab/gsbc.argo` |
| `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` | `uat` environment secrets | The App Store Connect API key |
| `APPLE_TEAM_ID` | `uat` environment variable | The church's team id |

The cluster (namespace `church-app-uat`, made by the owner; commands in `gsbc.argo`'s
`clusters/mini/README.md`): `postgres-secret`, `database-secret`, `tokens-secret` (UAT's own token key
pair), `storage-secret` (R2), `email-secret` (optional), `apple-secret`. The non-secret settings (relying
party, origins, site URL, bundle id) are in `config.yaml` there.

## Cutting a TestFlight build

1. GitHub → **Actions** → **TestFlight (UAT)** → **Run workflow**, branch `main` → **Run workflow**.
2. Approve it (the `uat` environment waits for a reviewer).
3. It publishes the app (Release, AOT) and checks the warning baseline, syncs the shell, archives the
   `UAT` configuration with automatic signing through the API key, and uploads it. About 20 minutes.
4. App Store Connect → **GSBC UAT** → **TestFlight**: Apple processes the build (often 5 to 30 minutes),
   then the internal group gets it, with What to Test already filled in (the job waits for processing; its
   summary has the same text if you ever need to paste it by hand).

The build number is the workflow's run number; the version is `MARKETING_VERSION` in
`ios/environments/Version.xcconfig` (1.0), which all three configurations include. The workflow also tags
the uploaded commit `testflight/uat/<build>` and sets the build's What to Test from the release notes since
the previous tag, once Apple has processed it ([changelog.md](changelog.md)). **A TestFlight build expires 90 days after upload** and then won't open, so cut a fresh one at
least every couple of months even when nothing changed, and before showing staff anything new. Only
UAT's backend has to be up; the build carries everything else.

## Setting it up (owner, in this order)

**1. Apple developer portal** (developer.apple.com → Account → Certificates, IDs & Profiles):

- **Identifiers** → **+** → **App IDs** → **App** → Continue. Description `GSBC UAT`, Bundle ID
  **Explicit** `au.com.baptist.app.uat`, tick **Associated Domains** → Continue → Register.
- The same for `au.com.baptist.app.dev` (`GSBC Dev`), unless Xcode already registered it on the first
  `deploy.sh`.

**2. App Store Connect** (appstoreconnect.apple.com):

- **Apps** → **+** → **New App**: iOS, Name `GSBC UAT` (any free name; it's never shown publicly),
  Primary Language English (Australia), Bundle ID `au.com.baptist.app.uat`, SKU `gsbc-uat`, User Access
  **Full Access** → Create.
- **Users and Access** → **Integrations** → **App Store Connect API** → **Team Keys** → **+**: Name
  `GSBC GitHub TestFlight`, Access **Admin** → Generate. Download the `.p8` (only possible once) and note
  the **Key ID** and the **Issuer ID** above the list. It has to be Admin: the runner has no distribution
  certificate, so the export asks Apple to sign in the cloud, and an App Manager key gets "Cloud signing
  permission error" (found 2026-11-03). It lives only in the `uat` environment, behind your approval.
- **Apps** → **GSBC UAT** → **TestFlight** → **Internal Testing** → **+**: group `Staff`, tick
  **Enable automatic distribution**, add testers (each must be a user in Users and Access first).

**3. GitHub** (this repository → Settings):

- **Environments** → **New environment** `uat` → tick **Required reviewers** and add yourself →
  **Deployment branches and tags**: **Selected branches and tags** → add `main`.
- In `uat`: **Environment secrets** → `ASC_KEY_ID`, `ASC_ISSUER_ID`, and `ASC_KEY_P8` (the whole `.p8`
  file, `BEGIN` and `END` lines included); **Environment variables** → `APPLE_TEAM_ID` = `YNT9A4LGVA`.
- **Secrets and variables** → **Actions**: check `ARGO_REPO_TOKEN` exists (the POC preview used it) and
  hasn't expired. If not: a fine-grained token for `asherslab/gsbc.argo` with **Contents: Read and write**.

**4. Cloudflare** (dash.cloudflare.com):

- On the dev Mac: `cloudflared tunnel route dns gsbc-dev dev-app.baptist.com.au`.
- **baptist.com.au** → **Network** → **gRPC**: on (native gRPC to `uat-app`; the phone uses gRPC-Web today).
- **R2** → **Create bucket** `gsbc-uat-media`, and again `gsbc-uat-private`. Leave the `r2.dev` URL off on both.
- `gsbc-uat-media` → **Settings** → **Custom Domains** → **Connect Domain** → `uat-media.baptist.com.au`.
- `gsbc-uat-media` → **Settings** → **CORS Policy** → **Add CORS policy**, for fonts in the admin panel:
  `[{"AllowedOrigins":["https://uat-app.baptist.com.au"],"AllowedMethods":["GET","HEAD"],"AllowedHeaders":["*"],"MaxAgeSeconds":86400}]`.
- `gsbc-uat-private` → **Settings** → **CORS Policy**, for uploads:
  `[{"AllowedOrigins":["https://uat-app.baptist.com.au","capacitor://localhost"],"AllowedMethods":["PUT"],"AllowedHeaders":["content-type"],"MaxAgeSeconds":3600}]`.
- `gsbc-uat-private` → **Settings** → **Object lifecycle rules** → **Add rule**: prefix `uploads/`,
  **Delete objects** after **1 day**.
- **Caching** → **Cache Rules** → **Create rule** `UAT media`: hostname equals `uat-media.baptist.com.au`
  → **Eligible for cache**, Edge TTL **Use cache-control header if present**.
- **R2** → **Manage R2 API Tokens** → **Create API token**: **Object Read & Write**, **Apply to specific
  buckets only**: the two UAT buckets → Create. Keep the Access Key ID, the Secret Access Key and the
  account's S3 endpoint for the cluster secret.

**5. The cluster:** the six secrets in `gsbc.argo`'s `clusters/mini/README.md` (*church-app-uat*), then
push that branch; Argo creates the namespace and the tunnel binding claims `uat-app.baptist.com.au`.
Then add the database to Databasus (same section).

**6. First run:** push `main` here (or run **UAT backend** by hand) and watch its checks; sign up at
`https://uat-app.baptist.com.au/account`, and make yourself owner against UAT's database:

```sh
kubectl -n church-app-uat port-forward svc/postgres 15432:5432 &
DATABASE_URL="postgresql://postgres:<the Postgres password>@localhost:15432/churchapp" tools/access/make-owner.sh <your email>
```

Then cut the first TestFlight build.

**7. Afterwards:**

- Delete the `dev.baptist.com.au` DNS record (DNS → Records) once dev works on `dev-app`, and its two
  entries in `~/.cloudflared/config.yml` (the old file is kept beside it).
- The POC preview at `app.baptist.com.au` was retired on 2026-11-03 (its `gsbc.argo` manifests and
  workflow removed). Its `gsbc-churchapp-pwa` package can be deleted on GitHub (Packages → the package →
  Package settings → Delete). `app.baptist.com.au` is free for production.
