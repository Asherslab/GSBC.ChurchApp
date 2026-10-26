# GSBC Church App: spec

Status: draft, 2026-10-26. A starting point for discussion, not a commitment.

## 1. Why

Good Shepherd Baptist Church uses Tithe.ly for its app. It works, but it is clunky. It is generic,
slow to update and hard to make feel like *our* church. We want our own app that:

- feels native on iOS and Android, and works offline;
- puts the things people actually open first: this Sunday, sermons, events, groups and giving;
- lets church staff change content (events, announcements, sermons) the moment they need to,
  **without a store update**;
- is cheap to run on infrastructure we already have (the `gsbc.argo` cluster).

Non-goals for v1: a full church management system (people database, rostering, accounting), live
streaming infrastructure, and native UI. Native code is limited to one small, generic data layer
(§5).

## 2. Shape

One web app, packaged for both stores from a single build:

| Target | How | Updates |
|---|---|---|
| iOS | Capacitor shell (WKWebView) | Code and design: App Store. Data: live from the API |
| Android | Capacitor shell (Android WebView) | Code and design: Play Store. Data: live from the API |
| Web (later, maybe) | A separate host with a chosen subset of features (§8) | Ordinary deploys |

The app is not published as a website for now. The only public site is the staff admin panel (§10).

**Capacitor, with one native plugin of our own.** Nothing in scope needs native UI. Device features
come from official plugins: Push Notifications, Haptics, Share, Browser, App (Android back button,
deep links), Preferences and Splash Screen. The one plugin we write is the data layer (§5), in Swift
and Kotlin. It is generic, so it is written once per platform, not once per feature.

**UI stack:** Blazor WebAssembly, **AOT-compiled**. It reuses our existing GSBC stack (ImpactKids,
Accounting) and the patterns proven in the Graced POC: native-feel navigation, safe areas and
offline-first. The app is bundled in the store build, so AOT's larger download costs nothing at run
time and buys a faster start-up.

**API:** code-first gRPC (protobuf-net.Grpc). In the store apps, the native data layer makes the
calls over native gRPC (HTTP/2). Browsers, meaning the admin panel and any future website, use
gRPC-Web (§10). The app is AOT-compiled and AOT-checked from the first commit (§9).

**Modular:** every feature is a set of modules that a host opts into: the app, the admin panel and any
future public website (§8).

**Not AOT:** everything server-side (the gateway, the API and the migrator) and the admin panel.
AOT is where it pays: the app on a phone. On the server it would cost us EF Core (§10) and passkey
sign-in, for a start-up gain nobody would notice.

Downloading Blazor's compiled code (`.wasm`/`.dll`) over the air is a grey area under the store
rules, but this plan never does it (§4), so it is not a constraint.

## 3. Features (v1)

- **Home:** this Sunday (service times, what's on, the bulletin), plus a pinned announcement and
  quick links.
- **Sermons:** a series → sermon list with video (YouTube embed) or audio (podcast feed), notes and
  passages. Last-played position is remembered.
- **Events:** a list and calendar, with "add to calendar" and a registration link where one is needed.
- **Groups:** life groups and ministries, each with a contact or "I'm interested" button.
- **Give:** see §7, which is the most constrained feature.
- **Prayer requests:** submit privately to the pastoral team, with an optional public prayer wall.
  Never public by default.
- **Push notifications:** announcements, "service starting", and opt-in topics such as Youth, Kids
  and Prayer.
- **About / contact:** location, map, staff and beliefs.

No accounts are required to read anything. Sign-in, if added, is only for things that are personal
(prayer requests you sent, group membership).

## 4. What changes how

There are two kinds of change, and each has one route into the app.

| Change | Route | Examples |
|---|---|---|
| **Data** | The API, live | Events, announcements, sermons, groups, prayer wall, service times, staff |
| **Code and design** | A store release | New pages, new features, layout, colours, fonts, logo |

- **Data** is records from the church API (§10), edited by staff in an admin UI. It appears in the app
  the next time it fetches, so no release is needed. The screens are fixed; only what fills them
  changes.
- **Design** is built into the app. Colours and fonts are still CSS custom properties
  (`--colour-primary`, `--font-heading` and so on) so they are easy to change, but changing them
  means a release. This is a deliberate choice: a release keeps the app reviewable, testable and
  simple.
- Store reviews typically take a day or two now, and both stores support phased rollouts, so a
  release is routine.

**Store rules:** serving data from an API is ordinary app behaviour, and neither store restricts it.
The rules only bite on downloading *code* (Apple Guideline 2.5.2 and DPLA §3.3.1(B); Google Play's
Device and Network Abuse policy), which this plan does not do.

If we ever want over-the-air *styling*, the safe route is a theme record from the API mapped onto
the existing CSS variables. That stays data, not code. It is not planned for v1.

## 5. Data in the app: the native data layer

The store apps get a thin native layer that owns the network, storage and background work. The
Blazor UI talks to it through one Capacitor plugin.

### Why native

- **Storage:** Capacitor's own storage guide says the OS can reclaim WebView storage on a device low
  on space, IndexedDB included on iOS, and recommends SQLite for anything that matters. A native
  SQLite store survives.
- **Background sync:** the WebView, and with it our .NET code, doesn't run in the background.
  Capacitor's Background Runner runs plain JavaScript only (no WebView, no WASM, about 30 seconds per
  run on iOS). Syncing while the app is closed means native code.
