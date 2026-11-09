# Notifications and push

How any feature tells people something: an inbox in the app, a push on the lock screen, and the silent
push that makes the phone sync. The owner (2026-11-03): "We should make a proper notifications system
for this. There will be a lot of places they could come from." Backbone, like drafts: designed and
decided before anything raises one.

**Status: approved by the owner, 2026-11-03,** with one change (decision 9). Built, and running on phones through UAT; see *Status* at the end. Following (decisions 1, 2 and 4) is built as parts only: the Follow button waits for Ministries.

## Decisions (owner, 2026-11-03)

1. **Phones follow without signing in.** A signed-out phone can follow a Life Group or a ministry,
   and then gets its notices, as life-groups.md first planned. Because the audience always comes
   from Access, **Access gains a second kind of holder: a phone** (an install), beside a person.
2. **Following gives a phone notices, and for a Life Group it is joining.** A *Follower* role at a
   ministry. It opens nothing members-only; for a Life Group it is life-groups.md's per-phone Join,
   counted in the leader's "14 joined".
3. **A phone proves it is that phone with App Attest,** so the `[AppInstall]`
   tier (SPEC §6) is built first. Registering a push token and following are App writes.
4. **Signing in moves a phone's follows to the person,** so they follow on every device they sign
   in on.
5. **Staff announcements: compose, test, send.** An announcement is its own record (drafts decision
   1), written in the admin panel, tried on your own phone, then sent now or scheduled. Sending is a
   permission; anyone else gets *Ask to send*, listed for senders, as *Ask to publish* is.
6. **Email isn't a channel yet.** Inbox and push only. Security notices, which must reach you, also
   email. Each kind says whether email is allowed, so it can be added per kind later.
7. **Church-wide quiet hours.** Non-urgent pushes raised between 9pm and 7am (Brisbane) go out at
   7am; they reach the inbox at once. Urgent and security pushes go straight away.
8. **A notification about a thing opens that thing** when it's tapped, on the lock screen or in the
   inbox: the event, the ministry notice, the message, the draft. Every such notification carries
   the thing's in-app link, and raising one without a link is refused.
