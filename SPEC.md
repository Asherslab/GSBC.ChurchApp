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
| Web (later, maybe) | A separate host with a chosen subset of features (§7) | Ordinary deploys |

The app is not published as a website for now. The only public site is the staff admin panel (§9).

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
gRPC-Web (§9). The backend is native AOT too. Both are AOT-checked
from the first commit (§8).

**Modular:** every feature is a set of modules that a host opts into: the app, the admin panel and any
future public website (§7).

**Not AOT:** the admin panel. It is Blazor WebAssembly too, but staff-only, so AOT's start-up gain
isn't worth the build time (§9).

Downloading Blazor's compiled code (`.wasm`/`.dll`) over the air is a grey area under the store
rules, but this plan never does it (§4), so it is not a constraint.

## 3. Features (v1)

- **Home:** this Sunday (service times, what's on, the bulletin), plus a pinned announcement and
  quick links.
- **Sermons:** a series → sermon list with video (YouTube embed) or audio (podcast feed), notes and
  passages. Last-played position is remembered.
- **Events:** a list and calendar, with "add to calendar" and a registration link where one is needed.
- **Groups:** life groups and ministries, each with a contact or "I'm interested" button.
- **Give:** see §6, which is the most constrained feature.
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

- **Data** is records from the church API (§9), edited by staff in an admin UI. It appears in the app
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
  `fetch` can't do (§9).

### Keep the native side generic

The risk with a native layer is that every feature grows Swift and Kotlin code alongside its C#.
That would triple the work and break the modularity in §7. So the native side **never understands a
message**: payloads stay protobuf bytes that only C# encodes and decodes. Contracts, models and
logic stay in one place, in C#, and native gRPC libraries need no `.proto` code generation, because
they only move bytes.

The plugin does four things:

| Part | What it does | What it knows |
|---|---|---|
| **Transport** | Makes a gRPC call: method path + request bytes in, response bytes (or a stream of them) out. HTTP/2 via grpc-swift / grpc-java with a pass-through byte codec | URLs, auth headers, retries |
| **Store** | SQLite table of `(feed, id, version, deleted, payload)` plus a sync cursor per feed | Nothing about the payload |
| **Sync** | Pulls "changes since cursor" from one generic `Sync` service, writes them to the store, then tells the UI which feeds changed. Runs on launch, on resume, from a silent push, and from the OS's background scheduler (BGTaskScheduler / WorkManager) | Feed names |
| **Outbox** | Queues writes made offline (a prayer request at church with no signal) and sends them through Transport when the network returns, in the background if need be | Method paths |

On the .NET side:

- **A `CallInvoker` that bridges to Transport.** protobuf-net's generated proxies run on top of any
  `CallInvoker`, so feature code calls `IEventsService` exactly as it would over gRPC-Web. The
  serialized bytes simply go through the plugin instead of `fetch`.
- **Each feature reads its feeds from the Store** and decodes the payloads with its own contracts.
  Screens show stored data immediately and re-render when Sync reports their feed changed.
- **Feature modules declare their feeds on the server** (§9), so adding a feature adds no native code.

### Freshness and compatibility

- **Sync runs on launch, on resume, and on pull-to-refresh, and opportunistically in the background.**
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

## 6. Giving

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

## 7. Modules

Every feature (Events, Sermons, Groups, Prayer, Give, Push, …) is self-contained and **selectable per
host**. The app ships the features we choose. A public website, if we build one later, picks its own
subset. The admin panel picks the admin side of each.

```
src/
  Shared/                    time, module abstractions, small cross-feature interfaces
  Features/Events/
    Events.Contracts         messages + [Service] interfaces + [ProtoModel]      (AOT)
    Events.Api               service implementations, storage, server [ProtoGrpc] (AOT)
    Events.App               Razor class library: pages, components, cache,
                             client [ProtoGrpc]                                   (AOT)
    Events.Admin             Razor class library: staff pages, client [ProtoGrpc]
  Features/Sermons/ …        same four parts, only the ones a feature needs
  Hosts/
    Api                      native AOT; references the *.Api modules it serves
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
  covers its own project (§8). So a feature's client wiring lives inside the feature, and a host that
  leaves the feature out carries none of it.
- **Every assembly with routable pages is rooted for the trimmer** in the AOT hosts (§8). Feature
  `.App` modules have pages, so the App host roots each one it references. To do in the skeleton: make
  `Directory.Build.targets` add this automatically, so a new module can't forget it.

## 8. AOT from the first line

The app (Blazor WASM, `RunAOTCompilation`) and the API (`PublishAot`, native AOT) are AOT-compiled and
AOT-checked from the first commit. The admin panel is not (§9). The evidence for everything here,
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

1. **Every project sets `IsAotCompatible=true` and `TreatWarningsAsErrors=true`,** the Blazor app
   included. The app must also **root its own assembly** with
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
3. **Each project that calls or hosts a service declares its own `[ProtoGrpc]`,** and contracts
   projects declare none.
   - **Why:** the generator deliberately walks only its own assembly's types. Its design notes say a
     `[ProtoGrpc]` in a referenced assembly "has nothing to say about this one", so a factory in the
     contracts project can't rewrite calls in other projects. This is by design, not a bug.
   - **With `InterceptorsNamespaces` including `ProtoBuf.AOT`,** plain
     `channel.CreateGrpcService<T>()` calls are rewritten to the generated proxies.
   - **The server uses its generated `AddXxxServices()`,** never `AddCodeFirstGrpc()`, which registers
     the reflection binder.
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
     per-host baseline file.
   - **The CI script fails on** any warning in our code, any library warning not in the baseline, and
     any baseline entry that no longer appears, so the list never goes stale after an upgrade.
   - **Why not suppress library warnings directly:** suppression for a specific member of someone
     else's library isn't possible in the native AOT compiler. It has no `--link-attributes` option
     and ignores an embedded `ILLink.LinkAttributes.xml` (tested: 5 warnings before, 5 after). The
     WASM trimmer does support it, but only through `_ExtraTrimmerArgs`, an unofficial SDK property.
     `NoWarn` is per warning code across the whole build, so it would hide our own warnings too. A
     baseline is exact and works the same for both compilers.
6. **CI runs the published output, not just the build,** because a JIT build hides AOT failures. CI
   publishes the native AOT API and the WASM AOT app, runs the baseline check, then runs a smoke test
   that calls every service through the published binaries. Headless Chrome drives the WASM side.

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

## 9. Backend

A church API in the existing GSBC stack (.NET / Aspire, native AOT, deployed through `gsbc.argo`),
reached through the usual Cloudflare tunnel and ingress.

### Transports

| Caller | Transport | Why |
|---|---|---|
| The store apps (native data layer, §5) | Native gRPC over HTTP/2, through the ingress | Full gRPC, durable sync |
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

  The spike ran both from one native AOT server. Behind TLS, ALPN would let one public port do both.
- **To verify in the skeleton:** native gRPC from phones goes through Cloudflare, so confirm the
  tunnel and the Traefik ingress carry it end to end. Cloudflare's gRPC support has to be enabled for
  the zone, and the ingress must speak h2c to the `grpc` port. If it can't be made to work, the native
  layer falls back to gRPC-Web, still over HTTP/2.

### Services

- **Code-first gRPC services,** one set per feature module (§7).
- **One generic `Sync` service** serves the native data layer (§5). It returns changes since a cursor,
  per feed, as `(feed, id, version, deleted, payload bytes)`. Each feature's `.Api` module registers
  its feeds: what's in them and how a record is encoded.
- **Public read services:** events, sermons, groups, announcements and service times. They are
  anonymous. gRPC calls are POSTs, so Cloudflare doesn't cache them; the app's store (§5) does
  that job.
- **Write services:** prayer requests and "I'm interested" forms. They are rate-limited and go to
  the right staff member.
- **Push:** the API sends notifications directly to each platform's push service. There is no
  third-party push SaaS. See below.

### Admin panel

- **Blazor WebAssembly, not AOT.** It is staff-only, so start-up time matters less than build time.
  Its modules are still AOT-checked, because they share contracts and conventions with the AOT hosts.
- **Modular like the app:** it hosts the `*.Admin` module of each feature it manages.
- **Calls the API from the browser over gRPC-Web.** This is the only public website.
- **Authorization happens in the API, not just in front of the site.** Because the browser calls the
  API directly, every staff-only (write and admin) service checks the caller itself. Cloudflare
  Access, or the existing GSBC auth, signs staff in; the API validates that token on every admin
  call. Public read services stay anonymous.

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

## 10. Privacy and safety

- No third-party analytics or ad SDKs, which also keeps the store privacy labels short.
- Prayer requests are sensitive. Use HTTPS only, give staff least-privilege access, and offer a
  retention period and a delete option.
- No photos of children in content without consent. This follows the church's existing
  child-safety policy.
- Secrets (APNs key, FCM service account, payment keys) never go in this repo. **It is public.**

## 11. Open questions

1. Does ImpactKids (kids check-in) connect to the app, for example parent notifications?
2. Giving: stay with Tithe.ly's giving page, or move to Stripe? Is ACNC registration in place for
   Apple's nonprofit programme?
3. Who publishes to the stores? An Apple Developer account in the church's name (organisation
   enrolment needs a D-U-N-S number) and a Google Play developer account.
4. What from Tithe.ly do people actually use today? Measure before migrating.
5. Sermons: is YouTube or a podcast feed the source of record, or does the API hold them and link
   out?

## 12. Milestones

0. **Done:** code-first gRPC under AOT ([`spikes/aot-grpc`](spikes/aot-grpc/README.md)).
1. **Skeleton:** the module layout (§7) with the §8 rules, the warning baseline and an AOT CI
   gate (publish, check, then smoke-test the binaries). Then the native data layer spike (§5's four
   open points): a Capacitor shell with one screen fed through the native Transport, Store and Sync,
   on a real iPhone and Android device, through Cloudflare.
2. **API + admin:** events, announcements and sermons editable by staff, appearing in the app with
   no release.
3. **v1 features:** Home, Sermons, Events, Groups, Give (link out), push (APNs + FCM).
4. **Store submission:** TestFlight / internal testing, then public release.
5. **v2:** in-app giving (approved nonprofit), prayer wall, sign-in if needed.