- **Native gRPC:** native code gets real HTTP/2 gRPC, including client and two-way streaming, which
  `fetch` can't do (§10).

### Keep the native side generic

The risk with a native layer is that every feature grows Swift and Kotlin code alongside its C#.
That would triple the work and break the modularity in §8. So the native side **never understands a
message**: payloads stay protobuf bytes that only C# encodes and decodes. Contracts, models and
logic stay in one place, in C#, and native gRPC libraries need no `.proto` code generation, because
they only move bytes.

The plugin does four things:

| Part | What it does | What it knows |
|---|---|---|
| **Transport** | Makes a gRPC call: method path + request bytes in, response bytes (or a stream of them) out. HTTP/2 via grpc-swift / grpc-java with a pass-through byte codec | URLs, auth headers, retries |
| **Store** | SQLite (the platform's own: GRDB on iOS, Room on Android; there's no .NET on the native side, and two generic tables need no more) holding `(feed, id, version, payload)` plus a few generic index columns the server fills in per feed (`sort_key`, `starts_at`, `ends_at`, `group_key`), and a sync cursor per feed | Nothing about the payload |
| **Sync** | Gets changes from one generic `Sync` service, writes them to the store, then tells the UI which feeds changed. **While the app is open it holds a live stream** (below). When closed, it pulls from a silent push and from the OS's background scheduler (BGTaskScheduler / WorkManager) | Feed names |
| **Outbox** | Queues writes made offline (a prayer request at church with no signal) and sends them through Transport when the network returns, in the background if need be | Method paths |

On the .NET side:

- **A `CallInvoker` that bridges to Transport.** protobuf-net's generated proxies run on top of any
  `CallInvoker`, so feature code calls `IEventsService` exactly as it would over gRPC-Web. The
  serialized bytes simply go through the plugin instead of `fetch`.
- **Each feature reads its feeds from the Store a page at a time,** filtered and ordered by the index
  columns, and decodes only that page with its own contracts. Nothing loads a whole feed into memory
  to filter it.
  Screens show stored data immediately and re-render when Sync reports their feed changed.
- **Feature modules declare their feeds on the server** (§10), so adding a feature adds no native code.

### Bounded, by design

A store that only accumulates opaque payloads is a slow leak unless every way in has a way out. The
rules:

- **The server owns retention.** Each feed declares its window: events from 30 days ago onward,
  the latest N sermons, announcements until they expire. A record leaving its window is sent as a
  delete, exactly like a real deletion, so every phone converges on the server's window.
- **Deletes delete.** The phone removes the row when the delete arrives; it never keeps tombstones.
  The server keeps them for a fixed period (say 90 days). A phone whose cursor is older than that gets
  "reset" instead of changes: it empties that feed and fetches it fresh. A phone that hasn't opened the
  app since Easter can't replay a year of history.
- **The phone only stores feeds it asks for.** Each sync names the feeds this app version registers.
  On start-up, rows for any feed the app no longer registers (a feature removed in an update) are
  dropped.
- **Format changes reset a feed** rather than leaving undecodable rows. Payload changes are additive
  (§10), so this is rare.
- **Hard caps as a safety net:** a per-record size limit enforced by the server (say 64 KB), and a
  per-feed row and byte cap on the phone. If a cap is hit, the oldest rows go and it is logged, because
  it means a feed's window is wrong.
