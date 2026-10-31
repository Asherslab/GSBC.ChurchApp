# Ministries

Status: draft, 2026-10-30; areas separated into tabs 2026-10-31. Extends [SPEC.md](../../SPEC.md), where v1's "Groups" line ("ministries,
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
  its About (description, when and where it meets, who it's for, its leaders), then its public
  announcements, calendar and files, each in its own tab. Then, by the ministry's join setting:
  - **Open:** tap Join; the members-only side opens at once.
  - **Ask:** "Ask to join" sends a name, a phone number or email, and a message to the leaders, who
    accept or decline the request.
  - **Invite only:** "Message the leaders"; leaders add members themselves (Song Leading: you're asked
    after serving in Choir or Vocal Groups).
- **A member:** everything public plus the members-only announcements, events and files, the chats,
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
- **Items** (announcements, events, files and links) each carry an **audience**: Everyone or Members.
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
| News | `/ministries/{id}/news` | Announcements is on (and there's something to read, unless you lead it) |
| Calendar | `/ministries/{id}/calendar` | Calendar is on, likewise |
| Files | `/ministries/{id}/files` | Files is on, likewise |
| Team | `/ministries/{id}/members` | Team tabs design: you're a member; the team's own notices, dates and files |
| Roster | `/ministries/{id}/roster` | Rosters is on and you're a member |
| Chats | `/ministries/{id}/chats` | Chats is on, allowed (not with children yet) and you're a member |

A module that's off has no tab, and a visitor never sees the members-only ones. The first view is
deliberately short: the hero says what kind of ministry it is and carries the one action (Join, Ask to
join, Message the leaders, or Member); About then answers "what is this" for a visitor and "what's next
for me" for a member (your next date with Accept, Decline and Swap, then the next thing on the
calendar). Everything else is one tab away instead of a long scroll down.

Deep links land on the area: the Hub tile and Today's "You're on Sunday" open the Roster, a chat's
back button returns to Chats, and a notification carries the area's URL ("Music: you're on Sunday"
opens `/ministries/music/roster`; a team notice opens `/ministries/music/members`). The URLs are the
same in every design: in Team room a team area redirects into the room, and a room URL redirects to
the matching tab in the other two, so links in notifications don't depend on which design is chosen.

`SectionTabs` gained a page mode for this (the selected tab, a URL per tab, and "locked" tabs shown
with a lock), so the ministry page, the team room and the Life Group page share the Sunday tab's
sliding underline. Switching tabs replaces the history entry, so Back leaves the ministry rather than
stepping through its tabs. The roster, which was its own page, is now the Roster area (`RosterView`).

## Public and team-only: three designs

This is the central design question. The sending side is settled: everything a leader posts is marked
Everyone or Members. The question is how the receiving side keeps them apart. Three people look at a
ministry, for different reasons:

- **A visitor** is deciding whether to join. They need the About, a sense of what's on, and the way
  in. Team-only things are noise to them, but knowing they exist ("members get the roster and chats")
  is a reason to join.
- **A member** came to do something: check whether they're on, reply, find the practice recording,
  read the chat. They need the team things first, and the public things only as part of the picture.
- **A leader** is about to say something. The risk is saying a team thing in public ("pray for Jane's
  surgery", a child's name). They need to know, while writing, exactly who will read it.

And in a chat or a notice, a member needs to know who else can see it before they reply. With areas
separated, the question "does the team room still need to be its own place?" has a new answer: a tab
is already a place. So the old Two zones design (one long page with a shaded team band) becomes **Team
tabs**, and the three designs are re-cut around the tab bar. Switch them with the eye button on any
ministry page, or under This preview at the foot of the directory.

### Team tabs (recommended)

One page, one tab bar. The public areas come first (About, News, Calendar, Files) and show only what
everyone sees. A member then sees a divider with a lock, and the team's areas after it: **Team** (team
notices, team dates, team files), **Roster** and **Chats**. Each area opens with a one-line band that
says who sees it: "For everyone · anyone with the app sees this" or "Team only · only members of Music
and its teams see this". A leader's Post starts on the audience of the tab they're on. A visitor sees
only the public tabs, with "Members see more" on About.

