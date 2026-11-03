# Analytics

Status: **phase 1 built, 2026-11-03** (branch `feature/analytics`): error reporting from the app, the admin
panel, the server and iOS native crashes; App health; usage counts and Insights; the Settings switch.
What was built, and where it differs from the proposal below, is in [As built](#as-built-2026-11-03). The
owner's decisions are marked *(owner, 2026-11-02)* and *(owner, 2026-11-03)*.

The owner's brief: app usage numbers that church staff, who aren't technical, find friendly and
useful. Error reporting is covered too, because it uses the same plumbing.

## In one paragraph

The phone counts what happens on it: it was opened this week, a sermon was played, a notice was seen.
It sends **totals per day, with no identifier of any kind**, through the existing outbox to a gRPC
method. The server only ever holds numbers per day. Staff given the **Insights viewer** role see a
plain-language page: how many phones opened the app this week, whether that's growing, and what's
most popular. App errors travel the same way, to an Owner-only health page. There's no third-party
SDK, no new endpoint and no personal information. Anyone can turn it off in Settings.

## The questions staff want answered

All four groups are wanted in the first version *(owner, 2026-11-02)*. In staff's words:

| Question | Answered by |
|---|---|
| "How many people opened the app this week?" | Phones that opened the app this week, and this month |
| "Is the app growing?" | A 12-week trend of the weekly number, new installs per month, and "same week last year" once there is a year |
| "Who's using it: just the keen ones?" | The weekly number split three ways: not signed in, signed in, on a ministry team ([below](#three-groups-not-people)) |
| "Which sermons are people listening to?" | Plays per sermon and series, audio or video, and how far audio gets (started, halfway, finished) |
| "Did people see the notice about X?" | Each announcement, pinned notice and push: seen on how many phones, opened on how many |
| "What do people actually tap?" | Hub tile taps, events opened and added to the calendar, ministries viewed, "ask to join" and Join taps, Give tapped (never amounts) |
| "Are people on iPhone or Android, and on the latest version?" | Platform and app version on the headline number |

### Not worth measuring

- **Anything about a person:** who opened what, or whether Sam has seen the notice. The design
  can't answer these, on purpose.
- **Time in the app, session length, scroll depth, screen-by-screen paths.** They're hard to read
  well, they invite "engagement" thinking that doesn't suit a church, and nobody acts on them.
- **Search terms and anything typed.** Free text can hold names and pastoral detail.
- **Prayer:** which prayer items are viewed, or anything about a request. Prayer's own records already
  show how many requests arrive.
- **Giving amounts or frequency.** Tithe.ly already reports giving, and it's no business of analytics.
  Only "Give was tapped" is counted.
- **Location, contacts, device identifiers (IDFA, IDFV, Android ID), IP addresses.**
- **The admin panel's own use.** It has a handful of users, who can just be asked.
- **YouTube watch time.** YouTube Studio and the podcast host already report it for every viewer. The
  app counts only that a video was opened in the app.

## Privacy first

### What the law and the church expect

- **The Privacy Act 1988 and the APPs.** The church may fall under the small-business exemption, but
  the design assumes the APPs apply anyway: the exemption may go in future reforms, and the
  congregation expects it. The aim is stronger than complying: **analytics collects no personal
  information**, so APP 3 (collect only what's necessary), APP 6 (use), APP 8 (sending data
  overseas) and APP 11 (security and deletion) have almost nothing to bite on. App error reports are
  designed the same way.
- **Children's online privacy.** The 2024 amendments told the OAIC to write a Children's Online
  Privacy Code for online services likely to be used by children. Impact Youth's members may well
  use the app. Holding no personal information is the simplest way to stay clear of it. Check the
  code's final wording when it's registered.
- **Safe Ministry.** Nothing in analytics can identify a child or show what a particular young person
  did. Children's and youth ministry content is only ever counted in totals, with the small-number
  rule below.
- **The statutory tort for serious invasions of privacy** (in force since June 2025) is one more reason
  not to hold per-person activity at all.

### What is collected

Each report the phone sends holds:

- **the day** (Brisbane date) that the counts are for;
- **counts per measure:** a measure is a name declared in code (`sermons.played`), optionally
  with a subject (the sermon's id). Each feature declares its measures, the same way it declares
  permissions ([below](#how-it-fits-the-codebase)). Free-text properties aren't possible;
- **once-only flags** the phone decides for itself: "first open this day / week / month", "first
  open ever after install", "also opened last week";
- **coarse context:** platform (iOS or Android), app version, OS major version, and which of the
  three groups the phone is in.

**No identifier** *(owner, 2026-11-02)*: no account, no install ID, not even a rotating key. Distinct
counts work because **the phone counts itself once**. It remembers that it already said "active this
week" and so doesn't say it again, and it remembers which notices it has already reported as seen.
That memory stays on the phone and is pruned when a notice leaves its feed.

What this can't do, and why that's fine: it counts **phones, not people** (one person with an iPad
counts twice; a couple sharing a phone counts once), a reinstall counts as a new install, and
there are no cohort questions ("of April's new installs, how many stayed"). Every number on screen
says "phones" and explains this in one line.

### Three groups, not people

*(owner, 2026-11-02)* The owner's concern: people who sign in or serve on a team are more engaged
than the church as a whole, so one number can hide who it really reflects. So the headline numbers
and the top lists can be split three ways:

| Group | Means |
|---|---|
| **Not signed in** | The app has no account on this phone |
| **Signed in** | Signed in, not on a team |
| **On a team** | Signed in and holding any ministry role: member, leader, admin or staff |

- **Staff are never a group of their own.** There are too few of them, so "staff who saw notice X: 2"
  would all but name them.
- **The phone works out its group** from what it already holds (its session and its grants), and sends
  only the group's name. "On a team" waits for Ministries and for Access's private grants feed on the
  phone; until then there are two groups.
- **Only for the headline numbers and top lists.** Per-notice counts aren't split.
- A phone is counted in the group it was in at its first open of the week.

### The small-number rule

**Any count below 5 shows as "fewer than 5"**, everywhere: on screen, in a group split, on a
notice's line. A small ministry's members-only notice can't reveal which two people opened it.
It's one rule rather than a judgement per screen.

### Retention

*(owner, 2026-11-02)*

| What | Kept |
|---|---|
| Daily counts | 25 months (so this Easter can be compared with last Easter), then folded into monthly totals |
| Monthly totals | Indefinitely. They're numbers, not personal information |
| App and server error reports | 30 days; an error kind not seen for 30 days is dropped |
| Report request ids (to drop resent reports) | 7 days |

### Consent and opting out

*(owner, 2026-11-02)* **On by default, with a switch in Settings:** "Help improve the app: send
anonymous counts of what's used, and error reports. Nothing about you is sent." Turning it off stops
both and discards anything not yet sent. No first-launch prompt. Apple's App Tracking Transparency
doesn't apply, because nothing links data across apps or companies, and the SDKs that need it are
absent.

### What the privacy notice would say

> **How we learn what's useful.** The app counts how it's used (how many phones opened it this week,
> which sermons were played, how many phones a notice reached), so the church knows what's helpful.
> These are totals only. They don't include your name, your account, an ID for your phone, your
> location or anything you type, and we can't tell what any one person did. If something goes wrong
> in the app, it sends an error report (what failed, the app and system version) with nothing about
> you in it. Totals are kept for about two years and then summarised by month; error reports are
> deleted after 30 days. Everything stays on the church's own systems; no analytics company is
> involved. You can turn this off in Settings → Help improve the app.

**Store privacy labels:**

- **Apple:** "Data Not Linked to You": Usage Data (product interaction) and Diagnostics (crash
  data). Not used for tracking.
- **Google Play's Data safety:** app interactions and crash logs, collected, not shared, not
  linked to an identity.

Check both against the stores' wording at submission.

### Never collected

Names, emails or account ids; any device or install identifier; IP addresses (Cloudflare and the
gateway see them in transit, as for any request, but analytics never stores them); location;
contacts; anything typed; exact times (only the day); the content or subject of prayer requests;
giving amounts; anything from the admin panel.

## Architecture

### Options

| | In-house (recommended) | Self-hosted web analytics (Umami, Plausible CE) | App analytics services (Aptabase, TelemetryDeck) | PostHog (cloud or self-hosted) | The stores' own (App Store Connect, Play Console) |
|---|---|---|---|---|---|
| Fits "gRPC only" | Yes: one gRPC method | No: their trackers post to their own HTTP endpoint, a new server-side host, so the owner's yes is needed | Cloud: the SDK posts from the phone to them (no endpoint of ours). Self-hosted: same problem as the left | Same as the left | Nothing to add |
| SDK in the app | None | A JS tracker in the WebView | Their SDK (Swift, or .NET built for MAUI; WASM-AOT unproven) | Large JS/native SDK | None |
| SPEC §11 ("no third-party analytics SDKs") | Keeps it | Breaks it | Breaks it | Breaks it | Keeps it |
| Offline | Outbox, already built | Lost when offline | Their own queue | Their own queue | n/a |
| Data location (APP 8) | Our Postgres | Ours | EU or US | US or EU | Apple, Google |
| Cost to run on the Mac mini | One small table | Plausible needs ClickHouse; Umami is light | Self-hosting needs ClickHouse | Heavy (ClickHouse, Kafka) | Free |
| Staff-friendly page | We build it, in our own words | Web-site oriented (pages, referrers) | Developer-oriented | Developer-oriented | Owner's store accounts only |
| Per-person tracking | Impossible by design | Hashes per day | Pseudonymous IDs | Per-person by default | Opted-in users only |

**Recommendation: in-house, plus the stores' own consoles.** The questions are few and fixed, the
plumbing exists (outbox, gRPC, Postgres, the admin panel), and every alternative either adds an
SDK to an AOT app, a non-gRPC path, a heavy service on the mini, or data overseas. The stores'
consoles are free and complementary: installs, store page views and native crash reports from
opted-in users, all with no code. The owner already has Metabase on the cluster. Pointing it
read-only at the `analytics` schema would give the owner ad-hoc questions later without building
them (owner only, never staff; not decided).

Cloudflare Web Analytics stays off ([offline-and-caching.md](../architecture/offline-and-caching.md)):
it injected a third-party beacon into every HTML page of the admin panel, nobody used it, and the app
isn't served from the web anyway.

### How a count travels

1. **A feature records:** `IUsage.Count(SermonMeasures.Played, sermon.Id)` (Platform.Client). It's a
   no-op when the Settings switch is off, and it never touches the network.
2. **The phone sums** into local counters per day, measure and subject, and applies the once-only
   rules (first open this week, notice already seen).
3. **It reports** when the app goes to the background (at most once an hour) and at the first
   open of a new day: one `Analytics.Report` request holding every unsent day's counts, put in the
   **outbox** so it survives no signal. Each report is a set of additions, sent once: the outbox's
   `gsbc-request-id` lets the server drop a resent one. Counts carry their own day, so a report sent
   on Tuesday from Sunday's aeroplane mode lands on Sunday.
4. **The server validates** (known measures, flags at most 1, sane maxima per report) and adds the
   numbers to `analytics.daily_counts`. There's no raw-event table, because there are no events
   on the server, only sums.
5. **A nightly job** (Brisbane time, one replica, like Sync's pruning) folds daily rows older than 25
   months into `analytics.monthly_counts` and prunes error reports.

**The outbox needs a quiet kind of entry.** Today every entry shows to the person as pending, and as
"couldn't send" when it gives up. Analytics and error reports must never appear there, and must be
discarded silently when they give up (three days). That's a small change to the native outbox (a flag
on the entry) and to `IOutbox`, made when this is built.

**Tier: `[Public]` until App Attest works, then `[AppInstall]`.** App Attest waits on the Apple
enrolment, and analytics shouldn't wait for it. Anyone could send made-up counts to a public method.
The stakes are low (the numbers guide, they don't decide), and the validation above caps what one
report can add. The assertion App Attest adds proves the app is genuine; it isn't stored with the counts.

**A rate limit of its own.** `RateLimits.PublicWrites` allows 10 a minute per IP address, and on
Sunday the whole congregation is behind the church Wi-Fi's one address. Analytics needs a policy
sized for that (hundreds of phones, one report an hour each), and a rejected report simply stays in
the outbox and goes later.

### Storage

Schema `analytics`:

- `daily_counts`: day, measure, subject (nullable), group, platform, app version, count. Unique on
  everything but count; reports upsert into it.
- `monthly_counts`: the same, by month, after 25 months.
- `reports_seen`: request id and when, for 7 days.
- `error_kinds` and `error_reports` (below).

At a church's scale (hundreds of phones, a few dozen measures and subjects a day) this is tens of
thousands of rows a year. Postgres needs nothing special.

### Error reporting, alongside

*(owner, 2026-11-02: in-house, through gRPC)*

- **App errors.** An unhandled Blazor exception runs inside the WebView, so it's invisible to Apple's
  and Google's crash reports. Only the app itself can report it. The app catches unhandled exceptions
  (Blazor's error boundary and the WASM runtime's handler) and queues a quiet `Diagnostics.Report`:
  app version, platform, OS major version, exception type, our stack frames (method names only, no
  file paths), and the page's route template (`/sermons/{id}`, never the id). **No exception
  message**, since messages can carry names and emails, and no account or device id. Same Settings
  switch, same outbox.
- **Native crashes** (Swift, Kotlin): iOS's MetricKit hands the app its own crash diagnostics on the
  next launch with no SDK, and Android's `ApplicationExitInfo` does the same. The native layer forwards
  them through the same method. Later phase; the stores' consoles cover the meantime.
- **Server errors.** A gRPC interceptor beside the tier interceptor counts failed calls per method and
  status per day (no request contents), and groups unhandled exceptions by type and stack. Logs keep
  the detail, as now.
- **Grouping.** Reports are grouped into an **error kind** by exception type and top frames, with a
  first seen, last seen, count, and the app versions affected.
- **The Owner hears about new ones:** the first time a new kind appears, an email to holders of
  `analytics.health.view` through the existing email queue ([email.md](../architecture/email.md)),
  at most one per kind per day.
- **Not built:** a full observability stack (Grafana, Loki, OpenTelemetry collectors). The mini
  cluster is memory-bound, and the questions here don't need one.

### How it fits the codebase

A new feature, **Analytics** (`Features/Analytics`: Contracts, Api, Client), plus small pieces in
Platform, because every feature records counts and features can't reference each other:

- **Measures are declared in code**, like permissions and surfaces: `Measure` in Platform.Contracts
  (key, plain label, "what it counts" sentence, subject kind, whether it can be split by group),
  registered with `AddMeasures(...)`. The server rejects unknown measures, and the privacy notice
  can be checked against the catalogue. Being declared rather than reflected keeps it AOT-safe.
- **`IUsage`** in Platform.Client records counts. The Analytics feature implements the counters and
  the reporting, and the app works without it (a host that leaves Analytics out gets a no-op).
- **Subject names** ("Job 33: The breath of the Almighty" rather than an id) come through a small
  interface each feature implements, as `IScopeNames` does for places.
- **Services:** `Analytics.Report` and `Diagnostics.Report` (`[Public]`, later `[AppInstall]`);
  `Insights.GetSummary`, `Insights.GetTop`, `Insights.GetSeen` (`[Requires("analytics.view")]`);
  `Health.ListErrorKinds`, `Health.GetErrorKind` (`[Requires("analytics.health.view")]`). All gRPC, so
  **nothing needs the owner's approval under the hard rule.**
- **Reads are online,** like Access's screens: the Insights page asks the server. There's no feed,
  because nothing here is needed offline.
- The project map and the picture get the three new projects when it's built.

## Who sees it

*(owner, 2026-11-02: the Insights role only)*

| Permission | Plain words | Held by |
|---|---|---|
| `analytics.view` | "See how the app is used" | **Insights viewer**, a new system role, given by Access managers (and the Owner) to whoever should see it: the pastor, an elder, the office |
| `analytics.health.view` | "See app errors and get error emails" | The Owner (through `*`); not in Insights viewer |

Nobody holds `analytics.view` by default, not even the Staff role. Ministry leaders don't get a
per-ministry view. Revisit if staff ask; the counts already carry the subject, so it would be a
screen and a scoped permission, not new collection. Neither node is sensitive, because neither
reveals anything about a person.

## What staff see

One page, **Insights**, in the admin panel only *(owner, 2026-11-03)*. It's written as sentences, not
dashboards:

1. **Three headline numbers, each compared with last week:**
   - "**312 phones** opened the app this week, 8% more than last week."
   - "**24 new** this month."
   - "**140 sermon plays** this week."
2. **Who that is,** as one line: "180 not signed in · 90 signed in · 42 on a team."
3. **The trend:** the weekly number over the last 12 weeks as one line, with last year's line faint
   behind it once there is a year. Easter and Christmas show without explanation.
4. **Most popular this week:** top 5 sermons, events, notices and Hub tiles, each a short ranked list
   with counts.
5. **Notices:** recent announcements, pinned notices and pushes, each with "Seen on 142 phones ·
   opened on 38". Push adds "sent to 410 phones", which the server knows from its own topic subscriptions.
6. **iPhone and Android, and how many are on the latest version**, folded away at the bottom.

Throughout:

- Every number has a one-line "What does this count?" ("Phones and tablets that opened the app at
  least once, Monday to Sunday. One person with two devices counts twice.").
- Weeks run Monday to Sunday, Brisbane time, so Sunday's numbers land in the week they belong to.
  Numbers are as of the last report, typically within the hour.
- The small-number rule applies everywhere.
- **No raw event explorer, no filter builder, no export of raw data, nothing per person.**
- Holders of `analytics.view` also see the "Seen on N phones" line **where they edit the notice**,
  which is the moment they ask the question. The notice's feature shows it through a small Platform
  interface, so it needs no reference to Analytics.

The Owner's **App health** page lists error kinds (newest first: what, where, how often, which app
versions, first and last seen), and failed calls per method this week.

**Shared components** (Platform.Ui, generic, per AGENTS.md): a `StatTile` (number, label, change
against the last period, explanation), a `TrendLine` (plain SVG line for one or two series, no
JavaScript chart library, so it stays AOT-safe), and a ranked list (an extension of `GroupedList` if
it fits, otherwise a `RankedList`). Other features can use them (a ministry's roster fill, say).

**Weekly email:** later, if staff ask *(owner, 2026-11-02)*. When it comes: an opt-in per Insights
viewer, Monday morning, the headline numbers and top 5 through the existing email queue.

## Phases

**0. Now, no code (owner, once the store accounts exist).** App Store Connect and Play Console:
installs, store page views and native crashes from opted-in users.

**1. The smallest useful version: with Home, before the first TestFlight build.** The store
labels and the privacy notice are then right from the first submission, and the numbers start from
day one.

- Measures, `IUsage`, the phone's counters, the quiet outbox entry, `Analytics.Report`, the
  `analytics` schema, the nightly fold.
- Measures: app opened (day, week, month, new install, returning), Hub tile tapped.
- Groups: not signed in and signed in ("on a team" arrives with Ministries and the grants feed).
- The Insights page: the three headline numbers (sermon plays once Sermons exists), the group line,
  the 12-week trend. Insights viewer and the two permissions.
- App errors (Blazor) and server errors, the App health page, new-kind emails.
- The Settings switch, the privacy notice, the store labels.

**2. As each feature is built.** Each feature's doc gets a **Measures** section and declares its
measures with its code: Sermons (played, halfway, finished, audio or video), notices and pushes
(seen, opened, and "Seen on N phones" in their editors), Events (opened, added to calendar),
Ministries (viewed, ask to join, Join, and the "on a team" group), Give (tapped). "Most popular this
week" appears once two features report.

**3. Later.**

- MetricKit and `ApplicationExitInfo` native crash reports.
- `[AppInstall]` on the report methods once App Attest works.
- "Same week last year" once there's a year.
- If asked: the weekly email, a per-ministry view for leaders, Metabase for the owner.

## As built (2026-11-03)

### Decided when building *(owner, 2026-11-03)*

1. **Error reports never carry the exception's message**, in any environment: the type, our method
   names (no paths, arguments or build-specific numbers) and the route template only.
2. **Failed gRPC calls are counted on both sides.** The API's own interceptor (in front of the tier
   check) counts every failed call by method and status per day; the phone and the admin panel also
   report failures the server can't see (the gateway, the tunnel, dropped trailers) and the ones
   `LiveData` swallows. Phones don't report Unauthenticated, Permission denied or Cancelled (the server
   counts the first two), nor network failures while offline.
3. **iOS native crashes now, through MetricKit**, not in phase 3. The plugin collects MetricKit's crash
   diagnostics on the next launch and C# reports them like any other error. Their frames are a binary
   and an offset; Xcode's Organizer has the same crashes symbolicated.
4. **The admin panel reports its unhandled errors too**, straight to `Diagnostics.Report` (a browser has
   no outbox). It still sends no usage counts.
5. **Insights is in the admin panel only**, not in the app, even for holders of `analytics.view`.

### The recommendations, as settled

- **The quiet outbox entry is a flag** on the native outbox (`quiet`), not a second queue
  ([offline-and-caching.md](../architecture/offline-and-caching.md)).
- **The phone's counters live in the native preferences** (`ILocalPreferences`, UserDefaults on iOS), as
  one small JSON document: what's waiting to be sent, and the phone's memory of when it last opened
  (`OpeningMemory`).
- **Tier `[Public]`** for both report methods, with their own rate-limit policy, `RateLimits.Reports`:
  600 a minute per IP address. A rejected report stays in the outbox. Revisit after a real Sunday.
- **Reports are sent** at the first open of a new day and when the app goes to the background, at most
  once an hour; error reports go three seconds after the first error, grouped.
- **Which environment:** each environment reports to its own backend; App health names it
  ([environments.md](../architecture/environments.md)).
- **New-error emails** go to everyone holding `analytics.health.view` (through `IPermissionHolders` and
  `IAccountEmails` in Platform.Api, implemented by Access and Accounts), once per new kind, at most ten a
  day. A kind that comes back after being pruned is new again.

### What's built

| Part | Where |
|---|---|
| `Diagnostics.Report`, `Analytics.Report` (`[Public]`), `Health.GetOverview`, `Health.GetErrorKind` (`analytics.health.view`), `Insights.GetSummary` (`analytics.view`) | Analytics.Contracts, Analytics.Api |
| Schema `analytics`: `error_kinds`, `error_reports`, `failed_calls`, `daily_counts`, `monthly_counts`, `reports_seen`; pruning and the 25-month fold every six hours, one replica | Analytics.Api |
| `IClientDiagnostics` and `CurrentPage` (the route template); every feature's calls go through `ObservedChannel` | Platform.Client |
| `Measure`, `AddMeasures`, `MeasureCatalog`, `ISubjectNames`, `IUsage` | Platform.Contracts, Platform.Api, Platform.Client |
| The error reporter (a logger provider for unhandled render errors, unhandled and unobserved exceptions, failed calls, native crashes) and the usage counter | Analytics.Client |
| App health and Insights, both in the admin panel only | Analytics.Admin |
| "Help improve the app" under More: the switch and the privacy notice | Analytics.App |
| `StatTile`, `TrendLine` (plain SVG, two series at most: this year in the accent, last year in the de-emphasis gray, with a crosshair, a legend and a table view), `RankedList` | Platform.Ui |
| Quiet outbox entries, MetricKit crash collection | the native data layer (iOS) |

Measures declared so far: `app.active.day`, `app.active.week`, `app.active.month`, `app.installed`,
`app.returning` (Analytics) and `hub.tile.tapped` (Hub, named through Configuration's `ISubjectNames`).

### Not yet

- **"On a team"**: needs Ministries and the private grants feed on the phone. Phones are "not signed in"
  or "signed in" until then.
- **Sermon plays** as a headline number (the third tile is "phones this month" until Sermons exists),
  notices and pushes ("Seen on N phones"), events, ministries, Give: each feature declares its measures
  when it's built.
- **Android**: the Kotlin layer is stubbed, so Android phones report nothing yet.
- **Same week last year** draws by itself once there's a year of data.
- **`[AppInstall]`** on the report methods once App Attest works.
- **The privacy notice and store labels** as text for the website and the store listings (the in-app
  notice is under More → Help improve the app).

## Decided (owner, 2026-11-02)

1. All four question groups in the first version: use and growth, sermons, notices seen, what people tap.
2. No identifier of any kind; the phone counts itself once.
3. Segmentation into three coarse groups (not signed in, signed in, on a team), staff never alone,
   headline numbers and top lists only, and counts below 5 shown as "fewer than 5".
4. On by default, with a switch in Settings; no first-launch prompt.
5. Only the Insights viewer role sees it; nobody holds it by default.
6. Error reporting in-house through gRPC; no Sentry, GlitchTip or SDK.
7. No weekly email for now; later if asked.
8. Daily counts kept 25 months, then monthly totals; error reports 30 days.

## Open

- **The rate-limit policy's numbers** (600 a minute per address for now), sized on a real Sunday.
- **Check at submission:** the stores' privacy-label wording, and the OAIC's Children's Online Privacy
  Code once registered.