- **Images aren't in the store.** They live in a separate file cache with its own size limit, evicting
  the least recently used.
- **Space comes back.** SQLite with `auto_vacuum = INCREMENTAL`, run after large deletes, so deleted
  rows return disk space rather than leaving an ever-growing file.
- **The outbox drains or gives up.** Entries retry with backoff, give up after a limit (days, not
  forever), and the user sees "couldn't send" with a retry, rather than a queue growing silently.
- **Memory:** bridge messages are paged and size-limited, and streams arrive in chunks.
- **Proven, not assumed:** a soak test replays months of simulated changes (window churn, deletes,
  resets, an app update that drops a feed) and asserts the store's rows and file size stay bounded.
  Settings shows the space the app uses.

### Live updates

All content updates live in an open app: a new event, an edited service time or an approved prayer
on the wall appears within a second or two of a staff member saving it. There's one mechanism, and
features get it for free.

- **A gRPC server stream, not SSE or WebSockets.** `Sync.Watch(cursors)` first sends everything
  since the phone's cursors (catch-up), then keeps the call open and sends each change as it's
  committed, in the same `(feed, id, version, payload)` shape as a pull. SSE would be a non-gRPC
  server path ([AGENTS.md](AGENTS.md)), and it isn't needed: server streaming works over native gRPC
  and over gRPC-Web. The spike ran a server stream through both, including Blazor WASM in Chrome.
- **Live is just sync, pushed.** The stream's changes go through the same Store write and the same
  "feeds changed" event to the UI as a pull. Screens don't know which delivered them, and a feature
  that declares a feed gets live updates with no extra code.
- **Only while the app is in the foreground.** The native layer opens the stream on launch and on
  resume, and closes it when the app goes to the background; iOS suspends sockets soon after anyway.
  When the app is closed, silent pushes and background refresh take over (below).
- **Reconnect from the cursor.** On any drop, reconnect with backoff and jitter and resume from the
  stored cursors, so nothing is missed or duplicated. Jitter matters after a deploy, when every open
  app reconnects at once.
- **Heartbeats.** The server sends a small heartbeat message every ~30 seconds, so proxies with idle
  timeouts (Cloudflare, Traefik, YARP) don't cut a quiet stream. HTTP/2 PINGs alone aren't relied
  on, because not every hop counts them as activity.
- **Browsers get the same stream.** The admin panel watches over gRPC-Web, with no native store, so it
  keeps state in memory. New prayer requests appear in the pastoral inbox live, and two staff editing
  see each other's saves. A future website would do the same with public feeds only.
- **Tiers apply per feed.** `Watch` streams public feeds to anyone. Staff-only feeds, such as the
  prayer inbox, are only included when the call carries a staff token (§6).

On the server:

- **A change is committed and announced in one transaction.** Saving a record writes its feed change
  row (the same rows `Sync` pulls), plus a Postgres `NOTIFY` that carries the feed and new cursor.
  Postgres delivers `NOTIFY` only on commit, so a stream never announces a change that rolled back.
- **Every API instance `LISTEN`s** and fans the change out to the streams it holds, so it works with
  more than one replica and needs no Redis or message bus. If an instance misses a notification (for
  example, a dropped database connection), a reconnect from the cursor recovers it.
- **Scale is not a concern:** a church's concurrent open apps are in the hundreds at most.

### Freshness and compatibility

- **Sync runs live while the app is open (above), on pull-to-refresh, and opportunistically in the
  background.**
  iOS decides when background work runs; Android allows at most every 15 minutes. A "data changed"
  silent push triggers a sync. These pushes are budgeted by iOS, so they are used for real changes,
  not on a timer.
- **Urgent changes** (a cancelled service, a venue change) are a visible push notification as well as
  a sync.
- **A first launch with no network** shows the bundled About and contact pages, plus a friendly
  "connect to load this week's events" message.
- **The API only changes additively,** so older app versions keep working while people update: new
  fields get new numbers and old ones are never reused, and new services are added rather than old
  ones changed. Stored payloads follow the same rule, so an app update can still read what an older
  version stored. If a breaking change is unavoidable, the API can return a "minimum app version" and
  the app asks the user to update.

### Not yet proven (spike before building on it)

