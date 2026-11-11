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
| iCal links for calendar apps: `GET /calendars/{calendar}.ics` (a calendar for everyone), `GET /calendars/public.ics` (every calendar for everyone), `GET /calendars/mine/{secret}.ics` (everything the link's holder has added and been given), `GET /calendars/mine/{secret}/{calendar}.ics` (one calendar through the holder's link) | Gateway | Calendar apps (iPhone, Google, Outlook) only fetch calendars over plain HTTP; the owner chose one standard method for every calendar, public or private | 2026-11-08 |
| Website embeds: the public, read-only Events and Sermons pages the church website shows in iframes. Built as `GET /events`, `/events/{Id}`, `/sermons`, `/sermons/series`, `/sermons/series/{Id}`, `/sermons/speakers`, `/sermons/speakers/{Id}`, `/sermons/{Id}`; the same for everyone, no forms, writes, per-person content or JSON | Website (`embed.baptist.com.au`, its own container) | An iframe on the Tithe.ly website loads HTML; the owner chose server-rendered pages read from the public feeds (docs/features/website.md) | 2026-11-08 |

**How self-service sign-up uses them** (owner confirmed 2026-11-01): `Accounts.StartSignUp` is a gRPC
method that returns a short-lived sign-up ticket, and the first passkey is created through the
approved "register a passkey" ceremony with that ticket, exactly as accepting an invitation does. No
separate sign-up endpoint.

**Explicitly not exceptions:** health checks are gRPC (`grpc.health.v1`; Kubernetes probes it
natively). Anything else around sign-in, such as "who am I", sign-out, or managing staff and
invitations, is gRPC unless it is added here.

# HARD RULE: the app reads data only through Sync