- **For:** the split is still a place (you're on a team tab or you aren't), and now it's visible all
  the time in the tab bar rather than only after scrolling, with the band repeating it in words. A
  member's public tabs are exactly what a visitor sees, so a member always knows what the public sees.
  There's one page and one tab bar, Calendar and Files aren't in two places, and deep links land
  straight in the area with no extra hop. The default audience follows the tab, so the easy path is
  the safe one. It fits Kids: a public calendar on the Calendar tab, the team's notices on Team.
- **Against:** "what's new?" means two tabs (News and Team), as Two zones meant two parts; About's
  "Next up" and notifications soften it. A ministry with every module on has seven tabs, which scroll
  sideways on a phone (Music, for a leader); the active tab always scrolls into view.

### Team room

The ministry page is the **public front door**, identical for everyone, with the public tabs. Members
see one extra thing on About: a dark **team room** card ("Music team room · You're on 1 Nov · 1 team
notice · 2 chats"). The team room is its own page (`/ministries/{id}/team`) with a deeper header, a
"Team room · only Music members see this" badge, and its own tabs: Notices, Roster, Chats, Calendar,
Files, holding team items and public ones, the public ones tagged **Everyone**. A leader posting from
the front door starts on Everyone; from the room, on Members.

- **For:** the strongest separation: the whole screen changes, header and all, so you can't mistake
  where you are. Inside the room the unusual thing (public) is the one tagged, which is the right way
  round for a member.
- **Against:** with areas separated, most of what the room bought is now bought by the tabs, and what's
  left costs: two tab bars, Calendar and Files in both places, a hop for members (Home and
  notifications link into the room, but browsing doesn't), and the team card only on About.

### Marked

One page, one tab per kind, in the order of the brief: About, News, Calendar, Roster, Chats, Files.
Members see public and team items together in each tab, newest first, with team items carrying a
**Members** lock tag; Roster and Chats carry a lock in the tab bar. Visitors see the public items and
a "Members see more" note.

- **For:** the fewest tabs and nothing in two places; "what's new?" is one tab.
- **Against:** within a tab the separation rests on a small tag that's easy to stop noticing. A member
  can't tell at a glance what a visitor sees, and a leader replying in the flow has no sense of place.
  Weakest for Kids and Youth, where getting it wrong matters most.

### Common to all three

- The composer always shows **Who sees this** as a two-way switch, with a sentence that says it in
  full ("Only Music members, including Vocal Groups, Choir, Song Leading and Production. Visitors
  browsing the app don't see it." / "Anyone with the app, including visitors looking at Children's Ministry
  and parents."), and the button repeats it: **Post for everyone** or **Post for members only**.
- Every chat starts with "Only Music members can read this chat. Messages are kept."
- Roster and chats are always members-only; there's no public roster or public chat.

### Recommendation

**Team tabs** (changed 2026-10-31 from Team room). The owner's instinct that viewing should feel split
is right, and Team room was recommended because it was the only design where the split was a place
rather than a label. Separating the areas makes every tab a place, so the split no longer needs a
second page: putting the team's areas after a lock in the same tab bar keeps the boundary in view the
whole time, and the band on each area says it in words. It drops Team room's costs (a second tab bar,
Calendar and Files twice, an extra hop) and keeps its strengths (the public tabs are what the public
sees; the default audience follows where you are). The three still differ in a way staff can feel, so
the switch stays: Team room is the fallback if staff want the whole screen to change for team things
(perhaps for Kids and Youth only); Marked is not recommended for ministries with children. To decide
with staff: have a leader post one thing of each audience in each design, and ask a member which
they'd trust.

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
  `/ministries/{id}/{area}`, `MinistryArea.cs`), the team room (`/ministries/{id}/team/{area}`), chats
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
- **Rosters:** Elvanto is the source of truth; leaders roster through the app into Elvanto; members
  read from it; Elvanto stays usable directly (owner, 2026-10-30).
- **Chats:** several per ministry; leaders create; members can if enabled; leaders-post-only chats.
  None for Kids or Youth yet (owner, 2026-10-30).
- **Auth:** out of scope for the POC (owner, 2026-10-30).

## Open questions

1. **Which split?** Team tabs is recommended (Team room until 2026-10-31); staff and the pastor to
   choose.
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