1. **Generated proxies over a custom bridging `CallInvoker`** under WASM AOT, including server
   streaming surfaced as `IAsyncEnumerable`.
2. **Pass-through byte codecs:** grpc-swift and grpc-java calling a protobuf-net code-first service
   with no generated stubs.
3. **Bridge cost:** Capacitor plugin calls pass JSON, so the bytes travel base64-encoded. That should
   be fine at church-app volumes; measure it on a real phone.
4. **Background scheduling** on real devices: how often iOS actually runs the refresh, and how
   reliably silent pushes arrive.
5. **Long-lived streams end to end:** a `Watch` stream held open through Cloudflare, Traefik and YARP,
   over native gRPC and gRPC-Web. Find each hop's idle and maximum-duration limits and confirm the
   heartbeat and reconnect cover them.

## 6. Who may call what

Every service method falls into one of three tiers. The API **fails closed**: the default policy
requires a signed-in staff member, so a method that forgets its attribute is locked, not open.

| Tier | Examples | Requirement |
|---|---|---|
| **Public read** | Events, sermons, groups, `Sync` and `Watch` for public feeds | `[AllowAnonymous]`, rate-limited |
| **App write** | Prayer request, "I'm interested", push-topic subscription | A **genuine installed app** (below), rate-limited per install |
| **Admin** | Editing anything, sending pushes, reading prayer requests | A **staff passkey** session; every method, no exceptions |

A test enumerates every gRPC endpoint and fails if any method lacks an explicit tier, or if an
admin feature's service has a method below Admin.

### Admin: passkeys, behind a YARP BFF, as in ImpactKids

The same shape as ImpactKids' gateway, with MealPlanner's passkeys in place of Auth0:

- **One YARP gateway is the only public entry.** It serves the admin panel's files, runs staff sign-in,
  and proxies gRPC to the API. The API itself is cluster-internal.
- **Staff sign in with passkeys at the gateway,** reusing MealPlanner's approach. The WebAuthn
  ceremonies are the one approved non-gRPC path ([AGENTS.md](AGENTS.md)). The approach: ASP.NET Core
  Identity's passkey support, invitations by link, user verification required, and a session cookie
  that is HttpOnly, Secure and SameSite=Strict. The same traps apply:
  - passkeys are bound to the admin site's host for good;
  - the relying party is configuration, never a header;
  - every POST checks its origin.
- **The cookie never leaves the gateway.** A YARP request transform, like ImpactKids'
  `AddBearerTokenToHeadersTransform`, swaps the session for a short-lived signed token (minutes) on
  each proxied admin call. The API validates that token: issuer, audience, staff role.
- **Admin services aren't reachable without a session.** The gateway only forwards admin service
  routes for a signed-in staff member, and the API checks the token again. Two locks, so a routing
  mistake on one side doesn't open the door.
- **A removed staff member or a "sign out everywhere"** ends the gateway session at once; any token
  already issued expires within minutes.

### App writes: proving it's a genuine installed app, without accounts

Nobody signs in to send a prayer request. Instead, the native layer (§5) proves to the API that the
request comes from our app, unmodified, on a real device, using the platforms' own attestation:

- **iOS: App Attest.** The native layer creates a key in the Secure Enclave and has Apple attest it.
  The API verifies the attestation once (Apple's certificate chain, our app ID) and registers the
  install with that public key. Each write then carries an assertion: a signature over the request
  with an increasing counter, checked against the registered key.
- **Android: Play Integrity.** Each write carries an integrity token bound to a hash of the request.
  The API asks Google to decode it and checks: our package name and signing certificate, an app
  installed from Play, and a genuine device.
- **What it proves, and what it doesn't:**
  - Proves: the request came from our real app on a real phone.
  - Doesn't prove: good intent. A person with the genuine app can still send nonsense, so rate limits
    per install still apply, and nothing an app write creates is public without a staff member's
    approval. Prayer requests already go privately to the pastoral team, and the prayer wall (v2)
    would be moderated.
  - Excludes some devices: Android phones without Google Play services, and some rooted or very old
    phones, fail attestation. Those installs get a polite "can't send from this device; here's the
    church's email".
- **Server side:** App Attest verification is CBOR decoding, an X.509 chain check and ECDSA, all in
  the BCL. Play Integrity is one HTTPS call to Google, authenticated with a service account. Both live
  in the API, so attested app traffic passes through the gateway untouched.
- **Pseudonymous, not anonymous:** an install has an ID so limits and blocks can apply. It isn't
  linked to a person unless they type their name into the request.

## 7. Giving

Donations are not digital goods, so Apple and Google in-app purchase does **not** apply. But the
stores still regulate how donations are collected. From memory, to be verified:

- **Apple (3.2.1(vi), 3.2.2(iv)):**
  - An *approved nonprofit* may take donations in its own app, and must support Apple Pay. Approval
    goes through Apple's nonprofit programme, which needs charity verification (in Australia, ACNC
    registration).
  - Without that approval, the app may only link out to Safari to give.