**The phone never loads data from the server with a direct call** (owner, 2026-11-10: "nothing should do that ...
make sure that's a hard rule. only explicit exceptions should ever bypass that"). In code that ships in the app
(`Hosts.App` and every project it references: each `*.App`, each `*.Client`, `Platform.Client` and `Platform.Ui`):

- **Reads come from the local store** (`ILocalFeeds`), fed by Sync. Everything a person may see is synced in the
  background, so a screen opens at once and works offline. A screen never shows a blank "can't load" page for data
  it could have had.
- **Writes go through the outbox** (`IOutbox`, or `IWrites`, which queues on the phone and sends directly in the
  admin panel). They show as waiting to send (`WaitingToSend`) and retry by themselves. Anything new gets its id on
  the phone, so a retried send is recognised as already done.
- **A direct call is anything else that reaches the server:** a gRPC proxy (`CreateGrpcService`) or call invoker
  made by the code, or a raw native call. Sync itself and the outbox worker are the transport, not direct calls.
- **Only an exception the owner has approved may make one.** Same process as approved non-gRPC paths: stop, ask the
  owner in chat (which call, from which screen, and why Sync or the outbox can't do it), wait for an explicit yes,
  and record it below with the date, in the same commit as the code.

**The admin panel is not covered:** it is online only by design (docs/architecture/offline-and-caching.md, *The
admin panel*). Code in `*.Client` that only the admin panel uses is marked `[AdminOnly]` (Platform.Client); app
code may never reach it, and anything shared depends on an interface the phone fills from the store.

**It is enforced by a test.** `tests/Architecture/…/SyncOnlyRules.cs` finds every direct call in app code and fails
on anything not in its `ApprovedDirectCalls` (which mirrors the list below) or its `NotYetOnSync` list, and on app
code that reaches `[AdminOnly]` code. Never edit `ApprovedDirectCalls` except to mirror this list. **`NotYetOnSync`
is temporary, empty by design and not an approval:** it only names calls waiting for a change already under way, and
each entry says which.

## Approved direct calls from the app

Every entry needs the owner's explicit approval in chat first.

| What | Calls | Why it can't go through Sync or the outbox | Approved |
|---|---|---|---|
| Sign-in and account actions: sign up, redeem a family or invitation code, account recovery, resend or confirm an email code, sign out (ending this or another device's session) and sign out everywhere, delete my account, register the phone for push and claim the install, make a child's sign-in code | `IAccountsService.Redeem`, `StartSignUp`, `RequestRecovery`, `ResendConfirmation`, `ConfirmEmail`, `SignOutEverywhere`, `EndSession`, `DeleteMyAccount`; `IInstallsService.Claim`; `INotificationsService.RegisterDevice`; `IPeopleService.MakeSignInCode` | Each needs the server's yes or no at that moment and means nothing offline | 2026-11-10 |
| Links: signed links to private pictures and files, your private calendar-subscription link and a new one, opening a link to something outside the app | `IMediaService.GetLink`; `ICalendarFilesService.Link`, `ResetLink`; `IDestinationsService.Describe` | The server hands out a link; there is nothing to store | 2026-11-10 |
| Leader lookups: searching the church's people to add someone to a ministry, the "what goes" previews before deleting or moving a ministry or removing its calendar, and *Tell people* | `IMinistryLeadingService.Find`, `Deletion`, `Moving`, `CalendarRemoval`; `IEventEditingService.TellPeople` | Searching everyone's names can't be synced to every phone, and a preview must be the server's current answer | 2026-11-10 |
| Account security changes: changing your name and removing a passkey (ending a device's session is above); your account itself syncs and shows offline, read only | `IAccountsService.UpdateName`, `RemovePasskey` | The owner: "Account and security is read only offline" | 2026-11-11 |
| Event editors: browsing the Media library beyond what's synced, online only | `IMediaService.List`; `IMediaFoldersService.List` | The owner: "those that *are* synced already (as some will be) can be browsed offline. otherwise online" | 2026-11-11 |

**Uploads are not an exception** (narrowed 2026-11-11; owner: "uploads can work offline? keep it and send it when
online?"): the app queues a file with the save that uses it, and only the native outbox worker calls
`IMediaService.FindByHash`, `BeginUpload` and `CompleteUpload` (docs/architecture/offline-and-caching.md, *Files in the
outbox*). No screen may call them; the test's `MadeOnlyByTheOutboxWorker` checks it. The admin panel's direct upload is
`[AdminOnly]`.

**Not yet on Sync** (temporary; the test's `NotYetOnSync`): nothing. The list stays empty; it is never an approval.

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

# Tests

How tests get a database, S3 buckets and the time, what they may not touch, and how to run a subset
are in [testing.md](docs/architecture/testing.md). In short: each test gets its own database and
buckets from `GSBC.ChurchApp.Testing` (never its own container), reads time from `TestClock`, and waits
for the signal it expects, never for time to pass. **A flake is a bug:** find the cause and fix it,
never retry, skip, lengthen a timeout or blame load. `TestRules` enforces what it can.

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

**Nothing typed is lost without asking.** A form that saves uses `SaveBar` and `UnsavedGuard` (or, if it
saves by itself, `SaveStatus` and the guard's autosave mode); see DESIGN.md, *Saving*. `SaveRules` enforces it.

**Data on screen stays current by itself.** A component that loads data uses `LiveData` (or
`RefreshOn`) and names what it depends on, so it reloads when a feed changes, the phone reconnects,
or the session or permissions change; never load once in `OnInitialized` and leave it. See
[offline-and-caching.md](docs/architecture/offline-and-caching.md), *Screens update themselves*.
`LiveDataRules` enforces it.

# Create means delete

**If staff can create something, they can delete it** (owner, 2026-11-09): a speaker, a series, a custom
service, a calendar, a folder, a tile, anything. Build the delete in the same change as the create, decide what
happens to whatever uses it (move it elsewhere, leave it without, or explain why it can't go yet), and ask before
deleting anything that can't be undone.
People can be deleted too (owner, 2026-11-09): a feature that keeps anything about a person (their id, their contact
details, their progress) implements `IPersonData` (Platform.Api) so deleting an account forgets it
([accounts.md](docs/features/accounts.md), *Deleting an account*).

# Where staff and leader tools live: the phone or the admin panel

Ask: **would someone need to do this away from a desk, as part of looking after something they're responsible for?**
(owner, 2026-11-11, replacing "Admin work stays in the admin panel" of 2026-11-03).

- **On the phone, and in the admin panel too:** the day-to-day running of something. Answering and approving (join
  requests, roster swaps, prayer wall requests), posting and sharing (news, files, a quick event change, telling
  people), looking after your own people (adding someone, linking a parent, giving a role in your ministry) and your
  ministry's own settings. Built for lay leaders and approvers who may never open a computer.
- **Admin panel only:** setting up and looking after the whole church's app. Who can do what (roles, permissions,
  accounts), how the app looks (styling, Home, tiles), the media library, imports, analytics and health, bulk and
  planning work (sending all invitations, planning the year's Life Groups, the sermon library), and dense work with
  many fields, filters, tables or history (the full pastoral inbox).
- **When it isn't clear, ask the owner and wait for a yes** before building it.

**Exception, Prayer** (owner, 2026-11-11: "Prayer wall approvals and such should be able to be done on the phone"):
people holding *Prayer wall* or *Pastoral team* approve wall requests and do the pastoral team's everyday steps on the
phone, through Sync and the outbox ([prayer.md](docs/features/prayer.md), *On the phone for staff*). The full inbox
stays in the admin panel.

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

To run a worktree's backend **beside** the owner's instead of stopping it, start the AppHost with
`Dev__PortOffset=100`: its fixed ports move up by 100 (the gateway on 5400), it starts no Cloudflare tunnel and
its media store gets its own volume. The copy script needs the main database stopped, so copy with `pg_dump`
from the main Postgres container instead (read-only), and give the copy a new `sync.origin`. Give it its own
dashboard ports too (`ASPNETCORE_URLS`, `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL`, `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`),
and to sign in on `http://localhost:5400` set `Accounts__Passkeys__RelyingPartyId=localhost` and
`Accounts__Passkeys__AllowedOrigins__0=http://localhost:5400`. Stop it by its own process id, never by pattern.

**Clean up merged worktrees.** Each worktree costs 1–3 GB on disk (builds, the app shell) plus a Postgres volume,
and 40 of them once filled the owner's disk (2026-11-09). Once a branch is merged, run `tools/dev/prune-worktrees.sh`
from the main checkout (`--dry-run` to see what it would do). It removes only worktrees whose branch is fully merged,
has no local changes and has nobody working in it, keeps the branches, and removes Postgres volumes no checkout
uses. Never remove a worktree with `--force`; keep anything uncommitted you want first. Put screenshots and other
throwaway files in your scratchpad, not the worktree.

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

# Looking at errors

When an error report comes up (an email from App health, a failure in dev or UAT), read it yourself with
`tools/health/errors.sh --env dev|uat` (list) and `tools/health/errors.sh --env <env> <id>` (one in full)
before asking the owner for details. It needs the owner to have connected a tool once
(`tools/health/connect.sh <env>`); if it says no tool is connected, ask them to. See
[analytics.md](docs/features/analytics.md), *Reading App health from the terminal*.

# When to stop and ask

Besides the hard rule above:

- anything touching systems outside this repo: the cluster, Cloudflare, the app stores, Apple and
  Google developer accounts, push credentials;
- secrets of any kind. **This repository is public.** Keys, tokens and personal data never go in it.
- an ambiguity where two readings lead to materially different designs.
