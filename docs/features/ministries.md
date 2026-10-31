# Ministries

Status: draft, 2026-10-30; areas separated into tabs 2026-10-31; who sees each module chosen per
ministry 2026-10-31. Extends [SPEC.md](../../SPEC.md), where v1's "Groups" line ("ministries,
each with a contact or 'I'm interested' button") was the placeholder. A POC is in the PWA spike
(`spikes/app-poc`, More → Ministries and a Hub tile). Everything in it is sample data.

## What it is

The owner's brief (2026-10-30): every ministry has leaders and members, and can have an info page, a
calendar, announcements and messaging (several chats, made by leaders, or by members if enabled) and
rosters, each switched on or off per ministry. Everyone on the app can see every ministry and what it's
about, in case they want to join. Most ministries only let non-members message the leaders about
joining; some (Young Adults, Men's) are open: tap Join and see what's on. A ministry may want a public
side and a private side: Kids wants to talk among its team privately but keep a public calendar and
announcements. Ministries are tiered: Music is over Vocal Groups, Choir, Song Leading and Production.
Documents matter (a code of conduct), and so do Dropbox links to practice recordings for Music. Staff
want to send important messages to every serving team, but not to fellowship groups such as Men's.

A second decision the same day: Life Groups are a kind of ministry ("they have a lot of overlap, so
more abstract and merged"). See [Life Groups are ministries](#life-groups-are-ministries).

## The ministries

From the church website's Ministries page (baptist.com.au/ministries), its Our Team page and the
owner. The spike uses these names; descriptions marked "sample" are written for the spike.

| Ministry | Source | In the spike |
|---|---|---|
| Children's Ministry | Website: "Children ages 4 to 11." | Serving team, ask to join; its Friday night is Impact Kids (from the bulletin) |
| Impact Youth | Website: the full description, Fridays 6:30–8:30pm in school terms | Serving team, ask to join |
| Young Adults | Website: "Men & Women 18 or older." | Fellowship, open |
| Music, over Vocal Groups, Choir, Song Leading and Production | The owner | Serving team and its four teams |
| Men's | The owner | Fellowship, open; Men's Prayer Fridays 5am (bulletin) |
| Ladies | The website's Our Team page | Fellowship, open; Ladies' Prayer Fridays 9:30am (bulletin) |
| Southland Baptist Productions | The website's Our Team page (a staff role, no description) | Serving team, ask to join; sample description |
| The Life Groups | The printed flyer ([life-groups.md](life-groups.md)) | Ministries of kind Life Group |

## What people can do

- **Anyone (a visitor):** browse every ministry, grouped as serving teams (with Music's four teams
  under it) and fellowship groups, each with what it offers and how to join. Open a ministry to read
  its About (description, when and where it meets, who it's for, its leaders) and how to join. For
  most serving teams that's all: their news, calendar and files are for members. Where a ministry
  has made a module public (Children's Ministry's news and calendar, Young Adults' calendar), the
  visitor sees its public stream in its own tab. Then, by the ministry's join setting:
  - **Open:** tap Join; the members-only side opens at once.
  - **Ask:** "Ask to join" sends a name, a phone number or email, and a message to the leaders, who
    accept or decline the request.
  - **Invite only:** "Message the leaders"; leaders add members themselves (Song Leading: you're asked
    after serving in Choir or Vocal Groups).
- **A member:** everything public plus the team's announcements, events and files, the chats,
  and the roster: "You're on Sunday · Choir item", with Accept, Decline or Swap. Music's roster shows
  the songs for each service with the practice recording (it plays in the app), the Dropbox link and
  the score. A notifications switch per ministry, and Leave.
- **A leader:** post an announcement, an event or a link, each marked **Everyone** or **Members
  only**; start chats, including leaders-post-only ones; assign people to roster positions; and, in
  Settings, switch modules on and off, change the join setting, let members start chats, accept join
  requests and see the members.
- **Staff:** everything a leader can do in every ministry, plus **Message all serving teams**: one
  important announcement, with a notification, to the members of every serving team and their
  sub-teams. Whether a ministry is a serving team is a staff-only setting.

"My ministries" is the top of the directory: your next roster date, then each ministry you're in with
its latest announcement. Home has it as a Hub tile ("You're on Sunday · Choir item · Morning service,
10:30am", or "Your ministries", or "Ministries: find where you fit"), a Today block and a Simple row.

## The model

- **Ministry:** a **kind** (Ministry or Life Group), name, a one-line summary, About paragraphs, meets (free text), who it's for, leaders,
  colour and icon, an optional **parent**, a **join setting** (Open, Ask, Invite only), a **serving
  team** flag, and its **modules**: Calendar, Announcements, Chats, Rosters, Files. A ministry with
  children (Children's Ministry, Impact Youth) can't switch Chats on yet (below).
- **Tiers:** one level, parent and child. A parent's leaders lead its children. A parent's
  announcements, events, files and chats show on each child, labelled "from Music". A child's member
  is a member of the parent for reading (a Choir member sees Music's members-only items and the Music
  team chat), but holds no role in the other children. A parent's roster holds every child's
  positions; each child shows its own.
- **Who sees each module** is a per-ministry setting a leader chooses for Announcements, Calendar and
  Files: **Members only**, **Everyone**, or **Both** (a public stream and a team stream). About is
  always public; Rosters and Chats are always members only. See
  [Who sees what](#who-sees-what-per-ministry-per-module).
- **Items** (announcements, events, files and links) each carry an **audience**: Everyone or Members,
  which must be one the module allows. A module set to Members only hides its earlier public items
  until it's changed back; a module set to Everyone never makes earlier members-only items public.
  An announcement can be **Important**, which sends a notification.
- **Chats** belong to a ministry, have a name, and may be **leaders post only** (announcement-style:
  members read and are notified but can't reply). Leaders start chats; a per-ministry setting lets
  members start them too.
- **Rosters** are dates (a service or event) with positions, the people on each and each person's
  reply, plus for Music the songs with their practice files. They come from Elvanto (below).

## Life Groups are ministries

A Life Group is a ministry of kind **Life Group** (owner, 2026-10-30). It shares the model and the
modules: About, leaders, Join and Leave, the notifications switch, notices (Announcements), resources
(Files) and its Sundays (Calendar, one entry per Sunday, a change shown on its entry). What's only
for Life Groups is a small part on the ministry: the room and its map number, "join as a parent",
plus the rounds, the 9:00 time, session changes and the Sunday reminder around it. A Life Group is
always open, all its items are for Everyone, and it has no chats or roster yet.

**Life Groups keeps its own entry point** (More → Life Groups, the home rows, the Hub tile): a view of
the ministries of kind Life Group, organised by round. The directory has a **Life Groups** section
with your group (or the next round) and a link, not the classes themselves. Why:

- Life Groups are chosen as a set, once a round: one of five classes at the same hour. The Life
  Groups page puts them side by side with their rooms and the map, which is how the choice is made.
- They change every eight weeks. Listed in the directory, five rotating classes would crowd out the
  ministries that don't change, and "Yours" would churn.
- What a Life Group page is about (this Sunday, the room, the round's Sundays) is seasonal and
  place-based; what it shares with ministries (leaders, notices, resources, joining) is now shared.

Same components, two pages: `MinistryAbout`, `PostList`, `FileList`, `EventList`, `BandHero`,
`MembershipSheet` and the paged `SectionTabs` render both a ministry and a Life Group. A Life Group's
areas are About (this Sunday's time, the About, the room), Sundays, Notices and Resources, at
`/groups/{id}`, `/groups/{id}/sundays`, `/notices` and `/resources`.

Overlaps to settle (open questions below): Kids Life (Sunday, Prep – Year 6) and Children's Ministry
(Friday, ages 4 – 11) have overlapping leaders and families; so do Youth and Impact Youth. A Life
Group could sit under a ministry as a child, but in this model a child's member is a member of the
parent, so a parent joining Kids Life would see Children's Ministry's team notices. That needs a
"followers" kind of membership first, so the spike keeps them separate.

## A view per area

The owner, 2026-10-31, on the first POC: "I don't know if I like the infinite scroll for all the
content under a ministry (keep scrolling down and you go into other areas like roster, chats, etc.).
Feels like properly separated might be nicer." So a ministry page is now a set of **areas**, each its
own view under the hero, reached by the section tabs and each with its own URL:

| Area | URL | Shown when |
|---|---|---|
| About | `/ministries/{id}` | Always. What it is, when it meets, who leads it, its teams; for a member, your next roster date and what's next |
| News | `/ministries/{id}/news` (and `/news/public`) | Announcements is on, and there's something you may read (leaders always see it) |
| Calendar | `/ministries/{id}/calendar` (and `/calendar/public`) | Calendar is on, likewise |
| Roster | `/ministries/{id}/roster` | Rosters is on and you're a member |
| Chats | `/ministries/{id}/chats` | Chats is on, allowed (not with children yet) and you're a member |
| Files | `/ministries/{id}/files` (and `/files/public`) | Files is on, likewise |

A module that's off has no tab, and a visitor sees only tabs with something public in them: for Music
that's none, so a visitor gets About and Ask to join, with no tab bar at all. A tab that's members
only carries a lock. The `/public` URLs are the public stream of a module set to Both. The first view is
deliberately short: the hero says what kind of ministry it is and carries the one action (Join, Ask to
join, Message the leaders, or Member); About then answers "what is this" for a visitor and "what's next
for me" for a member (your next date with Accept, Decline and Swap, then the next thing on the
calendar). Everything else is one tab away instead of a long scroll down.

Deep links land on the area: the Hub tile and Today's "You're on Sunday" open the Roster, a chat's
back button returns to Chats, and a notification carries the area's URL ("Music: you're on Sunday"
opens `/ministries/music/roster`; a team notice opens `/ministries/music/news`, and a public one on a
module set to Both opens its `/public` stream).

`SectionTabs` gained a page mode for this (the selected tab, a URL per tab, and "locked" tabs shown
with a lock), so the ministry page and the Life Group page share the Sunday tab's
sliding underline. Switching tabs replaces the history entry, so Back leaves the ministry rather than
stepping through its tabs. The roster, which was its own page, is now the Roster area (`RosterView`).

## Who sees what: per ministry, per module

The owner, 2026-10-31, on the tabs rework: "you're assuming too much stuff is public. For most service
ministries, only the About is public. Some ministries, e.g. Impact Kids, will have some public news and
calendar, but they'll also need a private calendar and private news."

So "who sees it" is no longer only a choice per post. Each ministry's leaders choose it **per module**
in Settings → Who sees what:

| Setting | A visitor sees | A member sees | A leader can post |
|---|---|---|---|
| **Members only** | No tab | One stream, marked Team only | For members only |
| **Everyone** | One public stream | The same stream, marked Public | For everyone |
| **Both** | The public stream only | Two streams in the tab: Team only and Public | Either; it starts on the stream they're looking at |

About is always public (what it is, when it meets, who leads it, how to join). Rosters and Chats are
always members only. The defaults, which leaders can change:

| Ministry | News | Calendar | Files | Why |
|---|---|---|---|---|
| Serving teams: Music and its four teams, Southland Baptist Productions | Members only | Members only | Members only | The owner's rule: for most service ministries only About is public. A visitor sees what the team does and how to join, nothing else |
| Children's Ministry (Impact Kids) | Both | Both | Members only | Parents need the Friday nights, the break-up night and "this Friday: space night"; helpers need their own briefings, planning and training dates. Its files (the helpers' guide, the code of conduct) are for the team |
| Impact Youth | Both | Both | Both | The same reasons as Kids, plus a public file families need (the camp permission form) beside the leaders' room lists. A guess, to confirm with its leader |
| Young Adults | Both | Everyone | Everyone | An open group: what's on is public, but the group has its own notices too ("camp carpool") |
| Men's, Ladies | Everyone | Everyone | Everyone | Open fellowship groups whose news is mostly "come along" |
| Life Groups | Everyone | Everyone | Everyone | See below |

**Life Groups are public.** A Life Group is a class anyone can walk into at 9:00; choosing one is done
by looking at them side by side, and joining needs no approval. What a group posts is logistics
(a moved room, no class this week, this week's notes) that a visitor deciding to come needs as much as
a member. So all three modules default to Everyone and the group page has no streams. When member
sign-in exists, a leader could set Notices to Both for things like prayer points.

### Where a module is Both: two streams in one tab

The owner has said all along that viewing should feel split. With most modules now wholly members only,
the question narrows to the modules set to Both, such as Kids' News and Calendar. Three ways were
considered:

- **A public tab and a team tab per module** (News, Team news, Calendar, Team calendar …): every
  stream gets a place, but a ministry with two modules on Both has eight or nine tabs, and the pairs
  sit apart in the tab bar.
- **One tab, both streams stacked** (a Team only part, then a Public part): the long scroll the owner
  asked to get rid of, back inside one tab.
- **One tab, a switch between the streams** (chosen): News has a two-way switch at the top, **Team
  only** and **Public**, and shows one stream at a time. Each stream has its own URL
  (`/ministries/children/news` and `/news/public`).

Why the switch: the streams are separate views, so the split is a place, not a tag on an item; the
switch says which one you're in, and under it a band says who reads it ("Only Children's Ministry
members see this" on a navy-tinted ground, "Anyone with the app sees this, including visitors" on
sand). The tab bar stays short. Team only comes first because members open a ministry for the team's
things; a notification for a public item opens the Public stream. A visitor never sees the switch:
they get the public stream and nothing else.

**Posting follows the stream.** A leader's Post starts on the audience of what they're looking at: Team
only (members only) from a team stream, About or a members-only tab, and Everyone only from a public
stream or a module set to Everyone. The sheet offers only what the module allows: on a module set to
Both it shows the two-way **Who sees this** switch; otherwise it states the one audience ("In Music,
announcements are for members only. Leaders change this in Settings."). The sheet's sentence says
exactly who will read it, and the button repeats it: **Post for members only** or **Post for
everyone**. Kinds of post whose module is off aren't offered.

### The three earlier designs, dropped

The spike had a switch between three ways of keeping team things apart (Team tabs, Team room, Marked).
Rechecked against per-module audiences, two no longer make sense and the third has become the design
above, so the switch is gone (the eye button still sets who you're viewing as):

- **Team tabs** (the team's notices, dates and files in one Team tab after a lock) assumed News,
  Calendar and Files were public. With them members only by default, Music would have had public tabs
  with nothing in them and everything in Team. What's left of it is the lock on a members-only tab.
- **Team room** (a separate team page) made the public page the front door. With only About public,
  the front door is About, and the room would be the whole ministry one tap further away; for Kids it
  meant two tab bars with News and Calendar in each.
- **Marked** (team and public items mixed in one list, team items tagged) is the opposite of what the
  owner asked for. Tags survive only on the About's "Next up" and on leftovers in a module set to
  Everyone.

### Common rules

- Every chat starts with "Only Music members can read this chat. Messages are kept."
- Roster and chats are always members only; there's no public roster or public chat.
- Staff's "Message all serving teams" is always members only.

### To decide with staff

Have a Kids leader post one thing to each stream, then ask a helper and a parent what they saw. The
defaults per ministry are guesses from the brief; each ministry's leaders should confirm theirs.

## Rosters and Elvanto

Elvanto stays the source of truth, and stays usable directly (owner, 2026-10-30). The app is a
front end to it, never a second roster.

| In the app | In Elvanto |
|---|---|
| A ministry with a roster | A department (Music, Children's Ministry); a child ministry is a sub-department (Choir, Production) |
| A position (Song leader, Sound) | A position in that sub-department |
| A roster date | A service (or event) and its time |
| You're on | Your roster request for that service and position |
| Accept / Decline | Accepting or declining the request in Elvanto |
| Swap | To confirm: Elvanto has no swap that we know of; the POC treats it as "still on, leaders asked to find a replacement", which may become a decline with a note |
| A leader assigning | Adding a volunteer to the position, which makes Elvanto send its request |
| Songs and practice files | The service plan's songs, with their arrangements, keys and attached files or links |

How it would work: the API reads Elvanto on a schedule and on demand, and writes leaders' and members'
changes back, all as **outbound** calls from the server with a church API key, which the AGENTS.md
hard rule allows. Elvanto pushing changes to us (a webhook) would be a non-gRPC path and needs the
owner's approval first; polling avoids it. The app reads rosters through a gRPC service like any
other feed. People must be matched to Elvanto people, which needs member sign-in (below). The spike
shows "From Elvanto · synced 4 min ago (sample)" on the roster.

## Chats

Several chats per ministry; leaders create them; a setting lets members create them; a chat can be
leaders post only. Messages are kept. In the spike chats are local and in memory: nothing is sent.

## Safeguarding, for later

Children's Ministry and Impact Youth have no chats in this pass, and the setting says so ("Not for ministries
with children yet. Safeguarding rules come first."). Before any chat that includes young people or
concerns children, the church's Safe Ministry policy needs these in the design:

- **Staff can read any youth or kids chat**, at any time, and the chat says so to its members.
- **No private messages between an adult and a minor**: no one-to-one chats where either is under 18;
  group chats only, with at least two screened adults in each.
- **Messages are kept** (no deleting, no disappearing messages) for a period set by the church.
- **Report a message**: anyone can report a message to staff, who see it in context.
- Leaders of these ministries hold a current Working with Children (blue) card; membership of the
  team requires it.
- Young people join with a parent's consent; parents may see the chats their child is in.

## Notifications

Each ministry is a push topic. Members have a per-ministry switch (in the spike it's a mock: nothing
is sent). Important announcements and new messages in your chats notify; everything else syncs
quietly. Staff messages to all serving teams are always important.

## Accounts

Ministries are the first feature that needs a **person**, not an install: membership, a name in a
chat, a roster match to Elvanto, a leader's role. The SPEC keeps reading account-free (§3), and that
still holds for the directory and every public item. Members-only content, chats and rosters need
member sign-in; Life Groups' leader sign-in (passkeys in the app) is the likely start. That design is
out of scope here; the spike has a "Viewing as" switch (Visitor, Member, Leader, Staff) instead.

## Data and services

A feature module `Features/Ministries` with all four parts (SPEC §8). Feeds (§5): `ministries` (the
directory, public), `ministries.items` (announcements, events, files; public items in a public feed,
members' items in a per-person feed), `ministries.chats` and `ministries.rosters` (per person). Every
service method is gRPC: `IMinistriesService` (public read), `IMinistryMembershipService` (join, ask,
leave, notifications), `IMinistryChatService`, `IMinistryRosterService` (reply, swap, assign; writes
through to Elvanto), and `IMinistriesAdminService` (posting, settings, staff sends). Authorisation is
per ministry, as for Life Group leaders: a leader's token can't touch another ministry, with a test.

## In the spike

- `Data/Ministries/`: the model, the sample content (`MinistriesContent.cs`) and `MinistriesService`
  (viewer role, design, membership, posting, chats, rosters, settings; joining is stored on the phone,
  the rest is in memory).
- `Pages/Ministries/`: the directory (`/ministries`), a ministry and its areas (`/ministries/{id}` and
  `/ministries/{id}/{area}` and `/{area}/public`, `MinistryArea.cs`), chats
  and settings, and the shared lists (posts, events, files, chats, the roster, your roster dates).
- Shared components added or extended: `BandHero` (Life Group's colour header, now shared),
  `HomeLineRow` (Life Groups' home row, now shared), `MinistryAbout` and `MembershipSheet` (used by
  ministries and Life Groups), `Toast`, and `ListRow`'s `Class` and `Below`.
- Life Groups (`Data/LifeGroups.cs`) are `Ministry` records of kind Life Group; their notices,
  resources and Sundays are generated into the shared posts, files and calendar. Joining a Life Group
  is ministry membership (a phone that joined before the merge keeps its groups).
- Ministry names are the church's (above). Children's Ministry, Impact Youth and Young Adults use the
  website's words; other descriptions, and people, dates, chats and files, are samples; every person
  is a placeholder ("Alex Sample"). Friday's Men's Prayer, Ladies' Prayer, Impact Kids and Impact
  Youth times are real.
- The practice recording is a synthesised rendition of the tune New Britain (public domain), made for
  the spike, so the app's audio player can be tried with a song.

## Decided

- **Posting:** every event, announcement and file is marked Everyone or Members (owner, 2026-10-30).
- **Visibility:** for most serving teams only About is public; ministries such as Impact Kids need
  public and private news and calendar (owner, 2026-10-31). Hence the per-module setting.
- **Rosters:** Elvanto is the source of truth; leaders roster through the app into Elvanto; members
  read from it; Elvanto stays usable directly (owner, 2026-10-30).
- **Chats:** several per ministry; leaders create; members can if enabled; leaders-post-only chats.
  None for Kids or Youth yet (owner, 2026-10-30).
- **Auth:** out of scope for the POC (owner, 2026-10-30).

## Open questions

1. **Defaults per ministry.** Which modules each ministry makes public or Both; the spike's table is a
   guess, to confirm with each ministry's leaders.
2. **Which ministries, under whom?** Parents and children, join settings, and which are serving
   teams. The spike guesses all but Music's tier.
3. **Southland Baptist Productions:** what it does, and whether it's Music's Production team, sits
   beside it, or isn't a ministry members join.
4. **Life Groups and the Friday ministries:** should Kids Life sit under Children's Ministry and Youth
   under Impact Youth (needs "followers", above)? Is the Ladies ministry the same group as a ladies'
   Life Group such as Adorning The Heart, and Young Adult LIFE (on Our Team) a Life Group of Young
   Adults?
5. **Member sign-in.** How a member proves who they are, and how that person is matched to Elvanto.
6. **Elvanto:** does its API allow writing roster replies and assignments, and is there a swap or
   replacement request? Rate limits, and whether the church's plan includes API access.
7. **Leaving and rosters:** does leaving a ministry in the app remove you from future Elvanto rosters,
   or only stop notifications?
8. **Parents:** for Children's Ministry, are parents members (they get team notices) or followers of the
   public side? The spike treats the team as leaders and helpers.
9. **Files:** documents are media, so they travel by gRPC (SPEC §12). Dropbox links stay links.
10. **Replies to "Ask to join"** reach the person by phone or email outside the app. Is that enough?