- **Google Play:** charitable donations are exempt from Play Billing. Check the current payments
  policy for any verification requirement.

Plan:
- **v1:** a Give screen that opens our existing giving page (Tithe.ly or a Stripe-hosted page) in the
  system browser. No approval needed, and it ships fastest.
- **v2:** apply as an approved nonprofit, then give in the app with Stripe + Apple Pay / Google Pay.

Never handle card numbers in our own code. Card details stay with the payment provider.

## 8. Modules

Every feature (Events, Sermons, Groups, Prayer, Give, Push, …) is self-contained and **selectable per
host**. The app ships the features we choose. A public website, if we build one later, picks its own
subset. The admin panel picks the admin side of each.

```
src/
  Shared/                    time, module abstractions, small cross-feature interfaces
  Features/Events/
    Events.Contracts         messages + [Service] interfaces + [ProtoModel]      (AOT)
    Events.Api               service implementations, EF Core storage, feeds
    Events.App               Razor class library: pages, components, cache,
                             client [ProtoGrpc]                                   (AOT)
    Events.Admin             Razor class library: staff pages, client [ProtoGrpc]
  Features/Sermons/ …        same four parts, only the ones a feature needs
  Hosts/
    Gateway                  YARP BFF: the only public entry, staff passkeys, serves Admin (§6)
    Api                      cluster-internal; references the *.Api modules it serves
    Migrator                 runs EF Core migrations before the API starts
    App                      Capacitor + Blazor WASM AOT; references the *.App modules it ships
      native/                the data-layer plugin (Swift, Kotlin): generic, no feature code (§5)
    Admin                    Blazor WASM, not AOT; the *.Admin modules
    Web (later)              a public site; whichever *.App modules it wants
```

- **Choosing a feature is explicit:** a project reference plus one registration line in the host
  (`builder.AddEventsApp()`). There's no assembly scanning, which keeps it trim-safe and readable.
- **Each module registers itself:** its services, its navigation entry and its routable assembly. The
  host builds the router's `AdditionalAssemblies` from what it registered.
- **Features never reference each other,** only `Shared`, so any subset builds. When one feature
  shows another's content, it goes through a small interface in `Shared` that the other feature
  implements. For example, Home shows "next events" through a home-card interface.
- **Each module that calls the API declares its own `[ProtoGrpc]`,** because the generator only
  covers its own project (§9). So a feature's client wiring lives inside the feature, and a host that
  leaves the feature out carries none of it.
- **Every assembly with routable pages is rooted for the trimmer** in the AOT hosts (§9). Feature
  `.App` modules have pages, so the App host roots each one it references. To do in the skeleton: make
  `Directory.Build.targets` add this automatically, so a new module can't forget it.

## 9. AOT from the first line

**Scope: the app.** The app host (Blazor WASM, `RunAOTCompilation`) and every project that ends up in
it are AOT-compiled and AOT-checked from the first commit: `Shared`, every `*.Contracts`, and every
`*.App` module. Server-side projects (`*.Api`, the gateway, the migrator) and the admin panel are
not. They run on the JIT, so EF Core, Identity and YARP work as they do in our other apps. The evidence for everything here,
and how to re-run it, is in [`spikes/aot-grpc`](spikes/aot-grpc/README.md).

### Code-first gRPC survives AOT

- **The runtime path doesn't work.** By default protobuf-net builds serializers and client proxies at
  run time with reflection and IL emit. Under native AOT that **compiles fine and crashes at
  start-up** (`PlatformNotSupportedException`); the spike confirmed it.
