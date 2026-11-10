# Prayer

Status: built 2026-11-11, overnight while the owner slept, from SPEC §3 ("submit privately to the pastoral team, with
an optional public prayer wall"). Every choice below marked *Claude's default* was made then and is the owner's to
change; the sketches and the decision page are in `.impeccable/mocks/prayer/` (`decision.html`). The one decision that
is the owner's is marked so.

## Who can do what

| Who | What |
|---|---|
| Anyone with the app, signed in or not | Ask for prayer, privately or also for the prayer wall; see the wall and tap *Pray*; see their own requests, say one was answered, take it off the wall, delete it |
| Staff with *Pastoral team* (`prayer.*`) | Read every request; keep notes only the pastoral team sees; mark followed up, answered or new again; delete a request; everything *Prayer wall* does |
| Staff with *Prayer wall* (`prayer.wall.moderate`) | Read only the requests that ask for the wall, choose what the wall shows (words, name, until when), keep one private or take it down |

- **Permissions** (Access, global): `prayer.requests.read` (*sensitive*, so its holders can't recover their account
  by email, identity.md) and `prayer.wall.moderate`. Both roles are listed under *Prayer* in Access. Nobody else,
  staff or Owner role aside, can read a request (the Owner role's `*` includes them, as it includes everything).
- **The admin panel's Prayer page** (People → Prayer) shows to anyone holding `prayer.wall.moderate`; the pastoral
  team's parts (notes, follow-up, getting in touch, requests that didn't ask for the wall) only with
  `prayer.requests.read`, checked on the server for every call.

## Asking for prayer (the app)

- **`/prayer/ask`**, from the Prayer page's *Ask for prayer* (and the Hub tile through `/prayer`). The request, then
  *Who reads it* as two choices that say what each means: *The pastoral team* (the default; "Nobody else sees it") or
  *The pastoral team and the prayer wall*. With the wall: *No name* or *My first name* (*Claude's default: no name,
  for everyone*; the phone can't tell a child from an adult, and anonymous is the safer start). Not signed in: an
  optional name. Anyone: *I'd like someone to get in touch*, with an optional phone or email.
- **Through the outbox** (`IPrayerService.Send`, `[AppInstall]`): the phone gives the request its id, so a retried
  send is stored once. With no signal it waits on the phone, the page says so, and it goes by itself.
- **Not signed in** (SPEC §6): the install proves it's the app (App Attest); the request is tied to that install
  only. A phone that can't prove it sees *Couldn't send* on the request with the reason, and can try again.
- **Limits** (*Claude's default*): 5 requests per phone a day (a sixth waits in the outbox and goes when the day has
  passed) and 300 *Pray* taps per phone a day (past that they're ignored). Counted in the database per install, since
  the install is only known inside the tier check.
- **Children** (**owner, 2026-11-11**: "a child's request may be public, but must be approved"): a young person's
  request can ask for the wall like anyone's, and like anyone's it shows only once staff approve it. Staff see
  *Under 18* (from People's age, `IFamilies`) on the request with a reminder to take extra care. The name default is
  *no name* for everyone (above); staff can always hide a name.

## Your requests

- **Signed in:** a private feed, `prayer.mine` (own records), so they show on every phone you sign in on, and go on
  sign-out.
- **Not signed in:** the phone keeps its own copy of what it sent (`ILocalPreferences`, a year), since a signed-out
  phone can't read a private feed. It knows a request is on the wall by finding its id in the public wall feed (the id
  is random and made on the phone, so it says nothing about who sent it).
- Each shows where it is: waiting to send, couldn't send, with the pastoral team, on the wall until a day with its
  prayer count, was on the wall, answered. Its page offers *It was answered* (with optional words of thanks the wall
  shows), *Take it off the prayer wall* (it can't go back), and *Delete it* (from the pastoral team and the wall;
  can't be undone).

## The prayer wall

- **Nothing is public until staff show it** (SPEC §6). Staff see what the person wrote (it never changes) and set what
  the wall shows: the words (*Claude's default*: staff may edit them, for when a request names someone else or says
  too much), the name (staff can hide one; they can never show one the person didn't allow), and the day it leaves
  (30 days to start, up to a year). A request that didn't ask for the wall can never be shown.
- **The public feed** `prayer.wall` carries only the shown words, the first name if allowed, when it was shown, until
  when, the prayer count and an answered line. No install, person or contact detail.
- **It leaves the wall** when its day passes (`PrayerSchedule`, every 15 minutes, one replica at a time), when the
  person takes it down, when staff take it down or keep it private, or when it's deleted.
- **Pray** (*Claude's default*): one mark per phone per request; tapping again takes it back. The count is all anyone
  sees. The button turns aqua once you've prayed (DESIGN.md's "praying" state).

## The pastoral inbox (admin panel)

- One page, `/prayer`: tabs *New*, *For the wall*, *On the wall*, *Followed up*, *Answered*, *Everything* (a wall-only
  holder sees the three wall-related ones); a search; the list; the request with its actions; and, for the pastoral
  team, *Notes for the pastoral team*, *Getting in touch* (their contact, or a link to them in People) and *What's
  happened*.
- **Live:** every change writes a marker to `prayer.inbox` (audience: `prayer.wall.moderate`, markers carry no words),
  so a new request appears without reloading.
- **What's logged** (*Claude's default*): every change, with who and when (sent, followed up, answered, notes added or
  deleted, put on or taken off the wall, changed). **Reading isn't logged.**

## On the phone for staff

**Owner, 2026-11-11:** "Prayer wall approvals and such should be able to be done on the phone" (an exception to
*Admin work stays in the admin panel*, AGENTS.md). In progress on `feature/prayer-on-phone`. The plan:

- **To approve** in Prayer, for holders of `prayer.wall.moderate`: the requests that ask for the wall, each opening to
  the same moderation as the admin desk (the words, starting from theirs; the name, none by default; *Under 18*; how
  long), *Put it on the wall* or *Not for the wall*. Also: take a post down, extend it.
- **Pastoral team extras** (*Claude's default*, only what's useful on a phone): mark followed up or answered, add a
  note, read the request. The full inbox with filters and history stays in the admin panel. *Mark answered* needs
  `prayer.requests.read`, as on the desk.
- **Data:** two feeds with a permission audience (`FeedAudience.Holding`), so they're private, cleared on sign-out and
  reset when the role changes: one for wall moderators carrying only wall-asking requests, without contact details or
  notes; one for the pastoral team carrying everything they may read. Every action goes through the outbox, applied
  once by its `gsbc-request-id` (as Events does with `HandledRequests`). No new direct calls; nothing in analytics.
- **Notifications:** `prayer.wall.waiting` stops being admin-panel-only and opens *To approve*; still without the words.
- **One moderation component** in Prayer.Client, used by the admin desk and the phone.

## Notifications

| Kind | Who | When |
|---|---|---|
| `prayer.new` *Someone sends a prayer request* | holders of `prayer.requests.read` | a request arrives; waits until it's marked followed up or answered |
| `prayer.wall.waiting` *A prayer request asks to be on the prayer wall* | holders of `prayer.wall.moderate` | a request asks for the wall; one notification per person with the one above (most specific first); waits until the wall is decided |
| `prayer.prayed` *Someone prays for your prayer request* | the person | at most once a day per request (*Claude's default*) |
| `prayer.answered` *The pastoral team marks your prayer request answered* | the person | staff mark it answered |

Neither staff kind puts the words in the notification; it says to read it in the admin panel. The person's kinds
reach signed-in people only (a signed-out phone sees the count in the app instead); both can be turned off.

## Keeping and deleting

- **Kept a year** (*Claude's default*, SPEC §11's retention period): a request is deleted, with its notes and
  prayers, a year after its last change.
- **Create means delete:** the person deletes theirs from the app; the pastoral team deletes any; notes can be deleted.
- **Deleting an account** (`IPersonData`, *Claude's default*): their requests (with notes and wall posts) and their
  prayers are deleted, and the counts drop. Requests sent without signing in aren't tied to anyone and stay until
  deleted or a year passes.
- **Never in analytics:** nothing about prayer is counted (analytics.md).

## Built

`Prayer.Contracts` (services, messages, `prayer.*` permissions, feeds, the Hub tile, notification kinds),
`Prayer.Api` (schema `prayer`: requests, notes, steps, prayed; `PrayerRequests` for the app, `PastoralCare` for staff,
`PrayerKeeping` for expiry and the year), `Prayer.Client` (`PrayerOnPhone`, the wall row, the Hub tile, the admin's
`PastoralClient`), `Prayer.App` (`/prayer`, `/prayer/ask`, `/prayer/yours/{id}`), `Prayer.Admin` (`/prayer`). Tests in
`Prayer.Api.Tests`: nothing public without approval, names never shown that weren't allowed, private rows only in their
person's feed, wall-only staff see only wall requests, expiry, per-phone limits and a resent request stored once,
taking down and deleting only by the person, deleting an account, the year.

## Open, for the owner

- Every *Claude's default* above, especially: staff editing the wall's words; no name by default for everyone; a year's
  keeping; 30 days on the wall; reading not being logged.
- Whether a staff reply to the person (rather than notes for the team) is wanted; today the team gets in touch
  outside the app.
- Whether the wall belongs on Home's Today as well as its tile.
