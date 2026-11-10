# Life Groups

Status: built 2026-11-10 as part of Ministries Stage 2 (below); draft 2026-10-26; Life Groups became a kind of ministry on 2026-10-30. Extends
[SPEC.md](../../SPEC.md) and [ministries.md](ministries.md); the site map it uses is
[site-map.md](site-map.md).

## Life Groups in the calendar (owner, 2026-11-10)

The owner: "Life Groups - their calendars are technically automatic. they run any time a life group round is on. maybe
make a way to link a calendar as the calendar life group timings auto show in (e.g. Services), with times between life
group rounds being set as "not on" automatically and such." Asked how, the owner chose **one "Life Groups" entry** in the
linked calendar: "i do want it to actually display when a life group "isn't on" - i.e. how breaks display for normal
regular events". And **all-year groups only follow the sets of Sundays**, for now.

- **Show Life Groups in** (admin → Life Groups, the row at the top): a calendar, or none. Choosing one makes a single
  regular event, *Life Groups*, in it; choosing another moves it; choosing none takes it off, after asking. Only someone
  who may plan Life Groups and adds events to that calendar (and publishes, if it needs a publisher) can choose it,
  through Access; the picker offers just those calendars. Removing the calendar removes the entry, and Life Groups then
  says *Not shown in a calendar*.
- **It's kept in step automatically** on every change to the sets of Sundays (planning, *Sundays and time*, taking a
  Sunday off, deleting a set). Every Sunday in a set is on at that set's time. Every Sunday from the first planned Sunday
  to the last that isn't in a set shows as not on, as a break does: *Between groups* between sets, *No Life Groups this
  Sunday* for one taken off inside a set. **Horizon** (*default*): the first planned Sunday through the last; nothing
  is added before or after. A set's *shows in the app from* date doesn't hide its Sundays: the calendar says when Life
  Groups meet, not which groups.
- **Times:** the time most Sundays have is the entry's own; other sets' times are those dates' own times, shown as plain
  dates, not as specials.
- **Owned by Life Groups:** Calendar shows it with "Life Groups keeps this in step with its own dates and times" and
  *Change it in Life Groups*, and the server refuses editing, a date's changes and deleting it there. *Tell people* and
  everything else that reads the calendar treat it as any regular event: people's week, Home's *Coming up*, iCal links,
  the digest and notifications.
- **Tapping it** in the app (the week, the month, Home) opens Life Groups; its own page, reached from a link, has *Open
  Life Groups*.
- **All-year groups add no Sundays of their own**: the entry comes only from the sets.

## An all-year group can be off for a set of Sundays (owner, 2026-11-10)

The owner: "we do need to handle an "all year" lifegroup being cancelled for a specific round", for example an all-year
group joining one of that set's groups.

- **On the desk**, under a set of Sundays that hasn't finished, *An all-year group isn't on these Sundays* marks one off,
  with what to tell its people and, optionally, a group of that set to point them to. Opening it again changes the note,
  or *It's on these Sundays after all* undoes it. Planning permission, as for the sets.
- **Shown, not hidden** (*default*): Life Groups in the app keeps the group under *All year* (and *Your groups*) with
  "Not on 8 November to 29 November · the note", so someone looking for it learns why; the desk lists it under the set
  as not on. The group's own page starts with the same words and *Go to* the group it points to.