- **The compile-time path does work.** protobuf-net 3.4+ and protobuf-net.Grpc 1.3.6+ ship source
  generators that write the serializers (`[ProtoModel]`) and the client proxies and server bindings
  (`[ProtoGrpc]`) as ordinary C#. With them, the spike ran unary and server-streaming calls from a
  native AOT server to a native AOT client (over native gRPC and over gRPC-Web) and to Blazor WASM
  AOT in Chrome.
- **They are a preview.** The generator attributes are `[Experimental]` (`PBN9001`), and their shape
  may change. Pin the versions and read the release notes when upgrading.
- **Fallback:** contract-first (`.proto` + `Grpc.Tools` + Google.Protobuf) is Microsoft's supported
  native AOT path. Contracts are per feature and small, so switching would be contained.

### Rules

1. **Every project in the app's scope sets `IsAotCompatible=true` and `TreatWarningsAsErrors=true`,**
   the Blazor app included. (`TreatWarningsAsErrors` applies everywhere; `IsAotCompatible` only in
   scope.) The app must also **root its own assembly** with
   `<TrimmerRootAssembly Include="$(AssemblyName)" />`, and so must every feature module with pages.
   - **Why:** `IsAotCompatible` implies `IsTrimmable`, which opts the assembly into trimming. The
     Blazor router finds pages by reflecting over the assembly, so the trimmer, seeing no static
     reference, removed them. In the spike the app rendered `CtorNotLocated` instead of the home page.
   - **What rooting does:** it keeps the assembly whole while still analysing it. In the spike, a
     rooted app with `IsAotCompatible=true` ran correctly.
   - **Also:** Blazor hides trim warnings by default, so the app sets
     `SuppressTrimAnalysisWarnings=false`.
2. **Reference `protobuf-net` directly in every project,** done once in `Directory.Build.props`.
   - **Why:** protobuf-net ships its MSBuild props in `build/`, not `buildTransitive/`. A project that
     only gets it through a contracts project never tells the analyzers it is an AOT build, so they
     stay silent. That's why the spike's first attempt failed with no warning.
   - **With the direct reference,** a calling project that has no `[ProtoGrpc]` fails the build with
     `PBN4015`.
