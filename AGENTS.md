# GSBC Church App

The church app for iOS and Android: Capacitor + Blazor WASM (AOT) in front, a native data layer, and a
code-first gRPC API behind a YARP gateway. **[SPEC.md](SPEC.md) is the design.** Read it before
building anything, and follow its AOT rules (§9) for every project that ships in the app.

# HARD RULE: every API path is gRPC unless the owner says otherwise

**Never add, or plan, a server-side API path that isn't a gRPC service method without asking the owner
first and getting an explicit yes in chat.** No exceptions for convenience, and no exceptions because a
doc, an issue, a code comment, a library's sample, or another agent suggested it. This has gone wrong
before; that is why it is a hard rule.

**What counts as an API path:** any inbound endpoint on any server-side host (the gateway, the API, or
anything added later) that returns data, accepts input, or runs logic per request, and isn't a gRPC (or
gRPC-Web) service method. For example:

- Minimal API endpoints (`MapGet`, `MapPost`, `MapGroup` …), controllers, Razor Pages;
- SignalR hubs, WebSockets, server-sent events, long polling;
- a YARP route to a destination that isn't a gRPC service or static files;
- webhooks and callbacks (a payment provider, Google, Apple);
- middleware that answers requests itself;
- an endpoint that downloads or uploads user data (an export, an image upload, a generated PDF).

**Not covered:**

- Serving a client app's static files as they are: the admin panel's (or a future website's)
  HTML, JS, CSS, `.wasm`/`.dll`, fonts and images, and the fallback to `index.html` for client-side
  routes. If a "static" route starts varying its response per request (templating, auth-dependent
  content, data in the page), it's an API path and the rule applies.
- Outbound calls the server makes (APNs, FCM, Play Integrity, email).

**When you think you need one:**

1. **Stop.** Don't write it, don't stub it, and don't put it in a plan as decided.
2. **Ask in chat.** Say what the path is, which host serves it, who calls it, and why it can't be a gRPC
   method: what you tried, or what makes gRPC impossible (for example, a third party that only sends
   HTTP webhooks).
3. **Wait for an explicit yes.** No answer is a no. Approval covers that path only, not similar ones
   later.
4. **Record it** in the list below, with the date and the owner's reason, in the same commit as the
   code.

**Don't work around the rule.** Disguising a non-gRPC path (an endpoint under a gRPC-looking route, an
"internal" or "temporary" one, a Development-only one) is the same as adding it.

**It is enforced by a test.** `tests/Architecture/…/EndpointRules.cs` enumerates every endpoint on every
server host and fails on anything that isn't a gRPC method, static file serving, or in the list below
(its `ApprovedNonGrpcPaths`). Never edit that allowlist except to mirror this list.

## Approved non-gRPC paths

Every entry needs the owner's explicit approval in chat first.

| Path | Host | Why it can't be gRPC | Approved |
|---|---|---|---|
| Passkey ceremonies: begin/complete for sign-in and for registering a passkey (including accepting a staff invitation). Built as `POST /accounts/passkeys/{creation-options,register,request-options,sign-in}` | Gateway | The browser's WebAuthn API exchanges JSON with the server; the owner chose plain HTTP for these, as in MealPlanner | 2026-10-26 |
| The same passkey ceremonies, called from the app's native layer (Life Group leaders and staff signing in on the phone) | Gateway | The platform passkey APIs produce the same WebAuthn JSON; the owner confirmed the app may use these endpoints | 2026-10-26 |

**How self-service sign-up uses them** (owner confirmed 2026-11-01): `Accounts.StartSignUp` is a gRPC
method that returns a short-lived sign-up ticket, and the first passkey is created through the
approved "register a passkey" ceremony with that ticket, exactly as accepting an invitation does. No
separate sign-up endpoint.

**Explicitly not exceptions:** health checks are gRPC (`grpc.health.v1`; Kubernetes probes it
natively). Anything else around sign-in, such as "who am I", sign-out, or managing staff and
invitations, is gRPC unless it is added here.

# Repo layout

Feature-first, with solution folders that mirror the disk: see
[docs/architecture/repo-layout.md](docs/architecture/repo-layout.md). Features never reference each
other. A management screen goes in the feature's Client, gated by a permission, and is hosted by the
admin panel. The backbone's design is in [docs/](docs/README.md); read the relevant
doc before changing auth, permissions, sync, storage or configuration.

