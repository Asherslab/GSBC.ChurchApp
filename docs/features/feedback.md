# Feedback

Status: built 2026-11-11. Staff and testers report a problem or an idea from the app or the admin panel, with screenshots and videos;
staff look after it on a board in the admin panel; Claude reads and moves it from the terminal. It works in the
production app, not through TestFlight. Choices marked *Claude's default* are the owner's to change.

## Why (owner, 2026-11-11)

- "later on staff will probably want a way to chuck in the same things i am while they test (e.g. if they notice a
  badly shaped button, etc)"
- "they might find bugs in the app that is already in prod. so i don't know that relying on testflight's feature is
  good."
- "quick and easy select a screenshot(s) or video(s) from photo roll and attach some text about what's wrong. shoots
  off to the server, i can send you to look at and grab them. delete their media and such once the ticket is verified
  resolved so we don't keep random crap constantly"
- "a role that shows it always. otherwise - on verified phones (attest), allow a max 5/10 a day … with maybe a phone
  banning option for abuse, hide the feedback menu under a tripple tap on the church logo in the more page. means that
  if a member hands me their phone with a "what the heck is wrong with it" i can quickly report it from their device
  without fuss, but normal people don't need to see it"
- "make the language more generic in the feedback area. I.e. - an idea is fine too, we'll just determine what it is by
  the language they put in the text box"
- "Feedback should probably record some things like app state and such if it's from a phone - may help with
  reproduction. local DB and such", and on who sees the copy of a phone's data: "the only people that should/would be
  seeing that data would already have permission to grab it anyway … That data only needs to be grabbed for
  debugging, doesn't need to be "displayed" to staff … maybe just add that data as another permission."

## Who can do what

| Who | What |
|---|---|
| The **Feedback** role (`feedback.send`) | Sees *Send feedback* in More on any phone they sign in on; sends up to 50 a day. It's about the phone; the admin panel doesn't need it |
| Anyone with a page in the admin panel (2026-11-11) | Sends feedback from the admin panel's sidebar; up to 50 a day |
| Anyone else, signed in or not | Reaches *Send feedback* by tapping the church logo at the foot of More three times, **only on a phone the app has verified** (App Attest); up to 5 a day per phone |
| **Feedback Triage** (`feedback.triage`, with `feedback.send`) | The admin panel's Feedback board: reads feedback with its pictures and videos, moves it along, keeps notes, blocks and unblocks a phone, deletes a ticket; gets the *New feedback* notification |
| **Feedback Triage, with phone data** (`feedback.*`) | Everything above, and downloads the copy of a phone's stored data that comes with each ticket (`feedback.phonedata.read`) |

- The three roles are listed under *Insights and health* in Access. None of the permissions is sensitive, so a
  connected tool (Claude) can use them (*Claude's default*: the phone-data copy can hold a member's private things,
  but the owner wanted it only for debugging by people who could already see it).
- Sending and looking after feedback are separate (owner, 2026-11-11). Looking after it is admin-panel only: triage is
  dense work (AGENTS.md, *Where staff and leader tools live*).

## Sending (the phone)

- **`/feedback/send`**, chosen on the decision page (2026-11-11): *The screenshot leads*. The first chosen picture sits
  large in its own 9:16 frame at the top, with a filmstrip of the others and a + to add more; empty, it is one tall
  dashed *Choose screenshots or videos*. Then *Tell us about it* (generic on purpose: problems and ideas alike) and a
  quiet line saying what goes with it. Send is in the foot bar. Words or a picture is enough; nothing else is required.
- **Recorded by itself:** app version and build, platform and OS version, the phone's model, whether it's a
  development build, the environment (on the server), the page they came from before More (`RecentPages`), who is
  signed in, the install, and **the phone's state** (sync state, online, signed in, every feed's rows, size and cursor,
  what's waiting in the outbox and why, time zone and language) as JSON. **A copy of the phone's local store**
  (`copyStore`, a SQLite backup) goes too, as an attachment only phone-data holders can open.
- **Through the outbox** (`IFeedbackService.Send`, `[AppInstallOrSignedIn]`): the phone makes the ticket's id, keeps
  the files on the phone, and sends the pictures, videos and the store copy with it, offline or not ([offline-and-caching.md](../architecture/offline-and-caching.md),
  *Files in the outbox*). The page says *Thanks, it's sent* or *Thanks, it's on its way* (waiting for a signal).