9. **No one rate limit for every kind** (owner, at approval: "3-per-hour doesn't make sense for a busy
   ministry chat"). Each kind declares its own pacing. A chat message pushes every time, as a
   messaging app does, grouped by chat in Notification Centre, and a chat can be muted. Kinds that
   staff or leaders raise (notices, announcements) are the ones with a limit.
10. **One notification per thing** (owner, 2026-11-08, from a sermon that arrived twice: "New sermon: …" and
    "New in Summer Fit: …"). A person, or a signed-out phone, gets at most one notification, in the inbox and as a
    push, for one thing. When several reasons apply, the most specific wins; muting falls back to the next reason
    (see *One notification per thing* below).
11. **Turned off means not in the inbox either** (owner, 2026-11-08: "nothing. inbox is for notifications"). A kind
    someone has turned off reaches neither their inbox nor their phone; with several related ones, they get the most
    specific one they haven't turned off, and if they've turned off all of them, nothing. Kinds that can't be turned
    off (`Mandatory`) always arrive. Turning a kind off applies from then on (see *People's control*).

## The pieces

```
Feature (Configuration, Ministries, Accounts, …)
   │  INotifications.RaiseAsync(kind, audience, title, body, link, subject)     Platform.Api
   ▼
Notifications feature (Api)
   ├─ resolves the audience through Access      → people and phones        IPermissionHolders
   ├─ applies preferences, rate limits, collapsing, quiet hours
   ├─ writes the inbox                          → notifications.inbox  (own records, per person)
   │                                            → notifications.public (public, for phones)
   └─ queues deliveries                         → APNs now, FCM later (outbound only)

Phone: native layer registers the APNs token, wakes Sync on a silent push, and hands a tapped
notification's link to Blazor, which opens it.
```

## Sources: kinds, declared like surfaces

A feature declares each **kind** of notification it raises in its Contracts, the way it declares
surfaces and permissions, and registers it on the server. Features never reference the Notifications
feature, only Platform.

```csharp
public static class DraftNotifications
{
    public static readonly NotificationKind WaitingToPublish = new(
        "config.drafts.waiting", "A draft is waiting for you to publish", "Drafts", Waits: true, For: HubSurfaces.Edit);

    public static readonly NotificationKind SharedWithYou = new(
        "config.drafts.shared", "Someone shares a draft with you", "Drafts", Waits: true,
        For: ConfigurationPermissions.PreviewDraft);
}
```

A kind has:

| Part | Means |
|---|---|
| Key, description, area | Shown in preferences, grouped by area, in the same voice as abilities ("Can …" becomes "Tell me when …") |
| `defaultPush` | Whether it's on (inbox and push) for someone who hasn't chosen (decision 11) |
| `Mandatory` | Can't be turned off (security). Implies push |
| `PerScope` | `null`, or a scope kind such as `Ministry`: the switch is then per ministry ("Music: notices") |
| `MayBeUrgent` | Whether a raise may be urgent (skips quiet hours, time-sensitive on iOS) |
| `Pacing` | `null` (each pushes: chat messages, security), or `NotificationPacing(pushes, per)`: past that many pushes per recipient and ministry in that time, they collapse (below). Declared per kind (decision 9) |
| `Waits` | It asks something of you (a draft to publish, a draft shared with you, an announcement to send). It shows under *Waiting* in the inbox until the feature **settles** its subject (below) |
| `For` | The permission its audience holds. The phone's preferences list only kinds a person could get, checked with `IAccess.CanSomewhere` (held anywhere, at any scope); a kind without one is for everyone (owner, 2026-11-03: "through permissions") |
| `InAdminPanel` | Its task is done in the admin panel (an announcement waiting to send). On the phone the row says so and doesn't open anything |

`Platform.Contracts.Notifications` holds `NotificationKind` (AOT; the phone reads it).
`INotifications` (Platform.Api, namespace `Platform.Api.Notifications`) is the one call a feature makes:

```csharp
await notifications.RaiseAsync(new Notification(
    DraftNotifications.WaitingToPublish,
    NotificationAudience.Holding(area.PublishPermission, scopeId),
    Title: $"“{draft.Name}” is waiting to publish",
    Body: $"{author} asked for it to be published. Publish it from the admin panel.",
    Link: $"/previews?draft={draft.Id}",
    Subject: $"draft/{draft.Id}",
    Action: "Preview it"), cancellationToken);
```

- **`Action`** (optional) names what opening it does, in the inbox's words: *Preview it*.
- **Settling.** `INotifications.SettleAsync(subject)` marks every waiting notification about that subject
  done and re-sends its inbox (and public) rows, so it leaves *Waiting* on every device. Drafts settle
  `draft/{id}` when a draft is published or deleted; Announcements settle `announcement/{id}` once it's
  sent, scheduled or deleted. Tested in `NotificationsTests` and `DraftEditorTests`.
- **`Link`** is an in-app route, checked by `LinkTargets` (`InApp` only). **`Subject`** names the
  thing (`draft/42`, `event/9`, `ministry/music/notice/17`). A notification with a subject must have a
  link (decision 8); `RaiseAsync` throws otherwise, and a test raises every declared kind in its
  tests to check it.
- **Raised after commit,** like email: `RaiseAsync` writes a row and returns; delivery is in the
  background. A failure to deliver never fails the feature's call.
- **One notification per thing (decision 10).** When a feature has several notifications about the same thing for
  audiences that overlap (a sermon for followers of its series and for followers of every sermon; *Tell people* for
  each role an event is shown to), it raises them together, most specific first:
  `INotifications.RaiseOneOfAsync([inSeries, anySermon])`. Each is still its own record, and they share a `OneOf` id
  and a `Rank` (0 is most specific). For each person and each phone the raiser picks one, with
  `OneNotification.Choose` (Notifications.Contracts): the most specific they're in the audience of **and** haven't
  turned off; if they turned off every one they qualify for, nothing (decision 11). Only the chosen one gets an inbox row and a push, so
  unread counts, the badge and "N new" count it once. Public rows carry `OneOf` and `Rank`, and a signed-out phone's
  inbox applies the same rule to the rows it follows, using the choices kept on the phone
  (`OneNotification.OnePerThing`), offline. Separate raises are separate things, even with the same subject.
  Tested in `OneNotificationTests` (the rule) and `OneNotificationDeliveryTests` (rows and pushes, and nothing when
  every reason is turned off).
- On start-up the server reconciles every registered kind into the public **`notifications.kinds`**
  feed, as Access does with the permission catalogue, so the phone's preferences list them without
  knowing the features.

Known sources, and how each will raise:

| Source | Kind | Audience |
|---|---|---|
| Drafts | waiting to publish; shared with you | holders of the area's publish permission; the person |
| Announcements | church-wide announcements | as staff pick (below) |
| Account and security | a new passkey was added, a session signed in, recovery used | the person; mandatory, also email |
| Ministries and Life Groups | important notice, new chat message, roster ("You're on Sunday") | holders of `ministries.notices.receive` at the ministry; chat members; the rostered person |
| Life Groups | "Choose your group for Round 3" | everyone who followed a group last round |
| Events (Elvanto) | changed or cancelled; reminders | followers of the event's ministry, or everyone |
| Prayer | your request was prayed for / answered | the person |
| Sync | "new data, fetch it" | silent; not a kind, not in the inbox (below) |

## Audience: always Access

```csharp
NotificationAudience.Everyone                         // every phone and every person
NotificationAudience.SignedIn                         // every person
NotificationAudience.Person(userId)
NotificationAudience.Holding(permission, scopeId?)    // people and phones holding it there
```

There is no list of recipients anywhere in a feature. "Members of Music" is
`Holding("ministries.notices.receive", "music")` once Ministries exists; "people who can publish the Hub" is
`Holding(HubSurfaces.PublishNode)`.

**Access is extended in two ways:**

- **`IPermissionHolders`** (Platform.Api, implemented by Access): `HoldersAsync(node, scopeId)`
  returns every person and phone whose compiled grants allow the node at that scope, including
  wildcards, denials, roles that apply to a ministry's teams, and end dates. It reads the same
  compiled grants the evaluator caches, so "who gets this" and "who may see this" can't disagree.
- **Phones as holders.** A phone's roles are kept apart from people's, in
  `access.phone_assignments` (install, role, place, end date), through `IPhoneRoles`
  (Platform.Api). Only roles declared `PhoneHoldable` can be given to a phone (Follower, today), so a
  phone can never hold anything else. They compile through the same `RoleGrants`, so
  `IPermissionHolders` treats both alike, while people's lists, the evaluator and the grants feed
  never see a phone. A signed-out phone can't read a private feed (Sync is public and unsigned), so
  **the phone will keep its own follows** locally, from the Follow reply and an `[AppInstall]` call
  that lists them, as life-groups.md plans; neither exists until Ministries builds Follow. The holder
  model is built and tested in `AccessEvaluatorTests` (a phone follows, a phone can't be given Member,
  the follow moves on sign-in).

**Following** (Ministries builds the button and its calls; the parts below are built):

- Ministries will declare the system role **Follower** (assignable to a phone or a person, at a
  ministry) with the one ability `ministries.notices.receive`. Member, Leader and Staff include it.
- Follow and Unfollow will be App writes (`[AppInstall]`): `GiveAsFeatureAsync` to the install, or to
  the person when signed in. Rate-limited per install.
- **Signing in** moves every assignment held by that phone to the person
  (`IPhoneRoles.MoveToPersonAsync`), merging duplicates, so the follows go with them
  (decision 4). Signing out leaves the phone following nothing; the person keeps the follows.
- Until Ministries exists there's nothing to follow; the model and tests are built now.

## Phones: App Attest and installs

Built here because decision 3 puts following and tokens behind it, and prayer requests need it next.

- **Accounts owns installs** (`accounts.installs`): key id, public key, counter, app id, the App
  Attest environment, first and last seen, blocked. It's where identity lives already.
- **Attesting, once per install** (both `[Public]`, rate-limited):
  `Installs.BeginAttestation` returns a one-use challenge; the native layer calls
  `DCAppAttestService.generateKey` and `attestKey(clientDataHash: SHA256(challenge))`, then
  `Installs.Attest(keyId, attestation)`. The API verifies the CBOR attestation: Apple's App
  Attestation root (a public certificate, in the repo), the chain, the nonce, the app id
  (`YNT9A4LGVA.au.com.baptist.app[.dev|.uat]`, from configuration), the counter at zero, and the
  `aaguid` matching the environment (development builds use `appattestdevelop`). All BCL: CBOR
  (`System.Formats.Cbor`), X.509 and ECDSA.
- **Each App write** carries `gsbc-install: <key id>` and `gsbc-assertion`: an assertion over
  `SHA256(method path ‖ "\n" ‖ the gRPC-Web request body)`. The tier interceptor (`[AppInstall]`,
  checks the signature and that the counter grew, then passes the install
  id to the method. The native Transport attaches both to every call to an `[AppInstall]` method;
  the outbox signs when it sends, not when it queues.
- **Signing rule (2026-11-04):** the counter must grow strictly, so two signed calls that overlap can
  reach the server out of order and the earlier one is refused (UAT saw this on `RegisterDevice`). So
  the phone:
  - **signs only `[AppInstall]` methods**, and learns which they are from the server:
    `Installs.ListProofMethods` (`[Public]`) returns every served `[AppInstall]` path, worked out from
    the tier attributes (`InstallProofMethods`). Nothing is listed by hand, so a new `[AppInstall]`
    method is signed without extra steps (`TierRules` guards it). The Transport asks once per launch,
    when the phone is attested and first makes a call;
  - **sends signed calls one at a time**: the assertion is made and the reply received before the next
    signed call starts, so counters arrive in order. Unsigned calls still go in parallel;
  - **copes with a backend updated while the app runs**: a refusal for a missing proof carries the
    trailer `gsbc-install-proof: required`; the Transport then fetches the list again and, if the
    method is now on it, retries once signed. A server too old to have `ListProofMethods`
    (`Unimplemented`) makes the phone sign every call, as before;
  - **registers for push one at a time**: `PushRegistration` runs one registration and, if asked
    again meanwhile, one more after it.

  The server's strict counter check stays (`InstallVerifierTests`: a replay, or an earlier assertion
  after a later one, is refused); `InstallProofOrderTests` (Swift) prove the phone's side.
- **Where App Attest can't run** (the iOS Simulator, Android until Play Integrity): such a phone
  reads everything, and Follow says "This phone can't follow groups" (SPEC §6: no fallback). There's
  no development bypass; App Attest is tested on a real iPhone, and the verifier by
  `AppAttestTests` with a stand-in root.
- **Built (2026-11-03):** `AppAttest` and `InstallsService` in Accounts.Api (table
  `accounts.installs`, migration `AppInstalls`), `InstallProofMiddleware`, `IInstallVerifier` and
  `CallingInstall` in Platform.Api, and on the phone `InstallAttestation` (the native layer attests
  once, on the first App write, and signs the `[AppInstall]` calls, one at a time; see *Signing rule*).
  The app id is `Apple:TeamId` + `Apple:BundleId`, as for the gateway's association file.

## Channels

### The inbox

Two feeds, because a signed-out phone has no person to hold a private feed for:

| Feed | Audience | Holds | Read state |
|---|---|---|---|
| `notifications.inbox` | own records | one row per person per notification: everyone's, their follows', and personal ones | on the row; synced to every device |
| `notifications.public` | public | notifications for *everyone* and for *followers of a ministry*; each says which ministry | on the phone only |

A signed-in phone shows `notifications.inbox`. A signed-out phone shows the `notifications.public`
rows for everyone plus the ministries it follows (the follows it keeps). Anything sent to phones is,
by construction, public content: Follower only covers public notices. Neither feed is pruned yet (see
*Status*).

- **Marking read** is an outbox write (`Notifications.MarkRead` with the ids, or `All`), so it works
  offline and reaches the person's other devices through the feed.
- **Opening a row** opens its link and marks it read. A row whose thing is gone (a deleted event)
  opens the page's own not-found state.
- **Shape (owner, 2026-11-03, "Waiting and Everything"):** a segmented switch under the masthead,
  *Waiting* (with an orange count) and *Everything*. It opens on *Waiting* while anything waits and
  disappears when nothing does. The masthead's note says "2 waiting for you · 3 new". Waiting rows
  carry their action ("Preview it", or "Do this in the admin panel"); unread rows have the orange dot
  and a 700 title (`ListRow Unread`). *Mark all as read* sits under *Everything*. The top bar's
  sliders button opens Notification settings. Direction contract:
  `.impeccable/surfaces/ures-notifications-client-inboxview-razor-001fb91d.md`.
- **Collapsed rows** ("3 new notices in Music") open the area, not one item.

### Push (APNs now, FCM later)

- **Devices** (`notifications.devices`): install id, the signed-in person if any, platform, app id,
  APNs environment (`sandbox` for the Dev build, `production` for UAT and the store app), token, last
  registered, failures. `Notifications.RegisterDevice` is `[AppInstall]`; a call carrying a session
  token also records the person, and signing out clears it.
- **Sending** is a queue like email's (`notifications.deliveries`), drained by `PushDelivery`:
  - APNs over HTTP/2 with a token from the `.p8` key (ES256 JWT, renewed every 50 minutes), to
    `api.sandbox.push.apple.com` or `api.push.apple.com` by the device's environment, with
    `apns-topic` the device's app id.
  - Retries with backoff on 429, 5xx and timeouts (1, 2, 4 … minutes; give up after an hour, since
    a late push is worse than none; it's in the inbox anyway).
  - **Dead tokens are dropped:** `410 Unregistered`, `400 BadDeviceToken` or `DeviceTokenNotForTopic`
    delete the device row.
  - **No key, no push.** Without `Notifications:Apns:*` the API logs once at start-up "Push is off:
    no APNs key configured", deliveries are marked skipped, and the inbox works as normal.
- **The payload** carries the title, body, `link`, the notification id, `thread-id` (the kind and
  ministry, so Notification Centre groups them), and `apns-collapse-id` (the subject, so a second
  push about the same thing replaces the first). Urgent ones are `interruption-level:
  time-sensitive`.
- **The badge** is the person's unread count, sent with every visible push and set by the phone when
  the inbox changes. A signed-out phone's badge is set by the phone alone (the server can't count
  its reads).
- **Pacing and collapsing** (decision 9), per kind:
  - `EveryOne`: every notification pushes. Chat messages are this: each message is its own push,
    grouped by chat (`thread-id`), and tapping one opens that chat. A person can mute a chat or a
    ministry's chats (a preference at that scope), as in any messaging app.
  - `Limited(n, per)`: notices and announcements. Past *n* in that time for one recipient and
    ministry, the rest wait 10 minutes and go as one summary push ("3 new notices in Music") with the
    area's link; every item is still in the inbox. Life Group urgent notices also keep their
    per-group daily limit on posting (life-groups.md).
  - A kind that doesn't say is `EveryOne`; the limit is opt-in, not a default.
- **Quiet hours** (decision 7): a non-urgent push due between 21:00 and 07:00 church time
  (`ChurchTime`) waits until 07:00, collapsed with anything else waiting for that person. The rule is
  `QuietHours` in Notifications.Contracts, so the server and the Announcements desk use the same one.

### Opening what was tapped (decision 8)

- The native layer is the push delegate (`UNUserNotificationCenterDelegate`). A tap records the
  payload's `link` and id, and the plugin raises `linkOpened` to Blazor (`NativeBridge.OnLinkOpened`); on a cold start the
  link waits until Blazor asks for it. Blazor navigates to the route (`NavigationManager`), as an
  in-app link does, and the inbox marks it read.
- **Capacitor must not take the delegate.** By default Capacitor's bridge makes its own
  `NotificationRouter` the notification center's delegate when it starts, which is after the app
  delegate set ours, so taps (and foreground banners) went to a router with no handler and every tap
  opened the app where it was. `ios.handleApplicationNotifications: false` in `capacitor.config.json`
  keeps `DataLayerPush` as the delegate (fixed 2026-11-03, from UAT).
- A push received while the app is open shows as the system banner (the delegate presents it), and the
  bell's count updates through sync.
- In the admin panel there's no push; its pages already update live.

### Silent push for sync

- When a feed changes, `SyncNudger` (Notifications.Api) collects the change for a short moment and
  sends a background push (`content-available: 1`, `apns-push-type: background`, priority 5) to
  devices that may read that feed and aren't holding a `Watch` stream right now: every device for a
  public feed, the person's devices for a private one.
- At most one per device per 15 minutes; iOS budgets them anyway (SPEC §5), and background refresh
  covers the rest.
- The native layer handles `didReceiveRemoteNotification`, runs one Sync pull, and calls the
  completion handler within iOS's 30 seconds. No WebView, no .NET: it's the existing `SyncEngine`.
- Not a kind and not in the inbox; people don't control it.

## People's control

- **More → Notification settings** on the phone (also from the inbox's top bar): each area's kinds with a switch (inbox and push), per ministry where
  the kind is per ministry ("Music: notices" beside Follow), mandatory ones shown on and locked with
  "Always on: it's about your account's safety". If iOS's permission has been refused the screen
  says so and opens Settings.
- **Asked on the first open** (owner, 2026-11-03: "notifications [are] a core functionality of the
  app"). See *The first-open sheet* below. Signing in also registers a phone that already allowed them.
- **Defaults** are each kind's `defaultPush`; a row exists only once someone chooses
  (as Configuration does with placements).
- **Stored per person** (`notifications.preferences`, synced through an own-records feed), or per
  phone when signed out (sent with the device registration). Signing in keeps the person's; the
  phone's are dropped, like its follows moving over.
- **A switch turned off stops that kind reaching you at all** (decision 11): no inbox row and no push. Mandatory
  kinds always arrive, and so does a staff member's own test of an announcement (they asked for it). Unread counts,
  the badge and "N new" count only the rows you got, so they need nothing extra.
- **Turning a kind off applies from then on** (chosen 2026-11-08, the simplest rule): rows already in a signed-in
  inbox stay, as read or unread as they were, and turning it back on doesn't bring back what was raised while it was
  off (it was never written). A signed-out phone's inbox is worked out on the phone from the public rows it follows,
  so there a kind turned off also hides the rows already shown, and turning it on shows them again, as unfollowing a
  ministry already does.

## The first-open sheet

`NotificationsWelcomeMoment` (Notifications.App) is a covered sheet over Home: a bell tile beside
"Hear from the church", one line about what the church sends, **Turn on notifications** (which raises
iOS's prompt) and **Not now**. Once iOS's answer is Allow, the same sheet becomes "Choose what buzzes your
phone" with the settings' own list (`NotificationSettings`, reused) and **Done**. Decided with the owner,
2026-11-03:

- **When:** once, while iOS's permission is still undecided (`notDetermined`, `ask` to the bridge). That
  covers a fresh install and a phone that updated without ever deciding. **Not now** is remembered on
  the phone (`notifications.welcome.answered` in local preferences), and once iOS's prompt has been
  answered, here or anywhere else, the permission is decided; either way it never shows again. (Closing
  the app while iOS's prompt is up leaves both undecided, so it asks on the next open.) **Not now** doesn't use up iOS's
  one-time prompt: More → Notification settings still offers it.
- **Declining in iOS's prompt closes the sheet** quietly; the settings screen explains how to turn them
  on later.
- **Before What's new:** at most one sheet per open. Features offer an opening sheet through
  Platform.Client's `IOpeningSheet`; the app's layout asks `OpeningSheets` after the first render and
  the first that shows wins, in the order the host registers them (Notifications before Release notes in
  the App host's `Program.cs`). What's new doesn't mark anything seen when it isn't asked, so on an
  update its highlights show on the next open. On a fresh install What's new shows nothing anyway.
- **Never in a browser build** (no native layer: the permission is `unavailable`). Everyone sees it,
  signed in or not.
- **Counted, anonymously:** the measure `notifications.push` counts each phone once a week, the first
  time it's closed that week, as *On*, *Turned off* or *Not chosen yet* (by then the sheet has been
  answered). Insights shows it under *How phones are set up*; the rules are Analytics'
  ([analytics.md](../features/analytics.md), *Settings measured once a week*).

## Staff announcements (decision 5)

- **An announcement** (`notifications.announcements`): title, body (plain text; push can't show rich
  text), an optional link (a place in the app, chosen from Places in the app's list with
  `PlaceField` ([destinations.md](destinations.md)), or the web), who it's for, urgent or not, and its state:
  draft, waiting to send, scheduled, sent, cancelled, recalled.
- **Who it's for,** in staff words, built on `NotificationAudience`: *Everyone with the app*,
  *Everyone signed in*, *Followers and members of a ministry* (when Ministries exists), *People who
  can …* (an ability, from the catalogue), *One person*. The composer shows "About 410 phones" from
  the same resolver before sending.
- **Permissions:** `notifications.announcements.write` (write, test, ask to send) and
  `notifications.announcements.send` (send now, schedule, cancel a scheduled one, recall a sent one). *Announcements
  Writer* holds the first and *Announcements Sender* both (2026-11-09; until then one *Announcer* role).
- **Send to my phone** sends it to you alone, marked *Test*, so you see the lock screen and the
  inbox before anyone else does.
- **Scheduling** in church time; a scheduled announcement can be changed or cancelled until it goes.
- **Recall, not delete** (owner, 2026-11-09: "they should be able to 'recall' an announcement. but it will always be
  there as basically auditable history"). A sent announcement has *Recall…*, for holders of `.send`, the same permission
  as sending. A confirm sheet says what happens. Then:
  - **It leaves every inbox and the app.** Its inbox rows are deleted and the inbox and public feeds get a tombstone
    for each, so unread counts, the badge and "N new" stop counting it, signed in or not. Phones online now drop it at
    once; the others drop it on their next sync, and the recall queues a sync push (within the usual 15-minute limit)
    so they sync soon.
  - **Nothing more goes out.** Pushes not sent yet (held for quiet hours, paced into a summary, waiting to retry) are
    given up as *Recalled before it went*, and the dispatcher skips any delivery of a recalled notification it had
    already leased.
  - **A push already shown can't be taken back** (the confirm sheet says so). Tapping it opens what it always opened:
    the place it linked to, or the notifications list, where it no longer is. Nothing on the phone tells "recalled"
    apart from "not synced yet", so no recalled message is shown (decided while building, 2026-11-09).
  - **The admin history keeps it**, marked *Recalled* on the desk, on the rail and in *Sent earlier*: "Recalled by
    Ruth 9:40am today. It was sent … to about 240 phones, and 31 people had read it." The read count is kept as it
    was at the recall, the name is looked up when the list loads (`IPeopleIndex.NamesAsync`), and deleting the
    recaller's account forgets who it was. It can't be changed, sent again or deleted; *Copy into a new announcement*
    starts a draft from it. Only a sent one can be recalled: one not sent yet is changed, cancelled or deleted.
  - Tested in `AnnouncementRecallTests`: it leaves every inbox and the public feed (tombstones a phone syncs), the
    badge no longer counts it, a push held for quiet hours never goes, the history keeps it with who recalled it, and
    it can't be sent or changed as it is.
- **The admin panel's Announcements page** (owner, 2026-11-03: "The fortnight ahead"; mostly the
  office, a few a month; writing it well is what's easiest). It opens on this week and next as the
  office week's rail (`DayRail`, shared with Home), sent ones in primary and scheduled ones as marker
  dots on their day; unsent ones (being written, waiting to send) are chips under it. Below is the
  desk: title and message, then a checklist (who it's for with its reach, when: Now or Later with a
  day and a time in church words, what it opens, urgent), beside a lock-screen proof
  (`LockScreenProof`). It saves as you type (`SaveStatus`, as Drafts does). *Send* opens a confirm
  sheet with the proof, the reach and the time, and warns at 0 phones; afterwards it says
  "Sent to about 240 phones at 9:14am". When quiet hours will hold a non-urgent one, the When note, the
  confirm sheet (which then carries the Urgent switch) and the sent or scheduled line say when phones
  get it: "Phones buzz at 7am (quiet hours). Turn on Urgent to buzz them now." (2026-11-04). A sent one shows how many people have read it (signed-in
  readers; phones not signed in aren't counted). Older sent ones are in a *Sent earlier* table.
  Scheduled ones show on Home's week, waiting-to-send under *Needs you* and as the sidebar count
  (`AnnouncementsOfficeWeek`). The list follows the `notifications.announcements` marker feed (for
  holders of `.write`). Direction contract: `.impeccable/surfaces/bc-churchapp-features-notifications-admin-de737a5a.md`.
- It's an admin-panel screen only (AGENTS.md, *Admin work stays in the admin panel*); the app shows
  staff only what's for everyone: the inbox, the bell and their preferences.

## How it's built

- **Platform.Contracts:** `NotificationKind`, the push payload's keys.
- **Platform.Api:** `INotifications`, `Notification`, `NotificationAudience`, `IPermissionHolders`,
  `AddNotificationKinds`, and the installs check behind `[AppInstall]` (`IInstallVerifier`).
- **Platform.Client:** the native bridge (`NativeBridge`): the tapped link (`linkOpened`), the token and
  the permission prompt.
- **Features/Notifications:** Contracts (services, messages, permissions), Api (the service,
  delivery, APNs, the nudger, schema `notifications`), Client (the inbox component, the bell's
  count, preferences, the announcement composer), App (More → Notifications, the inbox page), Admin
  (Announcements). No reference to any other feature.
- **Access:** phone holders, `IPermissionHolders`, moving a phone's assignments to a person.
- **Accounts:** installs and App Attest; the security kinds.
- **Configuration:** the two draft kinds, raised on *Ask to publish* and *Share*.
- **Native layer (iOS):** App Attest keys and assertions in Transport; push registration, the
  delegate, silent-push sync and tapped links in the plugin; the Push Notifications and App Attest
  entitlements per configuration (`aps-environment` development for Debug, production for UAT and
  Release).
- **Android:** stubbed, as the rest of the native layer is; FCM and Play Integrity when Android is
  built.
- **No new API paths:** every inbound call is a gRPC method. APNs is an outbound call. No webhooks.

## What the owner sets up

Done for UAT: the APNs key, the Push Notifications and App Attest identifiers, and the `apns-secret`.
Production doesn't exist yet; steps 1 and 4 repeat for it. For dev, step 2 puts the key in user secrets.

1. **Apple developer portal** (team `YNT9A4LGVA`), Certificates, Identifiers & Profiles:
   - **Keys → +**: name it `GSBC Church App push`, tick **Apple Push Notifications service (APNs)**,
     environment **Sandbox & Production**, Continue, Register, and **Download** the `.p8` (only
     once). Note the **Key ID**.
   - **Identifiers**: for each of `au.com.baptist.app.dev`, `au.com.baptist.app.uat` and
     `au.com.baptist.app`, tick **Push Notifications** and **App Attest**, and Save. Xcode's
     automatic signing then refreshes the profiles.
2. **Development:** in user secrets for the AppHost (`--id gsbc-churchapp-apphost`), which passes them
   to the API:
   ```bash
   dotnet user-secrets --id gsbc-churchapp-apphost set "Notifications:Apns:KeyId" "<key id>"
   dotnet user-secrets --id gsbc-churchapp-apphost set "Notifications:Apns:TeamId" "YNT9A4LGVA"
   dotnet user-secrets --id gsbc-churchapp-apphost set "Notifications:Apns:Key" "$(cat AuthKey_<key id>.p8)"
   ```
   then restart the AppHost.
3. **UAT:** the GitHub `uat` environment and the cluster secret `apns-secret` in `church-app-uat`,
   with `Notifications__Apns__KeyId`, `__TeamId` and `__Key` (the `.p8`'s contents), given to the API
   as `envFrom` with `optional: true`. The UAT API also needs `Apple__TeamId=YNT9A4LGVA` and
   `Apple__BundleId=au.com.baptist.app.uat` (not secrets: plain environment values in `gsbc.argo`),
   so App Attest accepts the UAT app; without them every App write is refused. Development gets the
   team id from the AppHost and the bundle id from the API's `appsettings.Development.json`.
4. **Production:** the same secret in production's namespace, when the API goes there, with
   `Apple__TeamId` and `Apple__BundleId=au.com.baptist.app`.
5. **Then, on the owner's iPhone:** `tools/ios/deploy.sh` (Xcode's automatic signing picks up the
   new capabilities), completely close the app and open it, More → Notifications → *Turn on
   notifications*, and from the admin panel send an announcement to yourself with *Send to my
   phone*.

The key never goes in the repo or in chat. One key serves every app and both APNs environments.

## Build order (as built)

1. Installs and App Attest (`[AppInstall]`), tested against Apple's sample attestation.
2. Access: phone holders, `IPermissionHolders`, the move on sign-in.
3. Platform contracts and the Notifications feature: kinds, raise, inbox feeds, mark read.
4. Preferences.
5. Push: device registration, APNs sender, collapsing, quiet hours, dead tokens; the native side.
6. Opening a tapped notification; the bell and the inbox on the phone.
7. Silent push for sync.
8. Announcements in the admin panel.
9. The drafts source, end to end on the owner's iPhone.

## Status (2026-11-03)

**Built and tested against Postgres. Running on phones through UAT** (the APNs key and the App Store
Connect record are done for UAT; the install-proof fix of 2026-11-04 came from UAT's refused
`RegisterDevice` calls):

- **App Attest** (`[AppInstall]`): see *Phones* above. `AppAttestTests`, `InstallVerifierTests`,
  `InstallProofOrderTests`.
- **Access:** phone holders, `IPermissionHolders`, `IPhoneRoles.MoveToPersonAsync`, called by
  `Installs.Claim` after signing in. `AccessEvaluatorTests`.
- **Platform:** `NotificationKind` (Contracts), `INotifications`, `NotificationAudience`,
  `AddNotificationKinds`, `IPeopleIndex` (Api; Accounts implements it), `IInboxCount` (Client) and
  the bell (`InboxBell`, shown by `Page` on every tab root, with DESIGN.md's orange badge).
- **Notifications feature:** schema `notifications` (migration `InitialNotifications`): the raiser,
  the four feeds, preferences, pacing, quiet hours, deliveries leased with `SKIP LOCKED` so two API
  replicas never send twice, `ApnsTransport` (ES256 token, HTTP/2, sandbox or production per
  device), dead tokens dropped, `SyncNudger`, announcements and their scheduler, the *Announcer*
  role. `NotificationsTests`.
- **Drafts:** *Ask to publish* tells holders of the publish permission of the draft's first area
  (alphabetically: everyone who can publish it holds that one); *Share* tells the person. Both link
  `/previews?draft={id}` (the phone has no Drafts screen: admin work stays in the admin panel), say
  *Preview it*, wait for you, and settle when the draft is published or deleted.
  `DraftEditorTests`.
- **Phone:** `DataLayerPush` (the notification delegate: a tap hands its `link` to Blazor through
  the existing `linkOpened` path, waiting for Blazor on a cold start; a silent push runs one Sync
  pull within iOS's budget), `registerPush`, `pushPermission`, `setBadge`, `proveInstall`; the
  Push Notifications (`aps-environment`) and App Attest entitlements per configuration
  (`development` for Dev, `production` for UAT and Release, from the xcconfigs) and the
  `remote-notification` background mode. The inbox (the bell, `/notifications`), More →
  Notifications, and push registration (asked from More → Notifications, and on signing in).
- **Admin panel:** Announcements (`/announcements`), for holders of
  `notifications.announcements.write`.

**Decided while building (no owner question needed):**

- A push that arrives while the app is open shows as the system banner; the bell's count updates
  through sync.
- *Who it's for* in the composer offers *Everyone with the app* and *Everyone signed in* for now;
  *People who can …*, *A ministry* and *One person* are in the service and come to the screen with
  Ministries and a people picker.
- A signed-out phone's inbox shows church-wide notifications until Ministries gives phones
  something to follow; the follows it keeps then add their ministries' rows.
- Silent pushes go to every device at most once per 15 minutes on any feed change outside
  Notifications' own; nothing tracks which devices hold a `Watch` stream yet, so the "aren't holding
  a stream" refinement under *Silent push for sync* isn't built.
- Inbox feed rows aren't pruned yet (`FeedRetention` has no window for them); to do with the first
  busy source.

## Open

- Analytics' "seen on / opened on" per push: still to do.
- FCM and Play Integrity: when Android is built.
- Whether a ministry's leaders may send an announcement to their own followers (likely: the same
  composer at a ministry scope, `notifications.announcements.send` at that ministry).
- Security kinds ("a new passkey was added") and their email: with the next Accounts change.
- *Waiting to send* on the admin home's *Needs you* (`IOfficeWeekSource`).
- The impeccable pass on the inbox and the Announcements page: both are built from Platform.Ui's
  existing parts and DESIGN.md, without a decision page yet.