3. **Each project that calls a service from the app declares its own `[ProtoGrpc]`,** and contracts
   projects declare none. (On the JIT server, protobuf-net's runtime binding works. The API may still
   use its generated `AddXxxServices()` for faster start-up, but it isn't required.)
   - **Why:** the generator deliberately walks only its own assembly's types. Its design notes say a
     `[ProtoGrpc]` in a referenced assembly "has nothing to say about this one", so a factory in the
     contracts project can't rewrite calls in other projects. This is by design, not a bug.
   - **With `InterceptorsNamespaces` including `ProtoBuf.AOT`,** plain
     `channel.CreateGrpcService<T>()` calls are rewritten to the generated proxies.
4. **These protobuf-net diagnostics are errors** (via `TreatWarningsAsErrors`):
   - `PBN3001`–`PBN3004`: a contract was dropped from the generated model;
   - `PBN3010`/`PBN3011`: a call still goes through the reflection model;
   - `PBN4010`: no serializer model was named;
   - `PBN4015`: an AOT project has no `[ProtoGrpc]`;
   - `PBN4016`/`PBN4017`: a call or DI client isn't using the generated proxies;
   - `PBN4018`: a contract was dropped and will throw.

   `TreatWarningsAsErrors` has already earned its place: it caught two obsolete APIs in the spike.
5. **Known warnings live in a committed baseline, checked in CI** (`aot-warnings/`).
   - **Our own code:** zero warnings. A genuine false positive is suppressed in source with
     `[UnconditionalSuppressMessage]` and a justification. The spike needed two:
     - the warnings the Razor compiler generates for `Router.NotFoundPage` in `App.razor`;
     - `IL2095`, an annotation mismatch inside protobuf-net that the trimmer reports on our generated
       `[ProtoModel]` class.
   - **Library code:** protobuf-net still contains reflection paths it doesn't use, so a publish
     reports warnings from inside it. Each one is listed exactly (warning code and member) in a
     baseline file for the app.
   - **The CI script fails on** any warning in our code, any library warning not in the baseline, and
     any baseline entry that no longer appears, so the list never goes stale after an upgrade.
   - **Why not suppress library warnings directly:**
     - The WASM trimmer can suppress a single member in someone else's library with
       `--link-attributes` (tested: it removed exactly the targeted warnings). But the only way to pass
       that option is `_ExtraTrimmerArgs`, an unofficial SDK property that could change without
       notice.
     - The native AOT compiler can't do it at all, if the server ever goes AOT: no such option, and it
       ignores an embedded `ILLink.LinkAttributes.xml` (tested: 5 warnings before, 5 after).
     - `NoWarn` works per warning code across the whole build, so it would hide our own warnings too.

     A baseline is exact and relies on nothing unofficial.
6. **CI runs the published app, not just the build,** because a JIT build hides AOT failures. CI
   publishes the WASM AOT app, runs the baseline check, then a smoke test drives it in headless Chrome
   against the real API, calling every service the app uses.

### Time

- **Only UTC crosses the wire.** Clients convert to local time for display. The server handles
  anything tied to the church's clock, such as "send the reminder at 10:00", using the church's zone
  (`Australia/Brisbane`, no daylight saving). This is the same approach as ImpactKids' `ChurchTime`,
  which moves into `Shared`.
- **Contracts assemblies set `[assembly: CompatibilityLevel(CompatibilityLevel.Level300)]`.** It
  sends `DateTime`/`TimeSpan` as `google.protobuf.Timestamp`/`Duration`.
  - **Tested:** with the default format, a UTC `DateTime` arrived as `Kind=Unspecified`. With
    Level300 it arrived as `Kind=Utc`.
  - **Why it matters:** `ToLocalTime()` happens to treat `Unspecified` as UTC, but `ToUniversalTime()`
    treats it as *local*. An `Unspecified` value is therefore a bug waiting for the wrong call.
  - **Note:** the older per-member `DataFormat.WellKnown` is obsolete. Level300 also sends `decimal`
    and `Guid` as strings.

## 10. Backend

A church API in the existing GSBC stack (.NET / Aspire, EF Core, deployed through `gsbc.argo`), behind
a YARP gateway (§6) that is reached through the usual Cloudflare tunnel and ingress.

### Transports

| Caller | Transport | Why |
|---|---|---|
| The store apps (native data layer, §5) | Native gRPC over HTTP/2, through the gateway | Full gRPC, durable sync |
| Admin panel (Blazor WASM) | gRPC-Web | It's a browser |
| A future public website | gRPC-Web | It's a browser |
| Other GSBC services | Native gRPC over HTTP/2, in-cluster | Full gRPC |

- **Browsers need gRPC-Web.** In a browser, or in WASM inside a WebView, .NET's `HttpClient` is
  `fetch`. `fetch` doesn't expose the HTTP/2 trailers gRPC needs, which is the gap gRPC-Web fills.
  That is why the store apps call the API from native code (§5), and why the admin panel and any
  website use gRPC-Web. gRPC-Web supports unary and server-streaming calls; client and two-way
  streaming are native-only.
- **One set of C# proxies serves both transports.** Feature code is written against the service
  interfaces. The host picks a `CallInvoker`: the native bridge in the store apps, a gRPC-Web channel
  in the browser hosts.
- **Kestrel listens on two ports,** because plaintext HTTP/2 (h2c) can't share a port with HTTP/1.1:
  - `web`: HTTP/1.1 + HTTP/2, gRPC-Web.
  - `grpc`: HTTP/2 only, native gRPC.

  The spike ran both from one server. The gateway proxies to whichever the caller used.
- **To verify in the skeleton:** native gRPC from phones goes through Cloudflare, so confirm the
  tunnel and the Traefik ingress carry it end to end. Cloudflare's gRPC support has to be enabled for
  the zone, and both Traefik and YARP must carry HTTP/2 end to end (h2c inside the cluster). If it can't be made to work, the native
  layer falls back to gRPC-Web, still over HTTP/2.

### Services

- **Code-first gRPC services,** one set per feature module (§8).
- **One generic `Sync` service** serves the native data layer and the browsers (§5). `Pull` returns
  changes since a cursor, per feed, as `(feed, id, version, deleted, payload bytes)`. `Watch` does the
  same, then stays open and streams changes as they're committed. Each feature's `.Api` module registers
  its feeds: what's in them, how a record is encoded, its retention window and its tier.
- **Public read services:** events, sermons, groups, announcements and service times. They are
  anonymous. gRPC calls are POSTs, so Cloudflare doesn't cache them; the app's store (§5) does
  that job.
- **Write services:** prayer requests and "I'm interested" forms. They are rate-limited and go to
  the right staff member.
- **Push:** the API sends notifications directly to each platform's push service. There is no
  third-party push SaaS. See below.

### Admin panel

- **Blazor WebAssembly, not AOT,** served by the gateway, which also runs passkey sign-in and swaps the
  session for API tokens (§6). Staff-only, so start-up time matters less than build time. Its modules
  are still AOT-checked, because they share contracts and conventions with the AOT hosts.
- **Modular like the app:** it hosts the `*.Admin` module of each feature it manages.
- **Calls the API from the browser over gRPC-Web,** with the token on every call. This is the only
  public website.

### Health checks

- **gRPC, not HTTP.** Every server host (gateway, API) serves the standard gRPC health service
  (`grpc.health.v1`, via `Grpc.AspNetCore.HealthChecks`). Kubernetes probes it with its built-in `grpc`
  probe type, so there's no `/healthz` endpoint (AGENTS.md's hard rule).
