# Spike: design preview on a real iPhone

**Question:** what does a GSBC app designed for the church (not for Tithe.ly's limits) look like, and
what does the Capacitor + Blazor WASM AOT stack (SPEC §2) actually deliver inside an iPhone, before we
build the real skeleton? It is a first design pass for staff and the pastor to react to, not the app.

Started 2026-10-26 with .NET SDK 10.0.401, Capacitor 8.5.3 and Xcode 26.2.

| Part | What it is |
|---|---|
| `App` | Blazor WebAssembly, AOT in Release. Every screen, the shared components and the JS bridge to Capacitor |
| `shell` | Capacitor 8 iOS project (Swift Package Manager, no CocoaPods), bundle id `au.com.baptist.churchapp.poc` |

## What's real and what's sample

| Content | Source |
|---|---|
| Sermons, audio, artwork, YouTube links | **Live** from `baptist.com.au/podcasts/media.rss` (through CapacitorHttp, as the site sends no CORS headers), with a bundled snapshot of the same feed for offline |
| Bulletin (25 Oct 2026) and sermon outlines | Typed from the Tithe.ly bulletin; the welcome letter is paraphrased |
| Service times, address, links | baptist.com.au |
| Events, prayer wall, inbox | Samples. Events are being redone; the model is just a calendar event, image, title and description |
| Life Groups | Round 2 (13 Sep – 18 Oct) from the printed flyer: titles, blurbs, leaders, colours and rooms. Leader photos are the church website's own (initials where it has none); Kids Life and Youth leaders are from the website's team page. Summer FIT is the next round. Notices and resources are samples, and so is the one changed Sunday (11 Oct) |
| Site map | Drawn from the Life Groups room map's Illustrator file, without its labels; room names are placeholders |
| Logo, colours, type | The GSBC Logo Pack 2022 (brand) and the website's 2026 theme; switch under More → Appearance |

Nothing is sent anywhere: the prayer form says so when submitted.

## Design decisions to put to staff

Chosen by the owner on 2026-10-26 from three dealt layouts (the impeccable design skill), combined at
their request. The direction is recorded in `.impeccable/surfaces/spikes-app-poc-app.md`, the visual
system in `DESIGN.md`.

- **Five tabs: Sunday, Week, Listen, Prayer, More.** Notifications and prayer requests are the go-to
  items: Prayer has its own tab, and a bell with an unread badge sits in every tab's header. First
  launch asks which notification topics you want.
- **Sunday is the bulletin, as native text, not PDF page images:** a navy header with the yearly theme
  and the next service (or "Live now"), sticky section tabs, tappable Bible passages, and notes saved on
  the phone.
- **Week is the calendar:** a seven-day strip and an agenda of services and events.
- **Listen is watch and listen:** the latest sermon in the header, keep listening with real progress,
  series and speakers.
- **Every screen shares the bulletin look:** navy header, white page, orange underline on the active
  section. Dark mode is app-wide, in the palette of the dark listen sketch.
- **Give opens Tithe.ly in Safari** (SPEC §7).
- **Two palettes for staff to compare:** the 2022 brand (navy, aqua, beige, orange, Montserrat) and the
  2026 yearly theme (the website's greens with DM Serif headings), under More → Appearance.
- **Text follows the iOS text-size setting** (Dynamic Type).

## Five layouts for staff

One build carries five layouts with the same content and colours; switch them under More → Appearance
(in Simple, Home → More → Appearance). The choice is remembered on the phone.

| Layout | Home | Navigation | Code |
|---|---|---|---|
| Bulletin | This Sunday's bulletin | Sunday · Week · Listen · Prayer · More | `Pages/Sunday.razor` |
| Today | A feed ordered by the moment in the church week (live, Sunday, after Sunday, midweek) | Today · Listen · Calendar · Prayer · More | `Layouts/Today/` |
| Hub | A grid of large live tiles, the old app's grid done properly | Home · Listen · Prayer · More | `Layouts/Hub/` |
| Simple | Six large buttons, 18% larger type app-wide | No tab bar; Home on every screen | `Layouts/Simple/` |
| The week | Last Sunday to next Sunday, with the bulletin and sermons on their day | Week · Sunday · Listen · Prayer · More | `Layouts/WeekFirst/` |

`Layouts/LayoutCatalog.cs` defines each layout's tabs; `Pages/Home.razor` picks the home. Each layout's
styles are in `wwwroot/css/layout-*.css`, scoped to it. Once staff choose, the losing layouts are
deleted and DESIGN.md is reconciled with the winner.

## Building

```sh
dotnet publish App -c Release -o out/app        # AOT, ~1 min; output in out/app/wwwroot
cd shell && npm ci && npx cap sync ios           # copies out/app/wwwroot into the iOS project
open ios/App/App.xcodeproj                       # pick your team, plug in the iPhone, Run
```

Or, once `shell/.team` holds your Apple team id (gitignored; never put it in `project.pbxproj`, the
repo is public), `shell/build-ios.sh <device-udid>` does all of it and installs and launches the app.
It needs the phone's hardware udid (`00008130-…`), not the CoreDevice identifier that
`xcrun devicectl list devices` prints; get it with
`xcrun devicectl list devices --json-output out.json` (`hardwareProperties.udid`) or from Xcode's
Devices window. The launch step fails harmlessly if the phone is locked.

`dotnet run --project App` serves the same UI in a desktop browser (without native features, and with
sermons from the bundled snapshot because of CORS). The dev server keeps stale ETags for files under
`wwwroot`, so rebuild after editing CSS or JS, or the browser keeps the old copy.

A free Apple ID can install it on a phone plugged into the Mac, for 7 days at a time. Push
notifications, App Attest and TestFlight need the church's paid developer account (SPEC §12); the
in-app **More → Device check** lists what this build can and can't prove.

## PWA preview at app.baptist.com.au

Short-lived, added 2026-10-27: staff try the layout and feel on their own phones for a week or two
before the church's developer account and TestFlight exist. The same build runs in Safari; opened in a
browser tab it offers **Add to Home Screen** (`Components/InstallSheet.razor`), and from the Home Screen
it opens full screen.

Native-only features say so instead of working: notification previews and Add to calendar report
"Phone only", haptics do nothing, and the first-run notifications sheet is skipped in a browser tab. Sermons come from the bundled feed snapshot (baptist.com.au
sends no CORS headers, and a proxy would be a non-gRPC API path). YouTube plays inline.

The service worker (`wwwroot/service-worker.published.js`, ported from Graced) precaches each build
under its own version, fetching every file cache-busted with its integrity hash, so Cloudflare (whose
baptist.com.au zone stretches static files to a 4-hour browser cache) can't serve a stale copy. A new
build installs behind the running one; the app checks when it returns to the foreground and every five
minutes, and `Components/UpdateBanner.razor` offers Reload.

`pwa/Dockerfile` builds an nginx image (context `spikes/app-poc`); `.github/workflows/app-poc-pwa.yml`
pushes it to GHCR and pins the tag in gsbc.argo (`clusters/mini/app/church-app-pwa`), the same as
Graced. Delete the workflow, `pwa/` and the gsbc.argo manifests once TestFlight is running.

## Life Groups and the site map

More → Life Groups (also on every home layout, and LIFE Groups in the week links to it). Today is the
break between rounds; the switch at the bottom of the Life Groups page previews 11 Oct, a Sunday in
Round 2, with a changed room. Joining is stored on the phone, and "Preview a notice" schedules a local
notification. Each group's "Find the room" opens a map of just that room, with the main buildings
labelled around it; the Map button on the Life Groups page is the overview of every room, numbered.
More → Site map has the same overview plus a Key places layer.
Design: `docs/features/life-groups.md` and `docs/features/site-map.md`.

## How it fits together

- **One audio player for the whole app.** `Native/PlayerService.cs` wraps a single `Audio` element in
  `wwwroot/js/native.js` (`gsbc.player`), so audio survives navigation; `AudioPlayer`, the Listen
  header and the tab bar's playing mark are views on it. Positions are saved to local storage every 5 s
  (`gsbc:position:<id>`, `gsbc:length:<id>`), which feeds Keep listening. Background playback comes from
  `UIBackgroundModes` audio in Info.plist and the `.playback` audio session set in `AppDelegate.swift`.
- **Themes are attributes on `<html>`:** `data-theme` (brand, year), `data-scheme` (light, dark) and
  `data-layout`. An inline script in `index.html` sets them from local storage before Blazor starts, so
  there's no flash; `NativeBridge.ApplyThemeAsync` keeps them in step with `AppSettings`. Every colour is
  a token on these selectors in `wwwroot/css/app.css`; type is in rem off `-apple-system-body`.
- **Native features** go through `window.gsbc` in `native.js`, which `Native/NativeBridge.cs` calls.
- **Shared components** are in `App/Components` (Page, Masthead, SectionTabs, Section, ListRow, Sheet,
  Cover, SermonRow, …). The AGENTS.md rule applies: extend these before writing new ones.
- **Time** comes from `Data/ChurchClock.cs`, so Device check's live preview can fake a Sunday service.

## Gotchas found on the device

- **`capacitor.js` must be loaded.** The native bridge doesn't create plugin proxies by itself; without
  `wwwroot/js/capacitor.js` (vendored from `@capacitor/core` 8.5.3, MIT) every plugin silently reports
  unavailable. Update it when Capacitor is upgraded.
- **Haptics:** `selectionChanged` does nothing without `selectionStart`, so taps use a light impact.
- **YouTube embeds fail on iOS** with error 153 ("Video Player configuration error"): the web view sends
  no referrer from `capacitor://`. On the phone, play opens the watch page in the in-app browser, and
  "Open in the YouTube app" uses the https watch URL (a universal link). An embed page hosted on the
  church's domain would allow inline playback later.
- **baptist.com.au sends no CORS headers**, so the RSS feed is fetched with CapacitorHttp
  (`capacitor.config.json`); in a desktop browser the bundled snapshot is used.
- **Sermon artwork:** the feed's `itunes:image` is a square crop from the site's resizer. `SermonFeed`
  decodes the resizer's base64 job to get the 1920×1080 original on CloudFront (about 1.6 MB each); the
  real API should serve a resized 16:9 image instead.
- **AOT publish** needs `ILLinkTreatWarningsAsErrors=false` for six trim warnings inside Microsoft's own
  Components and JSInterop assemblies; our code is warning-free.
- **First install on a phone:** Developer Mode only appears in Settings → Privacy & Security after Xcode
  has seen the phone, and the free-team certificate must be trusted under Settings → General → VPN &
  Device Management.

## Status at hand-over (2026-10-26)

Working and checked on an iPhone 15 Pro (iOS 26): the AOT build in the web view, free-team signing,
live sermons over native networking, the bulletin and its section tabs, 16:9 artwork, the prayer form
with Hide my name, and the five layouts (checked in a browser phone frame; installed on the phone).

**Still to confirm on the phone** after the latest fixes: audio with the screen locked and lock-screen
controls, haptics, YouTube in the in-app browser and the hand-off to the YouTube app, and each layout.

**Next steps**

1. The owner shows the five layouts and two palettes to staff and the pastor.
2. Keep the chosen layout; delete the other layouts' folders, their `layout-*.css`, their entries in
   `LayoutCatalog` and `Home.razor`, and the layout picker if only one remains. Then fold good ideas
   from the others into shared components (a playable `SermonRow`, a shared announcement row).
3. Reconcile `DESIGN.md` and `.impeccable/design.json` with the winner (the design detector notes
   small type-scale and radius drift in the layout stylesheets), and fill in Results below.
4. Then the real skeleton per SPEC.md: the native data layer and gRPC API replace `SampleContent`, the
   RSS fetch and local storage. This spike is a reference, not a base to grow.

**Design records:** `PRODUCT.md` (what the app is for), `DESIGN.md` and `.impeccable/design.json` (the
visual system), `.impeccable/surfaces/spikes-app-poc-app.md` (the bulletin direction), and
`.impeccable/mocks/decision/` (the three original options the owner picked from).

**Not for the repo:** `GSBC Logo Pack 2022.zip` in the repo root is the church's original logo pack
(gitignored); the app uses only files cut from it in `wwwroot/img`. Never redraw the logo.

## Results

Filled in after running on the device.
