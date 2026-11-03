# Notifications and push

How any feature tells people something: an inbox in the app, a push on the lock screen, and the silent
push that makes the phone sync. The owner (2026-11-03): "We should make a proper notifications system
for this. There will be a lot of places they could come from." Backbone, like drafts: designed and
decided before anything raises one.

**Status: approved by the owner, 2026-11-03,** with one change (decision 9). Being built.

## Decisions (owner, 2026-11-03)

1. **Phones follow without signing in.** A signed-out phone can follow a Life Group or a ministry,
   and then gets its notices, as life-groups.md first planned. Because the audience always comes
   from Access, **Access gains a second kind of holder: a phone** (an install), beside a person.
2. **Following gives a phone notices, and for a Life Group it is joining.** A *Follower* role at a
   ministry. It opens nothing members-only; for a Life Group it is life-groups.md's per-phone Join,
   counted in the leader's "14 joined".
3. **A phone proves it is that phone with App Attest,** so this station builds the `[AppInstall]`
   tier (SPEC §6) first. Registering a push token and following are App writes.
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
        "config.drafts.waiting", "A draft is waiting for you to publish", area: "Drafts",
        defaultPush: true);

    public static readonly NotificationKind SharedWithYou = new(
        "config.drafts.shared", "A draft is shared with you", area: "Drafts", defaultPush: true);
}
```

A kind has:

| Part | Means |
|---|---|
| Key, description, area | Shown in preferences, grouped by area, in the same voice as abilities ("Can …" becomes "Tell me when …") |
| `defaultPush` | Whether it pushes for someone who hasn't chosen. The inbox always gets it |
| `Mandatory` | Can't be turned off (security). Implies push, and email |
| `ScopeKind` | `null`, or `Ministry`: the switch is then per ministry ("Music: notices") |
| `MayBeUrgent` | Whether a raise may be urgent (skips quiet hours, time-sensitive on iOS) |
| `EmailAllowed` | Off for every kind except security, for now (decision 6) |
| `Pacing` | `EveryOne` (each pushes: chat messages, security), or `Limited(n, per)`: past *n* pushes per recipient and ministry in that time, they collapse (below). Declared per kind (decision 9) |

`Platform.Contracts.Notifications` holds `NotificationKind` (AOT; the phone reads it).
`Platform.Api.Notifications.INotifications` is the one call a feature makes:

```csharp
await notifications.RaiseAsync(new Notification(
    DraftNotifications.WaitingToPublish,
    NotificationAudience.Holding(ConfigPermissions.HubPublish),
    Title: $"“{draft.Name}” is waiting to publish",
    Body: $"{author} asked for it to be published.",
    Link: $"/drafts/{draft.Id}",
    Subject: $"draft/{draft.Id}"), cancellationToken);