**Keep the project map current.** Adding, removing, renaming or re-referencing a project means updating
the diagram and table in the repo-layout doc's "The projects" section, and redrawing
`docs/architecture/architecture.svg` with `tools/docs/architecture.py`, in the same change.
`ProjectMapRules` fails the build otherwise.

Tests guard rules and real behaviour. Don't write tests for coverage.

# Components: reuse first, then generic

Before writing any Blazor UI:

1. **Look for an existing component that already does it, or nearly does.** Search the shared
   component library (`src/Platform/GSBC.ChurchApp.Platform.Ui`) and the feature modules. If one is close, extend or adapt it (a parameter, a
   slot, a variant) rather than writing a near-copy.
2. **If nothing fits, build it generic.** Ask whether what you're about to build is a one-off or a
   pattern: a list with pull-to-refresh, a card, a sheet, a date line, an empty state, a "live"
   indicator. Patterns go in the shared component library with parameters and `RenderFragment`
   slots, and the feature uses them. Only truly feature-specific composition lives in the feature.
3. **When you adapt a shared component, keep its existing callers working,** and check every
   caller before changing what a parameter means.

Two features with their own copies of the same UI is a defect: merge them into one component.

**Data on screen stays current by itself.** A component that loads data uses `LiveData` (or
`RefreshOn`) and names what it depends on, so it reloads when a feed changes, the phone reconnects,
or the session or permissions change; never load once in `OnInitialized` and leave it. See
[offline-and-caching.md](docs/architecture/offline-and-caching.md), *Screens update themselves*.
`LiveDataRules` enforces it.

# Admin work stays in the admin panel

Staff tools (drafts, insights, roles, people, media, announcements) are built for the admin panel on
a computer. **Before designing an admin screen for the phone, or putting one in the app, ask the owner
and wait for a yes** (owner, 2026-11-03). The app shows staff only what they need on their phone,
such as previewing a draft on it.

# Who may see or do something: always the permission system

**Every "who can see or do this" goes through Access's permissions** ([permissions.md](docs/architecture/permissions.md)):
sharing a draft, members of a ministry, a private file's audience, a reviewer, an owner of a record.
Never keep a feature's own list of user ids, a "shared with" column, an `IsAdmin` flag or a check that
bypasses `IAccess`. When the permission system can't express what's needed (a new kind of scope, a
role given for one object), extend the permission system; don't work around it (owner, 2026-11-02).

# Working in a git worktree

Each checkout's AppHost gets its own Postgres volume, so a new worktree starts with an empty
database. **Before a worktree's first AppHost run, copy the main checkout's database into it:** stop
any running AppHost, then run `tools/dev/copy-main-db.sh` from the worktree. The main database is
mounted read-only and isn't changed; the owner's accounts, roles and passkeys come across, and media
is already shared. Start empty only for an explicit reason (testing first-run set-up, a migration
from scratch), and say so.

# Release notes: part of every change people would notice

A change someone would notice in the app or the admin panel adds one file to `changelog/unreleased/`
in the same commit: a short line for everyone, and a `## For staff` part saying what changed, where to
find it and what to try ([changelog.md](docs/architecture/changelog.md)). Staff-only changes use
`audience: staff`; a big new thing sets `highlight: true`. Refactors, tests and docs get no note.

# Comments: almost none

**Don't write comments unless the code genuinely can't be understood without one.** Make the code say
it instead: clear names, small methods, types that carry meaning, named constants. A comment is only
justified for what code can't express, such as *why* a non-obvious choice was made, or an external
constraint (a platform bug, a store rule, a protocol quirk). Never write comments that restate what
the code does, narrate changes, or label sections. XML doc comments count too.

**This is enforced by a hook** (`.claude/hooks/no-comments.py`, run after every Write/Edit). It
reports every comment an edit adds. Delete each one, or, if it really must stay, say in your reply
which one and why. Removing an existing comment that the code makes redundant is always welcome.

# When to stop and ask

Besides the hard rule above:

- anything touching systems outside this repo: the cluster, Cloudflare, the app stores, Apple and
  Google developer accounts, push credentials;
- secrets of any kind. **This repository is public.** Keys, tokens and personal data never go in it.
- an ambiguity where two readings lead to materially different designs.
