# GSBC Church App: spec

Status: draft, 2026-10-26. A starting point for discussion, not a commitment.

## 1. Why

Good Shepherd Baptist Church uses Tithe.ly for its app. It works, but it is clunky. It is generic,
slow to update and hard to make feel like *our* church. We want our own app that:

- feels native on iOS and Android, and works offline;
- puts the things people actually open first: this Sunday, sermons, events, groups and giving;
- lets church staff change content and branding **without shipping a store update**;
- is cheap to run on infrastructure we already have (the `gsbc.argo` cluster).

Non-goals for v1: a full church management system (people database, rostering, accounting), live
streaming infrastructure, and anything that needs native code beyond standard Capacitor plugins.

## 2. Shape

One web app, packaged three ways from a single build:

| Target | How | Updates |
|---|---|---|
| iOS | Capacitor shell (WKWebView) | Shell: App Store. Content: over the air |
| Android | Capacitor shell (Android WebView) | Shell: Play Store. Content: over the air |
| Web | The same build as a PWA | Service worker |

**Capacitor only.** Nothing in scope needs native UI. Native capabilities come from official plugins:
Push Notifications, Haptics, Share, Browser, App (Android back button, deep links), Filesystem,
Preferences and Splash Screen.

**UI stack (open question, §9):** Blazor WebAssembly reuses our existing GSBC stack (ImpactKids,
Accounting) and the patterns proven in the Graced POC: native-feel navigation, safe areas and
offline-first. The cost is a 1 to 2 second WASM start on older phones. A JS framework starts faster,
and its code is fully eligible for over-the-air updates (§5).

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

## 4. Content model

Everything that isn't code is **content**: JSON, Markdown, images and fonts, described by one manifest.

```
content/
  manifest.json        # version + every file with its sha256 and size
  theme.json           # colours, fonts, radius, logo → CSS custom properties
  home.json  events.json  groups.json  sermons/series.json  pages/*.md
  img/…  fonts/…
```

```json
{
  "version": 42,
  "minShell": "1.0.0",
  "files": {
    "theme.json":  { "sha256": "…", "size": 812 },
    "events.json": { "sha256": "…", "size": 10422 },
    "fonts/Heading.woff2": { "sha256": "…", "size": 48120 }
  }
}
```

- **Theming is data.** The UI only uses CSS custom properties (`--colour-primary`, `--font-heading`
  and so on). At start-up the app loads `theme.json` and writes it onto `:root`. Changing a colour
  or font is therefore a content change.
- `minShell` stops new content reaching an app version too old to understand it. That app keeps
  its current content and asks the user to update.
- **Authoring (open question, §9):** a git-based content repo edited through a simple web CMS such
  as Decap, or a small admin page in the existing GSBC stack. Either way, publishing builds the
  manifest and pushes the files to static hosting (cluster + Cloudflare).

## 5. Over-the-air updates

### How it works
1. **The app ships with a full content snapshot**, so the first launch and offline use always work.
2. **On launch and on resume,** it fetches `manifest.json` conditionally (ETag) with a short timeout.
   If there is no network, it carries on with what it has.
3. **It downloads only the files whose hash changed** into Filesystem/IndexedDB under a staging
   version, and verifies every hash.
4. **When all the files have arrived, it switches** the "current" pointer to the new version in one
   step. A half-finished download is never used. The previous version is kept for rollback.
5. **New content is applied** on the next screen change or the next launch, never mid-read. Urgent
   items such as service cancellations can be pushed and shown immediately.

Lookup order for every file: **downloaded current version → bundled snapshot**.

### What the stores allow
Policy summary from memory. Check it against the current App Review Guidelines, the Apple Developer
Program License Agreement and the Google Play Developer Policy before relying on it.

- **Apple (Guideline 2.5.2, DPLA §3.3.1(B)):**
  - Apps must be self-contained and must not download code that adds or changes features.
  - Downloaded *interpreted* code (HTML, CSS, JS run by WebKit) is allowed. The limits: it must not
    change the app's primary purpose, add a storefront, or get around review or payment rules.
- **Google Play (Device and Network Abuse):** apps must not download executable code (dex, JAR,
  `.so`) from outside Play. JavaScript in a WebView is exempt.

What that means for each kind of change:

| Change | Store update? |
|---|---|
| Colours, fonts, logo, images | No |
| Sermons, events, groups, pages, announcements | No |
| Small CSS/layout or JS bug fixes | No (allowed; keep them small) |
| Blazor `.wasm`/`.dll` changes | **Yes**. Treat as code, grey area at best |
| New features, new payment flows, anything changing what the app *is* | **Yes**, always |

Store reviews are typically a day or two now, and both stores support phased rollouts. Code
releases are routine, not an ordeal.

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

## 7. Backend

Kept deliberately small, and run in the `gsbc.argo` cluster alongside the other GSBC apps:

- **Static content host:** manifest + files behind Cloudflare. Use `no-cache` on `manifest.json` and
  immutable caching on hashed files.
- **Push:** Firebase Cloud Messaging (Android, and APNs relay for iOS), sent from the admin side.
  Device tokens are stored per topic.
- **Forms:** prayer requests and "I'm interested" go to the right staff inbox. Prayer requests are
  stored only as long as needed.

## 8. Privacy and safety

- No third-party analytics or ad SDKs, which also keeps the store privacy labels short.
- Prayer requests are sensitive. Use HTTPS only, give staff least-privilege access, and offer a
  retention period and a delete option.
- No photos of children in content without consent. This follows the church's existing
  child-safety policy.
- Secrets (push keys, payment keys) never go in this repo. **It is public.**

## 9. Open questions

1. Blazor WASM (reuse our stack) or a JS framework (faster start, fully over-the-air)?
2. Content authoring: a git-based CMS or an admin page in the existing GSBC stack?
3. Does ImpactKids (kids check-in) connect to the app, for example parent notifications?
4. Giving: stay with Tithe.ly's giving page, or move to Stripe? Is ACNC registration in place for
   Apple's nonprofit programme?
5. Who publishes to the stores? An Apple Developer account in the church's name (organisation
   enrolment needs a D-U-N-S number) and a Google Play developer account.
6. What from Tithe.ly do people actually use today? Measure before migrating.

## 10. Milestones

1. **Spike:** Capacitor shell + one screen + theme.json applied at runtime, on a real iPhone and
   Android device.
2. **Content sync:** manifest, hashed downloads, atomic switch-over, bundled fallback. Prove it by
   changing a colour and a font with no store release.
3. **v1 features:** Home, Sermons, Events, Groups, Give (link out), push.
4. **Store submission:** TestFlight / internal testing, then public release.
5. **v2:** in-app giving (approved nonprofit), prayer wall, sign-in if needed.