```

- **`Link`** is an in-app route, checked by `LinkTargets` (`InApp` only). **`Subject`** names the
  thing (`draft/42`, `event/9`, `ministry/music/notice/17`). A notification with a subject must have a
  link (decision 8); `RaiseAsync` throws otherwise, and a test raises every declared kind in its
  tests to check it.
- **Raised after commit,** like email: `RaiseAsync` writes a row and returns; delivery is in the
  background. A failure to deliver never fails the feature's call.
- On start-up the server reconciles every registered kind into the public **`notifications.kinds`**
  feed, as Access does with the permission catalogue, so the phone's preferences list them without
  knowing the features.

Known sources, and how each will raise:

| Source | Kind | Audience |
|---|---|---|
| Drafts (this station) | waiting to publish; shared with you | holders of the area's publish permission; the person |
| Announcements (this station) | church-wide announcements | as staff pick (below) |
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
`Holding(MinistryPermissions.ReceiveNotices, "music")`; "people who can publish the Hub" is
`Holding(ConfigPermissions.HubPublish)`.

**Access is extended in two ways:**

- **`IPermissionHolders`** (Platform.Api, implemented by Access): `HoldersAsync(node, scopeId)`
  returns every person and phone whose compiled grants allow the node at that scope, including
  wildcards, denials, roles that apply to a ministry's teams, and end dates. It reads the same
  compiled grants the evaluator caches, so "who gets this" and "who may see this" can't disagree.
- **Phones as holders.** `access.assignments` gets a holder kind: a person (`user_id`) or a phone
  (`install_id`), exactly one. Only roles marked *may be given to a phone* can be (Follower, today),
  so a phone can never hold anything else, whatever an assignment says. A phone's grants come
  through the same compiler and a phone's own grants feed row, so `IAccess.Can` works on a signed-out
  phone for "am I following Music?".

**Following** (Ministries builds the button; this station builds the parts):

- Ministries declares the system role **Follower** (assignable to a phone or a person, at a
  ministry) with the one ability `ministries.notices.receive`. Member, Leader and Staff include it.
- Follow and Unfollow are App writes (`[AppInstall]`): `GiveAsFeatureAsync` to the install, or to
  the person when signed in. Rate-limited per install.
- **Signing in** moves every assignment held by that phone to the person
  (`IRoleAssignments.MoveInstallToPersonAsync`), merging duplicates, so the follows go with them
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
  today "not available yet") checks the signature and that the counter grew, then passes the install
  id to the method. The native Transport attaches both to every call to an `[AppInstall]` method;
  the outbox signs when it sends, not when it queues.
- **Where App Attest can't run** (the iOS Simulator, Android until Play Integrity): such a phone
  reads everything, and Follow says "This phone can't follow groups" (SPEC §6: no fallback). There's
  no development bypass; App Attest is tested on a real iPhone, and the verifier by
  `AppAttestTests` with a stand-in root.
- **Built (2026-11-03):** `AppAttest` and `InstallsService` in Accounts.Api (table
  `accounts.installs`, migration `AppInstalls`), `InstallProofMiddleware`, `IInstallVerifier` and
  `CallingInstall` in Platform.Api, and on the phone `InstallAttestation` (the native layer attests
  once, on the first App write, and signs every later unary call except Sync's and Installs').
  The app id is `Apple:TeamId` + `Apple:BundleId`, as for the gateway's association file.

## Channels

### The inbox

Two feeds, because a signed-out phone has no person to hold a private feed for:

| Feed | Audience | Holds | Read state |
|---|---|---|---|
| `notifications.inbox` | own records | one row per person per notification: everyone's, their follows', and personal ones | on the row; synced to every device |
| `notifications.public` | public | notifications for *everyone* and for *followers of a ministry*; each says which ministry | on the phone only |

A signed-in phone shows `notifications.inbox`. A signed-out phone shows the `notifications.public`
rows for everyone plus the ministries it follows (from its own grants). Anything sent to phones is,
by construction, public content: Follower only covers public notices. Retention: the latest 200 per
person, and 60 days for the public feed.

- **Marking read** is an outbox write (`Notifications.MarkRead(ids)` / `MarkAllRead`), so it works
  offline and reaches the person's other devices through the feed.
- **Opening a row** opens its link and marks it read. A row whose thing is gone (a deleted event)
  opens the page's own not-found state.
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
  (`ChurchTime`) waits until 07:00, collapsed with anything else waiting for that person.

### Opening what was tapped (decision 8)

- The native layer is the push delegate (`UNUserNotificationCenterDelegate`). A tap records the
  payload's `link` and id, and the plugin raises `notificationOpened` to Blazor; on a cold start the
  link waits until Blazor asks for it. Blazor navigates to the route (`NavigationManager`), as an
  in-app link does, and the inbox marks it read.
- A push received while the app is open shows as a banner in the app (Platform.Ui, generic), not the
  system banner, and the bell's badge updates.
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

- **More → Notifications** on the phone: each area's kinds with a push switch, per ministry where
  the kind is per ministry ("Music: notices" beside Follow), mandatory ones shown on and locked with
  "Always on: it's about your account's safety". The iOS permission prompt is asked the first time
  something would push (following a group, signing in), not at launch, and if it's been refused the
  screen says so and opens Settings.
- **Defaults** are each kind's `defaultPush`; a row exists only once someone chooses
  (as Configuration does with placements).
- **Stored per person** (`notifications.preferences`, synced through an own-records feed), or per
  phone when signed out (sent with the device registration). Signing in keeps the person's; the
  phone's are dropped, like its follows moving over.
- The inbox always gets everything addressed to you; preferences only stop the push.

## Staff announcements (decision 5)

- **An announcement** (`notifications.announcements`): title, body (plain text; push can't show rich
  text), an optional link (an in-app page, or the web), who it's for, urgent or not, and its state:
  draft, waiting to send, scheduled, sent, cancelled.
- **Who it's for,** in staff words, built on `NotificationAudience`: *Everyone with the app*,
  *Everyone signed in*, *Followers and members of a ministry* (when Ministries exists), *People who
  can …* (an ability, from the catalogue), *One person*. The composer shows "About 410 phones" from
  the same resolver before sending.
- **Permissions:** `notifications.announcements.write` (write, test, ask to send) and
  `notifications.announcements.send` (send now, schedule, cancel a scheduled one). A system role
  *Announcer* holds both; App editor and Staff can include it.
- **Send to my phone** sends it to you alone, marked *Test*, so you see the lock screen and the
  inbox before anyone else does.
- **Scheduling** in church time; a scheduled announcement can be changed or cancelled until it goes.
- **The admin panel's Announcements page** (desktop, DESIGN.md's admin section, `DataTable` and
  `SplitView` as People and Media are): the list on the left (Waiting to send, Scheduled, Sent with
  "reached 410 phones · 38 opened" when analytics exist), the chosen one on the right with the
  composer and a lock-screen preview. Waiting-to-send shows on the office week's *Needs you*
  (`IOfficeWeekSource`) and as a count on the sidebar link.
- The same component works in the app for people holding the permission, as every management screen
  does.

## How it's built

- **Platform.Contracts:** `NotificationKind`, the push payload's keys.
- **Platform.Api:** `INotifications`, `Notification`, `NotificationAudience`, `IPermissionHolders`,
  `AddNotificationKind`, and the installs check behind `[AppInstall]` (`IInstallVerifier`).
- **Platform.Client:** `INotificationOpens` (the tapped link), and the native bridge calls for the
  token and permission prompt.
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
   as `envFrom` with `optional: true`.
4. **Production:** the same secret in production's namespace, when the API goes there.

The key never goes in the repo or in chat. One key serves every app and both APNs environments.

## Build order (this station)

1. Installs and App Attest (`[AppInstall]`), tested against Apple's sample attestation.
2. Access: phone holders, `IPermissionHolders`, the move on sign-in.
3. Platform contracts and the Notifications feature: kinds, raise, inbox feeds, mark read.
4. Preferences.
5. Push: device registration, APNs sender, collapsing, quiet hours, dead tokens; the native side.
6. Opening a tapped notification; the bell and the inbox on the phone.
7. Silent push for sync.
8. Announcements in the admin panel.
9. The drafts source, end to end on the owner's iPhone.

## Open

- Analytics' "seen on / opened on" per push: with the analytics station.
- FCM and Play Integrity: when Android is built.
- Whether a ministry's leaders may send an announcement to their own followers (likely: the same
  composer at a ministry scope, `notifications.announcements.send` at that ministry).