- **Limits** (owner: 5 a day; *Claude's default*: 50 with the role, as a guard): up to 6 pictures or videos; a picture
  up to 25 MB, a video up to **150 MB** (about two minutes of screen recording; *Claude's default*), the store copy up to
  100 MB. Over the day's limit the page says *This phone has sent 5 today* and that it can send more tomorrow; a
  sixth that was already queued waits in the outbox and goes the next day.
- **Blocked phones:** a blocked install can't send or upload; the page says *Feedback isn't available on this phone* and
  to speak to the church office. A phone that can't prove itself and has no role sees the same.
- **Signed out works** with no new path and no direct call: the new tier `[AppInstallOrSignedIn]` (a proven install, or
  a signed-in person) is on `Feedback.Send` and on Media's three upload methods, and a Media area can accept uploads
  from an install (`IMediaAreaAccess.MayUploadFromInstallAsync`). Feedback allows an install that isn't blocked and is
  under the day's limit, and caps upload calls per phone a day.

## Sending from the admin panel (2026-11-11)

The owner: "how do i submit feedback in the admin dash? (i.e. for admin dash feedback)", then "sure, send feedback just
above account in bottom left."

- **Who** (owner, 2026-11-11: "anyone who has access to admin can feedback in admin"): anyone signed in to the admin
  panel with at least one page of it in their sidebar, whatever their role: staff, a ministry leader, the Owner. A
  member with nothing there besides Home and *Places in the app* can't. It's asked through Access
  ([permissions.md](../architecture/permissions.md), *Using the admin panel at all*), on the sidebar and on the server.
  No triple tap and no per-phone limit; 50 a day per person is the guard. The phone is unchanged: `feedback.send` still
  shows *Send feedback* in More, and everyone else uses the triple tap on a verified phone, 5 a day. (Until this
  date the admin panel also needed `feedback.send`.)
- **Capture this page** works in Safari (owner, 2026-11-11: "capture this page worked in safari fine").
- **Where:** *Send feedback* in the sidebar's foot, just above Account. A feature adds a sidebar entry like it with a
  `SidebarAction` (Platform.Client), whose sheet the sidebar shows over the page.
- **The sheet**, chosen on the decision page (2026-11-11, *A sheet over the page*): *Capture this page* (the browser
  asks to share the tab, then the sheet hides while it takes one frame; the owner wanted it offered), *Choose files*,
  and ⌘V or a drop anywhere on the sheet, up to 6 pictures and videos; *Tell us about it*; a line of what goes with it;
  Cancel and Send. Closing it keeps what you wrote and chose until you send. After sending it closes and the sidebar
  says *Thanks, it's sent* for a few seconds (owner: thanks and close, no ticket number, no list of what you've sent).
- **Recorded by itself:** the page (route and title), the last 8 pages opened, the browser and system, the window size,
  light or dark, the admin panel's version (its build's informational version), the environment (on the server) and
  the account. No phone state or store copy.
- **Files** upload as soon as they're chosen, straight from the browser (`IDirectUploads`, Platform.Client, filled by
  Media's `[AdminOnly]` direct upload) into the ticket's own *Made by the app* Feedback area, private to
  `feedback.triage`; one removed or never sent goes with the 4-day clean-up. All calls are gRPC
  (`Feedback.Send` with `Source = AdminPanel`).
- **Shared pieces:** `FilePick` takes paste and drops and can hand the chosen files to its caller; the thumbnail
  (`AttachmentThumb`) is the phone's filmstrip's too.

## The board (admin panel)

Chosen on the decision page (2026-11-11) after two re-rolls steered by the owner ("more kanban, more GitHub like"), then:
"build the board with ticket docked, but make sure a command click opens the ticket as a page in a new tab".

- **Technical → Feedback** (`/feedback`): numbered tickets as cards in columns New, In progress, Fixed to check and
  Resolved (the 20 most recent; find older ones by number or words), with Not doing folded at the end. A card shows
  its first picture, `#number`, who, the first line, the app build, the phone and how many files.
- **Where it came from:** a card from the admin panel is labelled *Admin panel* with its browser instead of an app build
  and phone; *Everywhere*, *The app* and *The admin panel* above the board filter by it. Its ticket shows the browser,
  window, page and the pages before it instead of *This phone*.
- **Click** a card to dock the ticket beside the board; **⌘-click or Ctrl-click** (or middle-click, or *Open as a page*)
  opens `/feedback/{number}` in a new tab. **Drag** a card to another column to change its status, or use the
  ticket's status: its stages as a row of arrows, passed ones ticked, the current one filled; click one to move it there
  (owner, 2026-11-11: the segmented row "look like tabs, not progression buttons"; *Pipeline of arrows* chosen on the
  decision page).
- **The ticket:** its pictures (full size) and videos (playable), the words, *What's happened* (status changes and
  notes, with who and when), *Add a note*, what was sent with it, this phone (how many it sent, *Block this phone*
  with a reason, or *Unblock*), *Download a copy* of its stored data for phone-data holders, the phone's state
  (folded), and *Delete this feedback* (asks first).
- **Blocked phones** is the board's second tab: each blocked phone with its reason, who blocked it and when, and
  *Unblock*. Create means delete: a block is removed there or on any of its tickets.
