# Chats

Status: built 2026-11-11 (Ministries Stage 4), overnight while the owner slept. The design is the owner's interview in
[ministries.md](ministries.md) (*Chats, built well, from the first release*, *Asking to join is a chat*,
*Safeguarding, for later*). Where the interview didn't settle something, Claude picked and built it; every pick is
marked *Claude's pick* below and on the decision page (`.impeccable/mocks/chats/decision.html`, sketches beside it),
for the owner to redirect.

## What people can do

- **Chats live in a ministry.** Leaders start a **group chat** (everyone in it writes) or a **channel** (only leaders
  post; everyone reacts). A chat is for **the team**, **attendees and their parents** or **parents only**, the same
  audiences as posts, and membership follows the audience by itself. *Chosen people* chats exist on the server (a
  *Chat member* role at the chat) and are used for asking to join; choosing people by hand isn't in the start sheet
  yet.
- **Who can start a chat is per ministry** (owner, 2026-11-12: "make it configurable per ministry"; default leaders
  only). Settings → What it uses → *Who can start a chat*: leaders, or the team too. Team members start **group chats
  for the team**; channels and chats with attendees or parents stay with leaders, who also change and delete chats.
  `Chats.Start` is `[SignedIn]` and checks it (`ChatPlace.TeamMayStart`).
- **A ministry's people changing reaches its chats at once** (2026-11-12): Chats handles Platform's
  `IMinistryPeopleChanged` as well as watching grants.
- **Where:** *Chats* (`/chats`: every chat you're in, newest first, a parent's yes waiting on top), a ministry's
  **Chats tab**, a Hub tile, and in the admin panel the **Chats desk**. Deep links: `/chats/{id}`;
  `/ministries/{id}/chats`.
- **In a chat:** reply (quoted), react (six quick reactions), mention with `@`, read receipts ("Seen by 4" under your
  last message, tap for who), edit and delete your own messages ("edited", "You deleted this message"), report,
  mute (mentions still reach you). Leaders can delete any message in their ministry's chats and delete the chat.
- **Photos and files** (built 2026-11-11, on the shared offline uploads): the composer's paperclip keeps a picked photo or
  file on the phone, and it goes with the message through the outbox; the message waits for its files and gets their
  ids (`ChatFiles.MediaIdInSend`). On the server they're the *Chat files* area (`chat-files`): Chats answers who may
  upload and read (`ChatMediaAccess`, `IMediaAreaAccess`: whoever may write in the chat; whoever is in it, or may check
  it), the server only accepts files the library verifies, and files them under the chat. Files are kept with the
  message whatever happens to it. Photos show in the bubble; a file opens through a short-lived private link (online).
- **Offline:** sending works with no signal. A message shows *Sending* until the server has it and *Couldn't send ·
  Try again · Delete* if it gives up; drafts stay while you move around the app.
