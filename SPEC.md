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
streaming infrastructure, and anything that needs native code beyond standard Capacitor plugins.

## 2. Shape

One web app, packaged three ways from a single build:

| Target | How | Updates |
|---|---|---|
| iOS | Capacitor shell (WKWebView) | Code and design: App Store. Data: live from the API |
| Android | Capacitor shell (Android WebView) | Code and design: Play Store. Data: live from the API |
| Web | The same build as a PWA | Service worker |

**Capacitor only.** Nothing in scope needs native UI. Native capabilities come from official plugins:
Push Notifications, Haptics, Share, Browser, App (Android back button, deep links), Preferences and
Splash Screen.

**UI stack:** Blazor WebAssembly, **AOT-compiled**. It reuses our existing GSBC stack (ImpactKids,
Accounting) and the patterns proven in the Graced POC: native-feel navigation, safe areas and
offline-first. The app is bundled in the store build, so AOT's larger download costs nothing at run
time and buys a faster start-up.

**API:** code-first gRPC (protobuf-net.Grpc), called from the app over gRPC-Web. The backend is
native-AOT too. Both are AOT-checked from the first commit (§7).

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

- **Data** is records from the church API (§8), edited by staff in an admin UI. It appears in the app
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

## 5. Data in the app: offline and freshness

- **Every list is cached on the device** (IndexedDB) with the time it was fetched.
- **Screens show the cache immediately,** then refresh from the API in the background
  (stale-while-revalidate). If the refresh fails, they keep showing what they have, with an
  "updated 3 hours ago" note.
- **The app refreshes on launch, on resume, and on pull-to-refresh.**
- **Urgent changes** (a cancelled service, a venue change) go out as a push notification. Tapping it
  opens the app and refreshes the relevant screen.
- **The API only changes additively,** so older app versions keep working while people update:
  new fields get new numbers and old ones are never reused, and new services are added rather than
  old ones changed. If a breaking change is unavoidable, the API can return a "minimum app version"
  and the app asks the user to update.
- **A first launch with no network** shows the bundled About and contact pages, plus a friendly
  "connect to load this week's events" message.

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

## 7. AOT from the first line

Every .NET project is AOT-compiled: the Blazor app with `RunAOTCompilation`, and the backend with
`PublishAot` (native AOT). This is enforced from the first commit, not retrofitted.

### Code-first gRPC under AOT: investigated

The question was whether code-first gRPC (protobuf-net.Grpc: C# interfaces and `[ProtoContract]`
classes, no `.proto` files) survives AOT, or whether we would need `.proto` files and code
generation. **It survives.** The evidence and how to re-run it are in
[`spikes/aot-grpc`](spikes/aot-grpc/README.md).

- **The runtime path does not work.** By default protobuf-net builds serializers and client proxies
  at run time with reflection and IL emit. Under native AOT that **compiles fine and crashes at
  start-up** (`PlatformNotSupportedException`); the spike confirmed it.
- **The compile-time path does work.** protobuf-net 3.4+ and protobuf-net.Grpc 1.3.6+ ship source
  generators that write the serializers (`[ProtoModel]`) and the client proxies and server bindings
  (`[ProtoGrpc]`) as ordinary C#. With them, the spike ran unary and server-streaming calls from a
  native AOT server to both a native AOT client and Blazor WASM AOT in Chrome.
- **They are a preview.** The generator attributes are `[Experimental]` (`PBN9001`), and their shape
  may change between versions. That is acceptable for a new app with a small API, but pin the
  versions and read the release notes when upgrading.
- **Fallback:** if the generators stall, contract-first (`.proto` + `Grpc.Tools` + Google.Protobuf)
  is Microsoft's supported native AOT path. Contracts are small, so switching would be contained.

### The rules

1. **Library projects** (contracts, shared code): `IsAotCompatible=true`. This turns on the trim and
   AOT analyzers for our own code.
2. **The Blazor app project:** do **not** set `IsAotCompatible` or `IsTrimmable` on it. They make the
   trimmer trim the app assembly itself, which removed its routed pages in the spike
   (`CtorNotLocated`). Set `EnableTrimAnalyzer`, `EnableAotAnalyzer` and
   `SuppressTrimAnalysisWarnings=false` instead. Blazor hides trim warnings by default.
3. **Warnings are errors for our code:** `TreatWarningsAsErrors`, including all `IL2xxx`/`IL3xxx` and
   these protobuf-net diagnostics:
   - `PBN3001`–`PBN3004`: a contract was dropped from the generated model;
   - `PBN3010`/`PBN3011`: a call still goes through the reflection model;
   - `PBN4010`: no serializer model was named;
   - `PBN4016`/`PBN4017`: a call or a DI-registered client isn't using the generated proxies;
   - `PBN4018`: a contract was dropped and will throw under AOT.