- **Live:** every change writes a marker to `feedback.desk` (audience `feedback.triage`, no payload), so a new ticket
  appears without reloading.

## The terminal tool

`tools/feedback/feedback.sh`, with the same *Connect a tool* sign-in as `tools/health/errors.sh` (the secret in the
login Keychain as `gsbc-tool-<env>`, `tools/health/connect.sh <env>`). All calls are gRPC.

```bash
tools/feedback/feedback.sh --env dev                       # open feedback, newest first (--all for closed too)
tools/feedback/feedback.sh --env dev 42                    # #42 in full; saves its files and the phone's state to a folder it prints
tools/feedback/feedback.sh --env dev 42 --status in-progress --note "Reproduced on an SE"
tools/feedback/feedback.sh --env dev --source admin            # only what came from the admin panel (--source app for the app)
```

Each ticket's line says where it came from; one from the admin panel prints its browser, window, page and the pages
before it. A closed ticket says the day its files go.

Statuses: `new`, `in-progress`, `fixed`, `resolved`, `not-doing`. The account needs Feedback Triage, and Feedback Triage
with phone data to download the store copy. `--env local` reaches a station's backend on `localhost:5400`.

## Keeping and deleting

- **Closing deletes the files a week later** (owner, 2026-11-11: "it should auto delete after say 7 days, unless an
  admin directly deletes the feedback. only because it is possible someone might *accidentally* move it to resolved
  before it is"). 7 days after a ticket is Resolved or Not doing, the hourly clean-up deletes its pictures, videos and
  store copy from Media (`IMediaSystemFolders.ForgetAsync`); the words, details, phone state and notes stay as a short
  record. Reopening within the week keeps them, and closing again starts the week again. The board's card, the
  ticket and the terminal tool say the day they go. Deleting a ticket removes everything at once.
- **A closed ticket is deleted entirely 180 days after it closed** (*Claude's default*); staff can delete one at any
  time.
- **Files sent but never used** (a send that was refused or discarded) are deleted after 4 days.
- **Deleting an account** (`IPersonData`): their tickets lose who sent them, and the copies of their phone's data are
  deleted. Notes they wrote stay, by "Someone".

## Notifications

| Kind | Who | When |
|---|---|---|
| `feedback.new` *New feedback, once a day at midday when there is any* | holders of `feedback.triage` | a digest at 12pm church time when tickets arrived since the last one: "New feedback today: 3 reports", opening the board in the admin panel (on the phone it opens the notifications list, whose row says to look in the admin panel). Nothing on a day without feedback, and your own feedback isn't counted for you |

Owner, 2026-11-12 (feedback #16): "Spammy. Would rather maybe a daily digest if there was any feedback today. Feedback
isn't urgent." Once a day at midday (owner, 2026-11-12), built on the platform's digests
([notifications.md](../architecture/notifications.md), *Digests*); it replaced one waiting notification per ticket.
Waiting rows already sent still settle when their ticket leaves New.

Staff settings and quiet hours apply as for every staff notification.

## Built

`Feedback.Contracts` (the `Feedback` and `Desk` services, tickets, limits, permissions, the feed, the notification),
`Feedback.Api` (schema `feedback`: tickets, attachments, notes, steps, blocks; sending, the desk, keeping, dev sample
tickets), `Feedback.Client` (`FeedbackOnPhone`, `PhoneState`, `RecentPages`, the admin's `FeedbackDeskClient` and the
styles), `Feedback.App` (`/feedback/send`, and `/feedback/{number}` pointing to the admin panel), `Feedback.Admin`
(`/feedback`, `/feedback/{number}`). Shared pieces it added: the `[AppInstallOrSignedIn]` tier, Media areas with their
own file types and sizes (videos and SQLite only where an area asks), uploads from an install, feature-side deletion,
`FilePick` choosing several files of the kinds asked for, `MediaVideo` and `MediaPreview`, the native `copyStore`, and the
phone's model and OS version.

Tests in `Feedback.Api.Tests`: who may send and how many a day; a blocked phone can't send or upload until unblocked;
a resent ticket is stored once; files go 7 days after closing (kept if it is reopened, at once if it is deleted) and the words stay; 180 days after closing; from the admin panel for anyone with a page there and not for a member with none, without the role, its files in the same area, and the board filtering by where it came from; deleting an
account; the phone data only for those who may read it. In `Media.Api.Tests`: an area takes only its own file types and
sizes.

## Open, for the owner

- Every *Claude's default* above: 50 a day with the role, 150 MB videos, 180 days after closing, the phone-data
  permission not being sensitive.
- Android: the data layer is still a stub, so feedback with files (and the store copy) sends from iPhone only until it
  is built.
- Whether the person who sent it should hear when it's fixed (today they don't; the phone only sends).
