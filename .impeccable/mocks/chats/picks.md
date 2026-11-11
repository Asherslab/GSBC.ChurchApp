# Chats (Ministries Stage 4): the picked designs

Claude's picks while the owner slept (2026-11-11); the owner can redirect any of them on `decision.html`.
Sketches: `.impeccable/mocks/chats/*.html` (shared CSS `sketch.css`). Names in copy below are placeholders;
`{Ministry}`, `{Chat}`, `{Name}`, `{Child}` are filled from data. Every string is a plain sentence (DESIGN.md).
Platform.Ui names are real components; "new" marks a part to build, generic, in Platform.Ui where it says so.

## Shared vocabulary

- **Kinds:** *Group chat* ("Everyone in it can write.") and *Leaders post only* ("Only the leaders write. Everyone
  can react."). Row icon: `chat` for a group chat, `megaphone` for a channel.
- **Audiences** (same four words as posts): *The team*, *Attendees and their parents*, *Parents only*, *Chosen
  people*. Shown as a pill-size 700 word in the audience box colour: team `primary`, attendees `accent`, parents
  `marker-ink`, chosen people `ink-2`. Row tile tint follows: team primary at 10%, attendees `RowTone.Accent`,
  parents `RowTone.Warm`, chosen `RowTone.Primary` (sand).
- **Safety team** is what members read for the *Chat auditor* role ("the church's safety team"). Open for the owner.
- Avatars: `Avatar` (initials) everywhere a person is.

## 1. Chats list (pick: newest first) — `list-recent.html`

**App-wide `/chats`** (opened from a new Hub tile "Chats"; also reachable from More):
- `Masthead` title "Chats", note counts what needs you: "3 unread · 1 waiting for you"; bell in the top bar.
- One `ListGroup` of `ListRow`s, newest message first. Row: leading tile (kind icon, audience tint), title = chat
  name, subtitle = "{Sender first name}: {text}" (or "You: …", "Photo", "File: {name}"), trailing = time
  (tabular: "9:41am", "Yesterday", "Tue", "12 Nov") over an orange count capsule (the TabStrip's) when unread.
  `Unread` = true gives the 9px orange dot and 700 title; read rows title 500.
- A parent reading their child's chat: subtitle led by the small `eye` icon and "For {Child} ·".
- Muted: a small `bell-off` icon in ink-3 under the time; the row stays in place (not folded).
- **Waiting for you** (a parent's yes) sorts first, on the warm tile with the people icon, set on marker at 14%
  like a special agenda row: "{Chat} wants {Child}" / "{Ministry} · read why and approve" + chevron.
- An ask-to-join chat: quiet tile, title "Asking to join {Ministry}" for the asker, "{Name} asking to join" for
  leaders.
- Hint under the list: "Chats from every ministry you're in, newest first. A ministry's own chats are also on its
  Chats tab."
- Empty: `EmptyState` chat icon, "No chats yet", "When a ministry you're in starts a chat, it shows up here.",
  quiet button "Find a ministry".
- Hub tile: title "Chats", unread count capsule, one line with the newest unread: "Sam in Sound Team: Can someone
  cover 6pm?".
- Long-press a row: a small Sheet with *Mute* / *Unmute* ("No notifications from this chat. It still shows here.").

**A ministry's Chats tab** (`/ministries/{id}/chats`, a new area in `MinistryAreas`, shown while the ministry has
chats turned on, between Calendar and Files): the same rows filtered to the ministry, the subtitle led by the
audience word ("Team · Sam: Can someone cover 6pm?"), "Leaders post ·" for a channel. Tab carries the unread count.
- Leaders: a primary "New chat" (plus icon) above the list, as News' tools.
- A chat still waiting for parents (leader's view): warm clock tile, subtitle "{Audience} · 3 of 5 families have
  said yes".
- Hint: "Church staff can check messages in chats. Long-press a chat to mute it."
- Settings › *What it uses*: a `ToggleRow` "Chats", caption "Group chats and channels for this ministry's people."

## 2. A conversation (pick: bubbles) — `conv-bubbles.html`

Pushed page, solid navy top bar: back "Chats" (or the ministry), centred title = chat name with a pill-size line
under it in on-masthead-2: "{Audience} · 8 people" ("Leaders post · 14 people"; parent: "{Child}'s chat · you
can read it"). End: people icon button → the people sheet (members as Avatar rows, leaders marked, a parent shown
under their child; leaders get "Add someone" via `PersonFinder`). Channel's end button is mute.

Message list (new `ChatMessages`, feature-side; bubbles are a new generic `MessageBubble` in Platform.Ui):
- **Start of the chat:** a centred caption in ink-3 with a shield icon: "Church staff can check messages in this
  chat." With young people: eye icon, "Parents of the young people here can read this chat. Church staff can check
  it too." It is the first item of the history, so it scrolls away; never a banner.
- Day separators: centred pill-size 700 ink-3 "Today", "Yesterday", "Monday 9 November".
- **Others:** left, sand bubble (18px radius, 6px at the tail corner of the first in a run), 30px Avatar on the
  first of a run, name in caption 600 ink-2 inside the bubble ("Casey Demo · leader" with "· leader" at 400 ink-3).
  Body text at body size, 1.38. Time in pill size ink-3 floated to the bubble's end.
- **Yours:** right, primary fill, on-primary text, time at 72% white.
- **Written for a child:** name line "Sarah Example, for Tom" (yours: "You, for Tom").
- **Reply:** a quote block inside the bubble (page colour on sand; white 14% on navy) at the 10px radius: sender
  at 600 over one clamped caption line. Tapping it scrolls to the original. No coloured side stripe.
- **Mentions:** "@Casey" in accent 600; a mention of you gets accent at 14% behind it. On navy: white, underlined.
- **Reactions:** capsules under the bubble (26px, page fill, hairline ring, emoji + tabular count); yours tinted
  accent at 12% with an accent ring. Tap toggles yours; long-press lists who.
- **Photos:** in the bubble's place, 14px radius, up to 220px wide; tap opens full screen. **Files:** inside a
  bubble as a row: 34px tile with the file icon, name at 600, "PDF · 240 KB" caption. Private media, gRPC (SPEC §12).
- **Edited:** "· edited" after the time. **Deleted:** an outlined empty bubble, caption italic ink-3:
  "{First name} deleted this message" / "You deleted this message".
- **Sending:** your bubble at 72% with "Sending" and a clock icon in place of the time.
- **Couldn't send:** bubble at 60%, then under it, right-aligned, caption 600 in danger with an alert icon:
  "Couldn't send · Try again" (Try again in accent). Messages are queued locally and sent through Sync.
- **Seen by:** under your last message only, pill-size 600 ink-3 with a check: "Seen by 4" (tap → who, in a sheet).
- **Composer:** frosted bar, hairline above: 40px round sand attach button (photo, file), the sand capsule field
  "Message", 40px round primary send button (strong sand and ink-3 while empty). Typing "@" lists members.
  Replying: a context line above the field: reply icon, sand pill "{Name} · {text}", an x to cancel.
- **A parent in their child's chat:** the context line holds an accent chip with the child's Avatar, "For Tom",
  and "Others see "Sarah Example, for Tom"."; placeholder "Message for Tom". A parent with two children in it
  chooses which chip.
- **A channel, not a leader:** the composer is replaced by a stop bar: quiet lock tile, "Only the leaders post
  here." over "You can react. To talk about it, use the {Group chat} chat." (second sentence only when one exists).
- **Paused:** stop bar with a pause tile: "You can't send messages for now." over "The church has paused your chats
  while it looks into something. You can still read. Questions: speak to the church office."
- `LiveData` on the chat's feed; it reloads on reconnect and when permissions change (paused, removed).

## 3. Message actions (pick: a sheet) — `actions-sheet.html`

Long-press a bubble (haptic): it lifts above the 38% scrim; a `Sheet` opens with:
- A sand capsule row of six reactions (👍 ❤️ 🙏 😂 🙌 😮) and a "more" smile button opening the full picker.
- `ListRow`-style action rows (52px, primary icon, 600 label): **Reply**, **Copy the text**, **Edit** (yours),
  **See earlier versions** (trailing caption "Edited once" / "Edited 3 times", only when edited), **Seen by 4**
  (yours), **Delete** (yours, danger), **Report this message** (others').
- **Edit** happens in the composer: context line with a pencil, "Editing your message. Everyone can see earlier
  versions.", an x to stop; the send button becomes a check. The bubble being edited dims to 55%.
- **Earlier versions** sheet: title "Earlier versions", lead "{Name}'s message, newest first. Everyone in the chat
  can see these." Each version a hairline block: caption 600 ink-3 "Now · edited 9:44am" / "Sent 9:12am", then the
  text; changed words marked (added accent at 14%, removed struck in ink-3). Primary "Done".
- **Delete** confirms in a sheet: "Delete this message?", "Everyone will see "You deleted this message" in its
  place. The church keeps a copy, so staff checking the chat can still read it." Danger block "Delete the message",
  quiet block "Keep it".

## 4. Starting a chat (pick: one sheet) — `start-sheet.html`

`Sheet` titled "New chat in {Ministry}", opened from the Chats tab's "New chat" (leaders; members too when the
ministry's setting lets them). Fields, the stack gap between:
- `TextField` "Name" (no placeholder).
- `Segmented` Stacked, labelled "Kind": Group chat / Leaders post only, each with its detail line.
- `Segmented` Stacked, labelled "Who it's for": The team ("Leaders and helpers."), Attendees and their parents
  ("Young people who come along, and their parents."), Parents only, Chosen people ("You pick each person."). Chosen
  people adds a `ChipPicker` of the ministry's people ("Find someone in {Ministry}").
- A sand note (the Post sheet's): who reads it in a sentence: "16 people: 5 young people, their 8 parents (who can
  read it) and 3 leaders."
- When anyone under 18 is in it:
  - Adults check, `Notice` Info with the card icon: "**Two adults with a current blue card are in it:** Casey Demo
    and Alex Sample." Fewer than two: `Notice` Warning, "**Only one adult in it has a current blue card.** A chat
    with young people needs at least two. {Name}'s card has run out." with a small quiet "Add a leader".
  - `Notice` Warning with the people icon: "**Parents say yes first.** Your first message goes to each parent.
    Their child gets the chat once they approve; until then it's just the adults."
  - `TextField` multiline "First message: why you're starting this chat", placeholder "Parents read this before
    they say yes." (required).
- `SaveBar` in the sheet's actions, the button naming the result: "Start the chat" or, with young people, "Start
  the chat and ask the parents". Reasons when it can't: "Give it a name to start it.", "Add a second adult with a
  current blue card to start it.", "Write the first message to start it." Under it, caption: "Church staff can check
  messages in this chat." `UnsavedGuard` covers the sheet.
- After starting, the leader's chat opens with a warm `Notice` at the top until every family has answered: "**3 of
  5 families have said yes.** Their children are in. Waiting for the {Surname} and {Surname} families." + text link
  "See who".
- A chat's settings (people icon → leaders see *Settings*): rename, kind, who it's for, mute; **Delete the chat**
  (create means delete): danger text link, confirming "Everyone loses the chat. The church keeps its messages and
  the log."

## 5. A parent's yes (pick: in the chat) — `consent-inchat.html`

- Push (time-sensitive off): "Casey Demo wants Tom in a chat" / "{Chat}, in {Ministry}. Read why and say yes or not
  now." Also the warm row at the top of Chats, and an inbox item.
- Opens the chat as the child would see it (the parents line, the leader's first message). The composer's place
  holds the answer, on the page colour with a hairline above: "Can Tom join this chat?" (row size 700), a caption
  "16 people: 5 young people, their parents and 3 leaders. Casey Demo and Alex Sample have a current blue card.
  You'll be able to read everything Tom sees.", block primary "Yes, Tom can join", block quiet "Not now".
- Two or more children: "Can Tom and Mia join this chat?" and a row per child (Avatar, name, small quiet "Not now",
  small primary "Yes").
- After Not now: the row reads "You said not now for Tom · Change"; the leader sees "Sarah Example said not now for
  Tom." in the waiting notice's "See who". Nobody else is told.
- The child sees nothing until a parent says yes; then the chat appears in their list with the whole history.

## 6. Reporting (pick: a sheet, then the admin panel) — `report-sheet.html`

- From the action sheet, `Sheet` "Report this message": the message as a callout (sender and time, then text),
  "Who should see it?" as the Join sheet's choice cards (`.ministry-choice`): **{Ministry}'s leaders** ("{Names}.
  For things the leaders can sort out.") and **The church's safety team** ("Staff and pastors who look after
  safety in chats. They're told straight away and can pause someone from chats."). Leaders are chosen first, except
  when the message is from a leader: then the safety team is chosen and leaders are off. `TextField` "Anything to
  add? (optional)". Block primary "Send the report", caption "{First name} isn't told who reported it."
- Afterwards the bubble carries a caption only the reporter sees: flag icon, "You reported this to the safety team"
  (or "to the leaders"); a `Notice` Info: "**Thanks, it's with the safety team.** Someone will look at it soon. If
  anyone is in danger, call 000."
- Safety team: a time-sensitive push "A message was reported to the safety team" / "In {Chat}. Open it in the admin
  panel."; the inbox's waiting row with "Do this in the admin panel". Reports to leaders notify the leaders
  normally and appear on the desk for them (their ministries only).

## 7. Admin panel (pick: a Chats desk with four tabs) — `admin-reports.html`, `admin-check.html`, `admin-paused-log.html`

`/chats` in the sidebar under Church life, with an orange count of waiting reports. `TitleBand` "Chats", lead "2
reports waiting · 1 person paused · Opening a chat or a report here is logged". `TabStrip` (link mode): **Reports**
(count), **Chats**, **Paused people**, **Checks log**. Leaders see their own ministries; the safety team sees all.

**The logged line** (new generic `LoggedNotice`, Platform.Ui): a navy (masthead) box at the 12px radius,
on-masthead text, the eye icon in marker: "**This check is logged.** {Your name} opened this report at 9:20pm on 11
November." / "… reading {Chat} since 9:22pm. Leaders of {Ministry} can see the log." It heads every pane that shows
messages, and never scrolls out of its pane's top.

- **Reports:** `FilterChips` Waiting / Handled / Everything with counts; `DataTable` in a `SplitView`: Reported
  (tabular, 700 while waiting), Chat (ministry under it in caption), Message from, Sent to (`Pill` Danger "Safety
  team" / Quiet "Leaders"), By. Safety-team rows tinted danger at 8%. Detail: the logged line, "Reported to the
  safety team", "By {Name}, a parent, at 9:16pm: "{note}"", then "The messages around it" (five either side, read
  only, the reported one ringed 2px danger), actions: Danger "Pause {Name} from chats", quiet "Open the whole chat",
  quiet "Earlier versions"; `TextField` "What was done" ("Kept with the report"); primary "Mark as handled", quiet
  "Talk about it with the safety team" (opens or makes the team's chat for this report); then "What's happened" as
  a time-stamped list (reported, notified 3 people, opened by, paused by, handled by).
- **Chats:** the ministry tree (as the Ministries desk, 220px, `SearchField` "Find a ministry", counts of waiting
  reports) beside a `DataTable` of its chats (Chat, For, People, Last message); a row missing a second screened
  adult says "Needs a second adult with a blue card" in marker-ink 600, and a waiting chat "3 of 5 families have
  said yes". Choosing a row shows a summary panel (people, adults with a blue card, parents can read it, the checks
  of this chat) with a primary "Read this chat" and "Reading is logged with your name and the time." Only that
  button opens the messages (read only, deleted messages show what they said with "deleted 9:40pm" in the name
  line, edited ones open their versions), with the logged line on top. The checks of this chat sit under it.
- **Paused people:** lead "People who can read their chats but can't send, react or start one, in every ministry,
  until someone lets them back in."; `PersonFinder` "Find someone to pause" ("Pause from chats", asks why);
  `DataTable` Person, Paused (when), By, Why, small quiet "Let them back in" → panel "Let {Name} back into chats?",
  "{Name} can send, react and be added to chats again straight away. They aren't told who paused them or why.",
  `TextField` "Why (kept in the log)", primary "Let {Name} back in", quiet "Keep them paused". Hint: "Pausing and
  letting back in are both logged, with who and why."
- **Checks log:** `FilterChips` Everything / Chats read / Reports / Pauses / Deleted and edited messages read, a
  `SearchField` "Find a person or chat"; `DataTable` When, Who, Did ("Read the chat", "Opened a report", "Paused
  Riley Test"), Chat, Why ("Report", "Leader's check"). Hint: "Kept forever. Leaders see the checks of their own
  ministries' chats; the safety team sees everything." No export (it would be a non-gRPC path).

## 8. Asking to join is a chat (pick: the join sheet, then the chat) — `join-sheet.html`

- *Ask to join* opens `JoinSheet`, retitled "Ask to join {Ministry}", lead "This starts a chat with {Ministry}'s
  leaders, {Name} and {Name}." The existing choice cards (who it's for: "Me, on the team", "Mia, on the team" …);
  a child's card adds "Mia is 14. You'll be in the chat with her, and it needs two leaders with a blue card."
  Then `TextField` multiline "Your first message", prefilled "I'd like to chat about joining {Ministry}." (editable,
  required). Block primary "Send to {Ministry}'s leaders". The old note field goes.
- The chat: name "Asking to join {Ministry}" for the asker ("You and 2 leaders"), "{Name} asking to join" for
  leaders. Start line: "A chat with {Ministry}'s leaders. Church staff can check messages in chats." With a young
  person in it the minors' rules apply (two screened adults, parents can read; no parent's yes needed when the
  parent asked).
- The masthead's action pill becomes "Asked · chatting with the leaders" (clock) and opens the chat.
- Leaders: the chat's top holds a warm `Notice` "**{Name} asked to join the team.**" (or "to come along", "for
  Mia") with small primary "Add to the team" / "Add {Child}" and small quiet "Not now". Adding posts a system line in
  the chat (check icon in accent): "Jordan Placeholder added Morgan to the team. Morgan is now in the {Ministry}
  chats." Not now: "Jordan Placeholder said not now. You can keep chatting." The chat stays either way.
- People tab's *Asking to join* section is replaced by these chats (the Chats tab carries the count); until chats
  are on for a ministry, the old asks stay.

## Open for the owner

1. "The church's safety team" as the members' word for chat auditors.
2. Pausing from the phone (decision page 6B) is an admin action in the app: needs a yes.
3. Earlier versions visible to everyone in the chat; a deleted message's text only to people checking.
4. A young person asking to join by themselves: the ask chat follows the minors' rules.