- **Readiness includes dependencies:** the database for the API, and the API for the gateway. Liveness
  stays shallow, so a database outage doesn't restart every pod.

### Data access

- **EF Core with Npgsql,** as in our other apps. The API isn't AOT, so nothing about EF Core is
  constrained.
- **Migrations run as a separate job before the API starts,** the same pattern as ImpactKids'
  `Workers.DbMigrations`.

### Push notifications

Each platform has one push transport and there is no way around it. What *is* optional is a
third-party SDK or service on top.

- **iOS: APNs.** Capacitor's Push Notifications plugin returns a native APNs device token. The API
  sends to APNs over HTTP/2 using a token-based `.p8` key from the Apple Developer account.
- **Android: FCM.** Google only delivers push to Play-services Android devices through Firebase
  Cloud Messaging. Using FCM means a Firebase project and a `google-services.json` in the Android
  build; nothing else from Firebase is used. The API sends with the FCM HTTP v1 API and a service
  account key.
- **The API stores device tokens** with the platform and topic subscriptions (Youth, Kids, Prayer and
  so on). It fans out per topic itself.
- **Not used:** Firebase on iOS, Firebase analytics, and hosted push services (OneSignal etc.).
  This keeps the dependency to the bare transport.

## 11. Privacy and safety

- No third-party analytics or ad SDKs, which also keeps the store privacy labels short.
- Prayer requests are sensitive. Use HTTPS only, give staff least-privilege access, and offer a
  retention period and a delete option.
- No photos of children in content without consent. This follows the church's existing
  child-safety policy.
- Secrets (APNs key, FCM service account, payment keys) never go in this repo. **It is public.**

## 12. Open questions

1. Does ImpactKids (kids check-in) connect to the app, for example parent notifications?
2. Giving: stay with Tithe.ly's giving page, or move to Stripe? Is ACNC registration in place for
   Apple's nonprofit programme?
3. Who publishes to the stores? An Apple Developer account in the church's name (organisation
   enrolment needs a D-U-N-S number) and a Google Play developer account.
4. What from Tithe.ly do people actually use today? Measure before migrating.
5. Sermons: is YouTube or a podcast feed the source of record, or does the API hold them and link
   out?
6. Android devices without Google Play services fail Play Integrity: is "email the church instead"
   acceptable for them, or do we need a fallback (for example, writes held for staff review)?

## 13. Milestones

0. **Done:** code-first gRPC under AOT ([`spikes/aot-grpc`](spikes/aot-grpc/README.md)).
1. **Skeleton:** includes the fail-closed tier test (§6), the endpoint test that rejects any server path
   that isn't gRPC unless the owner has approved it ([AGENTS.md](AGENTS.md)), and the YARP gateway with
   passkey sign-in and the session-to-token transform.
   Also: the module layout (§8) with the §9 rules, the warning baseline and an AOT CI
   gate (publish, check, then smoke-test the binaries). Then the native data layer spike (§5's five
   open points): a Capacitor shell with one screen fed through the native Transport, Store and Sync,
   on a real iPhone and Android device, through Cloudflare.
2. **API + admin:** events, announcements and sermons editable by staff, appearing in the app with
   no release.
3. **v1 features:** Home, Sermons, Events, Groups, Give (link out), push (APNs + FCM).
4. **Store submission:** TestFlight / internal testing, then public release.
5. **v2:** in-app giving (approved nonprofit), prayer wall, sign-in if needed.