- **A quiet line** opens every chat: "Church staff can check messages in this chat." (with young people: "Parents of
  the young people here can read this chat. Church staff can check it too."). It is the first line of the history and
  scrolls away; it is never a banner.

## Who is in a chat: Access, worked out per chat

Everyone in a chat comes from Access ([permissions.md](../architecture/permissions.md)); Chats keeps no list of its own.

- **An audience chat:** the people who hold the audience's node at the ministry (`ministries.team.view`,
  `ministries.attendees.view` or `ministries.parents.view` at `ministry:<id>`), which Ministries hands over through
  `IChatPlaces` (Platform.Api) so the features stay apart. That includes leaders of the ministries above (their roles
  apply to the ministries under them). **A grant held everywhere is not membership** (*Claude's pick*): the Owner's
  `*` or a global role doesn't put someone in every chat; only a grant at that ministry does (`ChatFacts.HeldThere`).
- **A chosen-people chat** (an ask to join): holders of the hidden *Chat member* role at `chat:<id>` (a new scope
  kind), given by Chats.
- **Leaders** of a chat are whoever holds `chats.manage` at its ministry (Ministries' Leader and Admin roles carry
  it). They post in channels, delete messages and chats, and see the flags.
- `participants` (schema `chats`) is a **cache of that answer**, recomputed whenever Access, the ministries, families or
  blue cards change (`MembershipWatcher` follows their feeds, and sweeps hourly). It also holds each person's read
  position and mute.

## Safeguarding

All of it is plain code in `ChatMembership.Decide`, unit tested (`ChatMembershipTests`).

- **A young person gets nothing until a parent says yes.** Under 18 (from People's birth dates, `IFamilies`), they wait;
  their parents are in the chat from the start as **readers** and get a *Waits* notification ("Is Tom OK to join
  …?") once the leader has written the first message (the why). The parent answers in the chat: *Yes, Tom can join*
  or *Not now*. On yes the child gets the chat **with everything so far**. A young person with no parent linked can't
  join, and the leaders see why.
- **Parents read** every chat their child is in (team chats too: the interview's "parents can read the chats their
  child is in"), and the chat says so. **A parent can write for their child** once the child is in ("Sarah Example,
  for Tom"), never in a channel.
- **Two adults with a current blue card** (`ISafeguardingStandings`, answered by People from Safe Ministry Check and hand-entered
  cards): a chat with young people and fewer than two such adults (parents reading don't count) is **flagged** to its
  leaders and on the desk, never blocked.
- **Checking:** a ministry's leaders check every chat in their ministry and those under it (`chats.check`); the
  **Chat auditor** role (global, sensitive; `chats.audit` and `chats.check`) checks any chat. Checking happens in the
  admin panel, asks *why*, and is **logged** with who, when and the reason; the log is kept forever and shown to the
  ministry's leaders (their chats) and the auditors (everything). A check shows every version of every message,
  including what a deleted message said.
- **Reporting:** from a message, to the ministry's leaders or to "the church's safety team" (the auditors; *Claude's
  pick of words*). A message from a leader goes to the auditors. Leaders get an ordinary notification that opens the
  chat; auditors get an **urgent** one (skips quiet hours) and handle it on the desk: read it in context, pause the
  sender, say what was done. The reporter isn't named to the person reported.
- **Pausing:** an auditor pauses someone with a reason (the hidden *Chats paused* role). **A paused person loses their
  chats** until let back in (*Claude's pick*, stricter than the sketch's "can still read"): nothing new reaches their
  phone, their chats are taken off it, sending is refused and nothing they try is stored. Letting them back in
  restores everything. Both are logged.
- **Everything is kept:** every version of every message (`message_versions`: sent, edited, deleted), deleted chats
  (soft-deleted, still checkable by auditors), reports and their outcomes, and the audit (`audit`: chats started,
  changed and deleted, people added, consent, messages sent, edited and deleted, checks, reports, pauses).

## How messages reach phones: Sync, per person

The app reads chats only from its local store and writes only through the outbox (AGENTS.md).

- Two **own-record feeds**: `chats.chats` (a card per chat, personal: your role, whether you may write, your read
  position and mute, the people in it, flags for leaders, a parent's yes waiting) and `chats.messages` (one row per
  message **per recipient**, grouped by chat, sorted by its number in the chat). A row's owner is its recipient, so
  Sync only ever gives a phone its own rows, and someone who leaves, is paused or is still waiting for a parent never
  has a message in their store.
- **Sync reads own-record feeds by owner in SQL** (new index `(feed, scope_id, cursor)`, 2026-11-11): before, every
  phone scanned every row of an own-record feed and filtered in memory, which per-person chat rows would have made
  slow. The inbox, preferences and pins feeds get faster too.
- Joining a chat (or a parent's yes) writes the whole history to the newcomer; leaving writes deletions.
- Writes go through the outbox with ids made on the phone, so a retried send is recognised: `Send`, `Edit`,
  `DeleteMessage`, `React`, `MarkRead`, `Mute`, `Consent`, `Report`, `Start`, `Change`, `Delete`, `ChangePeople`,
  `AskToJoin` (gRPC `gsbc.chats.Chats`). The admin panel's desk uses `gsbc.chats.Oversight` directly (online only, as
  the admin panel is), and follows the `chats.oversight` marker feed.
- **Notifications:** each message notifies everyone in the chat but the sender, unless they muted it; a mention
  notifies even when muted (*Claude's pick*). Message notifications are per ministry in Notification settings.

## Deleting

- **A chat:** its leaders delete it (app or desk). Everyone loses it; the church keeps its messages and log.
- **A message:** its sender or a leader. Everyone sees that it was deleted; the text is kept for checks.
- **An account** (`IPersonData`): **the messages stay, shown as from "Deleted account"** (the owner's call that
  versions are kept forever for safeguarding); their reactions, read positions, mutes and consents go; the audit keeps
  the action without the name.

## Claude's picks (owner can redirect)

From the decision page, each built:
1. **Chats list:** one list across ministries, newest first, a parent's yes on top; plus a Chats tab per ministry.
2. **Conversation:** bubbles (yours navy on the right, others sand on the left), the shared `MessageBubble` (Platform.Ui).
3. **Message actions:** long-press opens a sheet with reactions and actions.
4. **Starting a chat:** one sheet; the first message (the why) is required.
5. **A parent's yes:** in the chat, where the composer would be.
6. **Reporting:** a sheet choosing leaders or the safety team; handling is in the admin panel.
7. **Admin:** one Chats desk with Reports, Chats, Paused people and Checks log; reading a chat asks why.
8. **Asking to join:** the Join sheet gains the editable first message; leaders add from the chat.

Also picked: a grant held everywhere isn't membership; leaders of the ministries above are in the ministry's audience
chats; a paused person loses their chats (not read-only); **earlier versions of an edited message are shown only to
people checking**, not to everyone in the chat (the sketch showed them to all); mentions notify through mute; members
can't start chats (leaders only; per ministry since 2026-11-12, above).

## Not built yet

- **Choosing people by hand** in the start sheet (a chosen-people chat), and adding or removing people from the
  people sheet (the server's `ChangePeople` works).
- **The auditors' coordination chat** about a report (its shape is open in the interview).
- **Direct messages** (tabled by the owner).
- A system line in the chat when a leader adds someone who asked to join (they're added; the chat doesn't say so yet).
- A roster link from a chat ("You're on Sunday"), once Rosters lands.

## Open for the owner

1. "The church's safety team" as the words members see for chat auditors (the role stays *Chat auditor*).
2. A paused person losing their chats entirely, or keeping them read-only.
3. Earlier versions for everyone in the chat, or only for checks (built: only checks).
4. A young person asking to join by themselves: the ask chat follows the minors' rules (a parent's yes before they get
   it; the parent reads it).
5. Pausing from the phone: an admin action in the app, so it needs your yes (not built).