4. **One `[ProtoModel]`** in the contracts project lists the message types. One `[ProtoGrpc]` in each
   **calling** project names the services; the app has its own, and the server has its own with the
   implementations. The spike found that a factory declared only in the contracts project does not
   rewrite calls in the projects that use it, and no diagnostic flags that. Each call site must see a
   `[ProtoGrpc]` in its own project, with `InterceptorsNamespaces` including `ProtoBuf.AOT`, or pass
   the factory explicitly.
5. **The server uses the generated `AddXxxServices()`, never `AddCodeFirstGrpc()`.** The latter
   registers the reflection binder.
6. **Library warnings are tracked, not hidden.** Even with the generators, protobuf-net still contains
   reflection paths it doesn't use, so a publish reports warnings from inside the library: about 20
   on the server and about 240 in the WASM build. Collapse them to one warning per assembly
   (`TrimmerSingleWarn`), commit the list of expected assemblies, and fail CI if a new one appears.
7. **CI runs the published output, not just the build.** The spike's lesson is that a JIT build
   hides AOT failures. CI publishes the native AOT server and the WASM AOT app, then runs a smoke test
   that calls every service through the published binaries. Headless Chrome drives the WASM test.

### Gotchas found

- **`DateTime` loses its `Kind`:** UTC went in and came out unspecified. Use UTC everywhere and
  restore the `Kind` on read, or use `DateTimeOffset` with a protobuf-net surrogate.
- **gRPC-Web from .NET clients:** set `GrpcWebHandler.HttpVersion` to 1.1. Otherwise the client
  tries HTTP/2 against an HTTP/1.1 endpoint and fails with `HTTP_1_1_REQUIRED`.
- **Size:** the WASM AOT `_framework` is about 26 MB of `.wasm` (6.7 MB Brotli). It ships inside the
  store build, so this only matters for the web/PWA target.

## 8. Backend

A church API in the existing GSBC stack (.NET / Aspire, native AOT, deployed through `gsbc.argo`),
reached through the usual Cloudflare tunnel and ingress:

- **Code-first gRPC services,** exposed to the app over gRPC-Web, so the same contracts work from
  WKWebView, Android WebView and the browser.
- **Public read services:** events, sermons, groups, announcements and service times. They are
  anonymous. gRPC-Web calls are POSTs, so Cloudflare doesn't cache them; the app's own cache (§5)
  does that job.
- **Write services:** prayer requests and "I'm interested" forms. They are rate-limited and go to
  the right staff member.
- **Admin UI:** a staff-only page where staff edit records and send push notifications. It sits
  behind sign-in, for example Cloudflare Access or the existing GSBC auth.
- **Push:** the API sends notifications directly to each platform's push service. There is no
  third-party push SaaS. See below.

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

## 9. Privacy and safety

- No third-party analytics or ad SDKs, which also keeps the store privacy labels short.
- Prayer requests are sensitive. Use HTTPS only, give staff least-privilege access, and offer a
  retention period and a delete option.
- No photos of children in content without consent. This follows the church's existing
  child-safety policy.
- Secrets (APNs key, FCM service account, payment keys) never go in this repo. **It is public.**

## 10. Open questions

1. Does ImpactKids (kids check-in) connect to the app, for example parent notifications?
2. Giving: stay with Tithe.ly's giving page, or move to Stripe? Is ACNC registration in place for
   Apple's nonprofit programme?
3. Who publishes to the stores? An Apple Developer account in the church's name (organisation
   enrolment needs a D-U-N-S number) and a Google Play developer account.
4. What from Tithe.ly do people actually use today? Measure before migrating.
5. Sermons: is YouTube or a podcast feed the source of record, or does the API hold them and link
   out?

## 11. Milestones

0. **Done:** code-first gRPC under AOT ([`spikes/aot-grpc`](spikes/aot-grpc/README.md)).
1. **Skeleton:** solution with the §7 rules and an AOT CI gate (publish, then smoke-test the
   binaries), plus a Capacitor shell with one screen fed by the API and an offline cache, on a real
   iPhone and Android device.
2. **API + admin:** events, announcements and sermons editable by staff, appearing in the app with
   no release.
3. **v1 features:** Home, Sermons, Events, Groups, Give (link out), push (APNs + FCM).
4. **Store submission:** TestFlight / internal testing, then public release.
5. **v2:** in-app giving (approved nonprofit), prayer wall, sign-in if needed.