- **Its people keep their place**; nothing about the group changes. It's back on the next set by itself.
- **Told once:** marking it off (while the set hasn't finished) sends one *Important* notification to everyone who sees
  what it shares with attendees (its attendees and their parents), with the note. Editing the note doesn't send another.
- Deleting the set removes its marks; deleting the group pointed to leaves the note without the link.
- **The calendar's Life Groups entry is unaffected** (the other groups still meet).
- **Not done:** marking the group's own calendar events not on for that set. A group's calendars are ordinary Events
  calendars with events staff add, and nothing ties their dates to a set of Sundays; doing it needs a decision on which
  events count (open below).

## Show in Life Groups (owner, 2026-11-10)

The owner saw the admin Life Groups page say "No Life Groups yet" although Kids Life (under Kids Ministries) existed, asked
how it works for Kids Life, and chose **every group, wherever it sits**. Then: "honestly - "ministry is a life group"
should basically just be a tag that either shows or doesn't show that ministry in the lifegroups lists. there's nothing
special about them otherwise, just where they show and how they might be pushed/discovered". This replaces the Life Group
kind below:

- **A Life Group is any ministry with *Show in Life Groups* on** (Settings → *Life Groups*, or *A new ministry*). It can sit
  anywhere, be top-level and have ministries under it; it is a ministry like any other in every other way. Details in
  [ministries.md](ministries.md#show-in-life-groups-owner-2026-11-10).
- **Every one shows on Life Groups:** the admin desk (each saying where it sits: "Under Kids Ministries"), the app's page,
  Home's rows and the Hub tile. *Your groups* in the app lists the ones you or your children are in. The empty state shows
  only when there are no Life Groups and no sets of Sundays at all, and offers *Add a Life Group* (the new-ministry sheet
  with the switch already on). A "Life Groups" ministry is optional: an ordinary ministry for the rotating adult groups and
  whoever runs them.
- **The sets of Sundays are church-wide** (*default*, Claude's reading of the brief): a set belongs to no ministry; a
  ministry shown in Life Groups is in one set or *All year* (*When it shows*). Planning sets (*Plan the next groups*,
  *Sundays and time*, deleting an empty set) needs `ministries.lifegroups.plan`, a church-wide permission: the Ministries
  Manager role has it (staff), and the new **Life Groups planner** role gives only that. Leading a ministry doesn't give it.
  Existing sets were kept as they were and lost their owner.
- **Adding a Life Group** follows *A new ministry*: under a ministry you may add under (its leaders and staff, through
  Access), or top-level for staff.
- **The room** is now the ministry's *When and where it meets*, and **Join as a parent** is gone: a parent joins for their
  child through *Coming along*, as in any ministry (the earlier rule, a parent joining a children's group for their child,
  still holds). The one thing lost: the app no longer hides "Me" from adults on a children's group.

## Built (2026-11-10)

Since superseded in part by *Show in Life Groups* above: there is no Life Group kind, room or *Join as a parent*, and the
sets of Sundays belong to no ministry.

- **A Life Group is a ministry of kind Life Group** under a *Life Groups* ministry, so joining, leaders, news, files and
  the People tab are the ministries' own (the owner: "all those features are why lifegroups are a subset/type of ministry").
- **No "round" anywhere people see it** (owner, 2026-11-10: "I wouldn't call them rounds to end users"). The app shows
  **Now on** (with its dates), **New groups from …** with *Choose a group* (a paged sheet, one group per page, opening the
  group to join), and **All year**. Each set of groups is kept by its Sundays, time and *shows in the app from* date; its
  staff label is its dates.
- **Staff set them up in the admin panel** (owner, 2026-11-10: "rounds and such for life groups will be managed/created
  through the admin page, not through app"): People → Life Groups, *Plan the next groups* (Sundays one by one or six at a
  time, the time, when they show), *Add a group* (name, line, room, *join as a parent*), and all-year groups. A set can be
  deleted once it has no groups.
- **Parents** (owner, 2026-11-10): a parent joins a children's group for their child (the child is the attendee, and the
  parent sees it through the family link) and joins their own adult group as themselves.
- **A group's Sundays** tab lists its Sundays; its leaders mark one *No class this Sunday* or say what's changed
  ("Combined with another group, in another room").
- **A Life Group can sit under any ministry** (owner, 2026-11-10: Kids Life "does sit under kids ministries, which needs to
  be possible"): made from *A new ministry* (*What it is: A Life Group*) in the app or the admin panel. It still shows on
  Life Groups with its Sundays, and its leaders are that ministry's ([ministries.md](ministries.md), *Follow-ups*).
- **A Life Group is a full ministry, just more temporary** (owner, 2026-11-10: "they're a full ministry, just more
  temporary. should have all the stuff normal ministries do, that's why they're a subset of them", and "it's a whole team of
  people" that needs volunteer chats and calendars). A Life Group has **a team** (leaders, admins, volunteers) with its own
  *Joining the team* setting, and **the people in the group** with *Joining the group* (Open, Ask or Invite only; never "No
  attendees", which the server refuses). Join offers the team as well as the group. Everything else is the ministry's own,
  with no Life Group case in the model or Access: the same roles at `ministry:<id>`, the People tab, the four audiences
  (team-only posts, files and calendars), *What it uses*, combined news and calendars, and moving it under another
  ministry; rosters (Stage 3) and chats (Stage 4) apply the same way. The only differences are its extras (its Sundays, its
  set of Sundays, its room, *Join as a parent*), that it can't be top-level, and that nothing sits under a Life Group
  (*default*: the model has never allowed it; the owner can ask for it).
- **No Sunday reminder** (owner, 2026-11-10). The site map isn't built, so a group shows its room as words.

## A kind of ministry

The owner, 2026-10-30: "Life Groups and Ministries should be somewhat joined, with Life Groups as a
type of ministry. They have a lot of overlap." So a Life Group is a **ministry of kind Life Group**,
in the same model as Music or Young Adults ([ministries.md](ministries.md)):

| Shared with every ministry | Only for Life Groups |
|---|---|
| Name, About (the blurb), who it's for, leaders, colour | **Rounds**: which round a group belongs to, or all year |
| Join and Leave (open to all, per phone), and the notifications switch | The Sunday 9:00–9:45 time, set per round |
| Notices: the Announcements module (an urgent notice is an Important announcement) | The **room** and its map number, and the site-map link |
| Resources: the Files module | **Session changes** (cancelled, moved, combined) |
| The Sundays: the Calendar module, one entry per Sunday of the round, a change shown on its entry | "Join as a parent" for Nursery and Kids Life |
| | The Sunday reminder at 8:30 |

Joining a round's group still lasts until the round ends: the membership stays, but the group drops
out of "Your groups" once its round is over. A Life Group is always open (Join), all its items are
for Everyone, and it has no chats or roster.

**Life Groups keeps its own entry point** (More → Life Groups, the home rows and the Hub tile), as a
view of the ministries of kind Life Group. The directory of ministries has a Life Groups section that
links to it, rather than listing the classes. Why: Life Groups are chosen together, once a round, as a
set of classes at the same hour (you pick one of five); they change every eight weeks; and their
page is organised around the round, the room and the map. Mixed into the directory, five rotating
classes would bury the ministries that don't change, and a member choosing a class would lose the
side-by-side choice. As ministries underneath, they share everything that isn't seasonal.

## What they are

Life Groups are the church's Sunday school: classes on Sunday from 9:00 to 9:45, before the 10:30
service. The time is set per round in the admin panel; 9:00–9:45 is the default. Adult groups run in **rounds**: six Sundays on, two off, with a new set of classes, leaders
and rooms each round. **Nursery (0–3), Kids Life (Prep–Yr 6) and Youth (Yr 7–12) run all year** with
the same leaders.

For example, Round 2, 2026 ran 13 Sep – 18 Oct. Its five adult groups each had a title, a short blurb,
one or two leaders with a photo, a colour and a numbered room on the map. 25 Oct and 1 Nov are the
break. After it, Summer FIT (8 – 29 Nov) combines all adult classes into one four-week study, so a
round can have a single group, and not every round is six weeks.

Today that's a printed flyer and a room map that a designer redraws every round, and a teacher who is
sick has no way to tell their class. The app replaces the flyer and the map and gives each group a
notice channel.

## What people can do

- **Browse** this round's groups, and the next round's during the break, plus the year-round
  groups: title, leaders, blurb, who it's for ("for ladies", "Yr 7–12") and where it meets.
- **Find the room:** every group shows its room on the site map, highlighted in the group's colour.
- **Join** a group. Joining is per phone, needs no account, and turns on that group's notices.
- **Parents join Nursery, Kids Life or Youth** to hear about their children's class: notices,
  events and what's being taught. The button says "Join as a parent" where that's the only reason
  to join.
- **Read the group's content:** notices from the leader and resources such as notes, readings,
  links and files, newest first, kept for the round.
- **Get notified** when the leader posts: "I'm sick, we're combining with Jesus: God & Man in the
  Library", "no class this week", "bring your Bible". Urgent notices are visible pushes; others
  update the group quietly.
- **See it on Home** on a Sunday: "Your Life Group, 9:00, Library (5)", or "Cancelled this week".

Not now: messages, whether group chat or replies to a leader. The design leaves room for them (below).

## How rounds work

Staff enter each round rather than the app computing it, because breaks move around Easter and
Christmas.

- **A round** has a name ("Round 3, 2026"), its Sundays (pre-filled six from a start date, editable
  to skip one or shorten it), a time (default 9:00–9:45) and a publish date. A round appears in the app once
  published, usually during the break before it.
- **A group** is a ministry of kind Life Group that belongs to a round, or to no round if it runs all
  year. Its own part is the room (a place on the site map) and a map number that defaults to its
  order; the title, blurb, leaders (name, photo, optional role), audience and colour are the
  ministry's.
- **A session** is one group on one Sunday, shown as an entry in the group's calendar. It's generated
  from the round and only stored when something differs: cancelled, a different room, combined with
  another group, or a substitute leader. Those are structured fields, not just text, so the map,
  Home and the group page all show the change, and a notice can be attached.

**Between rounds,** the app shows the next round as soon as it's published ("Summer FIT starts
8 Nov") with Join already open. Joining a round's group lasts until that round ends. When the next
round is published, everyone who was in a group gets one "Choose your group for Round 3" push, sent
to the Life Groups topic, not per group. Memberships of year-round groups don't expire.

## Joining, without accounts

The SPEC keeps the app account-free (§3, §6), and joining fits that:

- **Join is an App write** (§6): an attested call that records `(install, group)` and subscribes the
  install to the group's push topic. Leave reverses it. Both are rate-limited per install.
- **The phone remembers its own memberships** and shows "Your groups" first. The server holds the same
  pairs only to fan out pushes and give leaders a count.
- **Leaders see a number, not names** ("14 joined"), decided 2026-10-26. A name would make membership
  personal data, and messages would need it anyway, so that waits for messages.
- **Phones that can't attest** (Android without Play services, §6) can still join locally: the group
  is pinned and its notices show in the app, but no push arrives. The app says so at Join.
- **A Sunday reminder** ("Life Groups at 9, Library (5)") is an opt-in toggle per membership, off by
  default, sent by the server at 8:30 church time (§9 Time).

## Who posts

Group leaders aren't necessarily staff, and they post on a Sunday morning from their phone. **Leaders
work in the app, not the admin panel** (decided 2026-10-26): the admin panel is for staff setting up
rounds, groups and rooms.

- **A "Life Group leader" role,** invited like staff and signed in with a passkey **inside the app**.
  A leader can only act on their own groups: post notices and resources, mark a session cancelled,
  moved or combined, and see their member count. Staff can do everything, including rounds and rooms.
- **This is the app's first sign-in** (SPEC §3 allowed it for things that are personal). It is
  staff-tier sign-in, not member accounts: reading and joining stay account-free.
- **Passkeys in the app** can't use the browser's WebAuthn API: the app runs from `capacitor://`, which
  can't be a relying party. The native layer uses the platform passkey APIs (iOS
  `ASAuthorizationPlatformPublicKeyCredentialProvider`, Android Credential Manager), with the admin
  site's domain as the relying party via Associated Domains and Digital Asset Links. The same passkey
  then works in the app and the admin panel. The ceremonies use the gateway's passkey endpoints;
  the owner approved the app calling them on 2026-10-26 ([AGENTS.md](../../AGENTS.md)).
- **The session lives in the native layer** (Keychain / Keystore), and Transport attaches the
  short-lived token to Admin-tier calls (§5, §6). It is still the Admin tier; the new part is
  **per-group authorisation inside it**, checked in the API against the token's user, with a test that
  a leader's token can't touch another group.
- **Posting** is a title, a body and an "urgent" switch. Urgent sends a visible push to the group's
  members and is limited to a few per group per day. Non-urgent posts only sync.

## Content and privacy

- **Everything in a group is public-tier** (§6): anyone can browse any group and read its notices
  and resources. That matches the paper flyer and keeps sync simple. Leaders are told plainly that a
  notice is public, so no "pray for Jane's surgery" in a notice.
- **Leader photos** are adults' photos with their consent, managed by staff. Kids and Youth groups show
  leaders, never children (§11).
- **Files** (notes, PDFs) are media like photos: staff-uploaded, size-limited and cached on the phone.
  How media travels is an open question in the SPEC (§12), and it must be gRPC unless the owner
  approves otherwise ([AGENTS.md](../../AGENTS.md)).

## Data and feeds

A feature module `Features/LifeGroups` with all four parts (§8). Feeds (§5, §10):

| Feed | Records | Window | Index columns |
|---|---|---|---|
| `lifegroups.rounds` | Rounds | Previous, current and next | `starts_at`, `ends_at` |
| `lifegroups.groups` | Groups and their leaders | Groups of those rounds, plus year-round ones | `group_key` = round, `sort_key` = order |
| `lifegroups.sessions` | Session exceptions | Current and next round | `group_key` = group, `starts_at` |
| `ministries.items` | Notices (announcements) and resources (files) | Current and previous round | `group_key` = group, `sort_key` = posted |

Services:

- Joining and leaving use the ministries' membership service (App write); `ILifeGroupsService` keeps
  only `SetReminder`. The client stores the result locally.
- `ILifeGroupsAdminService` (Admin): rounds, rooms and sessions. Posting notices and resources is the
  ministries' admin service, scoped to the leader's groups.

Posting saves the post and its feed change in one transaction (§5); for an urgent post, the push is
sent after commit.

## Screens (app)

They reuse the spike's shared components (`Page`, `Masthead`, `ListGroup`/`ListRow`, `Section`,
`Sheet`, `ChipGroup`, `EmptyState`, `ToggleRow`) before adding any; the new ones are generic.

1. **Life Groups:** a header with the round ("Round 2 · 13 Sep – 18 Oct · week 5 of 6") or the
   break ("Back 8 Nov · Summer FIT"). Then "Your groups", "All year" (Nursery, Kids Life, Youth) and "This round",
   each a **group card**: colour band, title, leader photos and names, the first line of the blurb, and
   a room chip with the map number. "View map" opens the round's map with every group's room.
2. **Group:** colour header, leaders, the full blurb, "Where": a small map with this room highlighted.
   **Find the room** opens a full-screen map of just this group's room (this Sunday's, if it moved),
   with the main buildings labelled around it for bearings. The Life Groups page's Map button is the
   overview: every room this round, numbered like the flyer. The group page also has this round's
   Sundays with any changes, **Join/Leave** and the reminder toggle, then notices and resources.
3. **Home card** (through the Shared home-card interface, §8): your group this Sunday, or a nudge to
   choose one during the break.
4. **Leader view** (signed-in leaders only): my groups, post a notice, change this Sunday.

In the design spike, the group page is built from the shared ministry components (`BandHero`,
`MinistryAbout`, `EventList` for the Sundays, `PostList` for notices, `FileList` for resources and
`MembershipSheet` for Joined), with the round, the time and the room as Life Groups' own sections.
Like a ministry page (2026-10-31, [ministries.md](ministries.md#a-view-per-area)), it's split into
areas instead of one long scroll, each a tab with its own URL: **About** (`/groups/{id}`: this
Sunday's time or change, what the group is, its leaders and the room on the map), **Sundays**
(`/sundays`), **Notices** (`/notices`) and **Resources** (`/resources`). Notices and Resources only
appear when the group has some. The first view answers "when and where this Sunday" without scrolling,
and a notice about a moved room can link straight to the group's About.
Everything on a Life Group is public (its Notices, Sundays and Resources are set to Everyone): anyone
can walk into a class, and its notices are logistics a newcomer needs as much as a member. So the
page has no Team only and Public streams; see
[Who sees what](ministries.md#who-sees-what-per-ministry-per-module).

The design spike (`spikes/app-poc`) has screens 1–3 with Round 2's real groups, the site map and
sample notices, for showing staff. Its README says what's real and what's sample.

New shared components: a **colour-band card** (also suits events and sermon series), a **person chip**
(photo and name; also staff on About), and the **site map** with its place chip ([site-map.md](site-map.md)).

## Admin

- **Rounds:** create from a start date, edit Sundays, publish. "Copy from last round" carries over
  rooms and the year-round groups.
- **Groups:** title, blurb, audience, colour (from a fixed palette that reads on the map in light and
  dark), leaders (pick a person, upload a photo), room (pick on the map).
- **Preview:** the round as the app will show it, map included, so the printed flyer can be
  checked against the same data. A printable flyer generated from it is a later nicety.

## Later: messages

Messages would need a person, not an install: a name, sign-in or a verified phone, moderation and
safeguarding for groups that include youth. Kept out of v1. Ministries have chats
([ministries.md](ministries.md)); a Life Group could switch the Chats module on once member sign-in
and the safeguarding rules exist.

## Decided

- **A kind of ministry:** Life Groups share the ministries' model and modules, with rounds, rooms and
  the Sunday time as their own (owner, 2026-10-30). Life Groups keeps its own entry point.
- **Time:** 9:00–9:45 by default, set per round in the admin panel (2026-10-26).
- **Leaders:** sign in with a passkey in the app and do their work there (2026-10-26).
- **Membership:** leaders see a count, not names, for now (2026-10-26).
- **Nursery, Kids Life and Youth:** parents can join to get notices and info about their children's
  class (2026-10-26).

## Open questions

0. **An all-year group off for a set: its own calendar events.** Should its calendars' events on that set's Sundays show
   as not on too? They're events staff add by hand, so it needs a rule for which ones (for example, every date of its
   regular events that falls on one of the set's Sundays). Recommendation: yes, for regular events on those Sundays only.

1. **Room names:** the map only numbers rooms. What are they called (for example, the Library's east
   wing)? The spike uses placeholders.
2. **Leaders on the flyer without a public photo** (Garry Tingson, Michelle Hills) show initials in the
   spike; photos would come from staff, with consent.
3. **Peter George Jnr's photo:** the spike uses the website's "Pete George" (deacon) photo, assuming
   that is Peter George Jnr. Confirm.
