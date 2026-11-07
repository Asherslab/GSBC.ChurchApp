# Events: the church calendar

What's on at the church, in calendars people choose to follow, without drowning anyone in everything. Asked for
by the owner on 2026-11-07; SPEC §3 (*Events*). Designed with the owner on 2026-11-07/08; this doc records each decision, then what was
built (*Status*) and what was decided while building it.

## Words

Used the same way in this doc, the code and the screens (owner, 2026-11-07):

| Word | Means | Example |
|---|---|---|
| **The church calendar** | The whole feature, in people's words (the `Events` feature in code) | |
| **An event** | One thing on one date that people can go to | *Sunday evening service, 9 November, 6pm* |
| **A regular event** | A pattern that puts an event on many dates | *Sunday evening service, every Sunday at 6pm* |
| **A special** | One date of a regular event, changed | *Anniversary Sunday Service* |
| **A break** | Dates when a regular event doesn't happen | *No Sunday evening service, 21 December to 25 January* |
| **A calendar** | A named group of events that people follow or subscribe to (the owner's "channel") | *Sunday services*, *Kids*, *Kids team*, *Church members* |

Drafts' *event layer* (the app's look for a conference week, [drafts.md](../architecture/drafts.md)) is
unrelated and isn't used here.

## What the owner has said (2026-11-07)

- It's a **church calendar, not a registration system.**
- **Calendars:** some things matter to some people and not to all (Kids & Youth, for example).
- **Regular events** (Sunday services and so on), with **specials** ("Anniversary Sunday Service").
- **Breaks:** Sunday evening is usually off over the Christmas period.
- **Don't overload people.** The calendar must not drown people in everything; "a bit complicated", so it
  gets real design.
- **Subscriptions:** people subscribe to the calendars relevant to them (iCal and so on). Reading other
  calendars in (iCal) would also be good.
- **Ministries manage their own calendars:** Kids has its regular events and its special ones.
- **An event shows a decent amount when opened:** outreach Saturdays usually need a location.
- **Details come later:** a regular event often gets its details (location and so on) shortly before it
  happens, not when it's created. Whoever is responsible is reminded (in the app, by email or similar) before
  it's due.
- **Events don't notify by themselves.** A notification can link to an event, but telling the church is a
  conscious act (an event tomorrow without the expected registrations could send a reminder).

## Decided

### Calendars (owner, 2026-11-07)

1. **Calendars belong to the church calendar.** Every event lives in a calendar the Events feature owns. A
   ministry has calendars assigned to it (one or several); its leaders manage those calendars and can give
   others access to them. A ministry's page shows its calendars from here rather than keeping a list of its
   own, which replaces the per-ministry calendar module in [ministries.md](ministries.md) (not built). The
   ministry side is built with Ministries, not here.
2. **Some calendars belong to no ministry:** *Sunday services*, *Whole church*, *Church members*. The office
   looks after them.
3. **One home calendar per event, shown on others too.** An event belongs to one calendar, which decides who
   edits it, and can also be shown on other calendars when whoever looks after each of those agrees.
4. **Who sees an event is the calendar's, never the event's.** A calendar is for everyone or for a group; a
   leader adding an event chooses its calendar, not an audience. Kids has *Kids* (everyone) and *Kids team*
   (its members). *Church members* (AGMs and the like) is for people who are officially members of the
   church and belongs to no ministry. Nothing private can reach someone who subscribed to a calendar meant
   for everyone.
5. **Who a calendar is for** is chosen as Hub tiles' audience is: everyone, anyone signed in, or people in
   chosen roles, checked through Access ([home.md](home.md), *Who sees it*). *Church members* is for the
   **Church member** role, which the office gives by hand in the admin panel; it may follow Elvanto's
   membership later. A ministry's team calendar is for that ministry's team once Ministries is built.

### Where events come from (owner, 2026-11-07)

6. **Entered here,** in the admin panel (and by ministry leaders once Ministries is built). The church uses
   Elvanto for rosters and services only, not for events, so Elvanto isn't a source of events. The church
   website has an events page, but staff stopped keeping it up a month or two ago (its editor frustrated
   them).
7. **A Register link** on an event, opened in the app's browser. The app never counts registrations; a
   reminder that registrations are low is a person's decision, sent as any notification is.

8. **Reading iCal in:** a calendar can copy an outside iCal address (school term dates, a Google Calendar a
   ministry already keeps). It stays read-only here and refreshes by itself; reading it is an outbound call.

### Regular events, specials and breaks (owner, 2026-11-07)

9. **A regular event repeats** weekly (on one or more days), every second week, monthly by weekday ("first
   Sunday", "third Friday"), monthly by date, or on a hand-picked list of dates (a six-week course). Each has a
   start and an optional end. Nothing more exotic until someone needs it.
10. **Breaks are named and reusable.** The office keeps breaks such as *School holidays 2027* and *Christmas
    break* once, and any regular event can use them; a regular event can also have its own one-off breaks.
    Fixing a break's dates fixes every regular event using it.
11. **A date that's not on still shows, clearly.** For the first week or two of a break, the skipped date shows
    in the calendar as not on, with the break's name, marked by an icon or colour; the event's name is never
    given a suffix ("— not on"), which a narrow row would cut off. A subscribed phone calendar just leaves the
    date out.
12. **A special keeps in step with its regular event,** overriding only what it changed (title, time, place,
    description, picture), as sermon corrections do. Changing the regular event warns first, naming the specials
    that keep their own value ("Anniversary Sunday keeps its own time, 9:00").
13. **Sunday services are entered here** as regular events in v1, not read from Elvanto. Whether to read
    Elvanto's services later is undecided.

### Not overloading people (owner, 2026-11-07)

14. **A new install shows only the default calendars.** The office marks a few calendars as for everyone by
    default (*Sunday services*, *Whole church*, likely). Every other calendar for everyone is listed to find,
    and stays off until someone **adds it to their calendar**. Adding needs no sign-in, and moves to the person
    when they sign in, as following a sermon series does.
15. **A ministry's calendars show for its team.** Someone given a private calendar (*Kids team*, *Church
    members*) sees it without adding it. A ministry's team also sees that ministry's public calendars
    (*Kids*): helpers almost always serve at those events. Either can be turned off.
16. **Regular events stay in the background.** The week view shows them as quiet single lines; the month view
    marks specials, changes and dates not on; the Home tile's **Coming up** shows the next few one-off events,
    specials and dates not on. The calendar itself has no Coming up section (owner, 2026-11-08, with *One long
    week*).
17. **Chips to hide a calendar for a moment,** one per calendar you've added, with **Browse all calendars**
    beside them to add or remove calendars. No "show everything" switch.

### The weekly digest (owner, 2026-11-07)

The office sends *What's Next at Good Shepherd* by email today, built by hand: a few items, each with a
picture, a label (the ministry or kind: "Outreach", "Ladies Event", "Life Groups on break"), a when line ("This
Saturday 24 October", "This Friday @ 6:30pm", "Next two Sundays"), a description with the place, a contact and
sometimes a register link, and a button to the website's events. Its items are exactly the calendar's one-off
events, specials and breaks, and some arrive before their details ("full details this Sunday").

18. **Try a "send the digest" button** in place of building it by hand: the digest made from the calendar.
    Only a proof of concept to show staff, with little invested, since it may be scrapped quickly. Built after
    the design settles, as a quick mock with sample events, not real code. Recommended shape: an admin page
    pre-fills the digest from *Coming up* for the next two weeks; staff untick, reorder or reword items; **Copy
    for email** gives the finished email for the tool the office uses now; **Send to the app** posts a
    notification that opens the digest in the app. The app's digest is **in the app's own design language**,
    made of its own components, never a copy of the email (owner, 2026-11-07). Which tool sends the email today
    isn't known (not Elvanto: it has no such template).

    The proof of concept is in [`.impeccable/mocks/digest/`](../../.impeccable/mocks/digest/README.md): the
    admin page, the digest in the app and the email (2026-11-08). A picture leading each item is fine here
    (owner, 2026-11-08): today's What's Next already puts a stock church photo related to each item between them,
    so the digest's picture cards are what people know, unlike the calendar, which stays text first.

### What an event carries (owner, 2026-11-07)

19. **Title, calendar and when** (start and end, all day, or several days for a camp), and optionally: a short
    **label** when the calendar's name isn't the right one ("Ladies Event"), a **picture** (Media), a
    **description** ([rich text](../architecture/rich-text.md)), a **place**, **contacts**, a **Register link**
    and other links (a flyer).
20. **A place is a site-map place or an address.** A site-map place (`PlaceId`, [site-map.md](site-map.md))
    opens the map; an address (Teralba Park, a bowls club) opens the phone's maps app. Addresses used before are
    offered again, so outreach Saturdays aren't retyped.
21. **Contacts** are app people (name, phone and email from their account, so they stay current) or a name with
    a phone and email typed in. Contact details show only to people signed in. A contact is shown, not given
    access to anything. **Once Ministries is built,** an event on a ministry's calendar offers *Contact the
    ministry* instead, through the ministry's chat, so the ministry decides who answers (several members, only
    leaders, or one person). Events reaches that through a place in the app (destinations.md), not by
    referencing Ministries.

### Details filled in later (owner, 2026-11-08)

22. **Organisers are reminded.** A regular event's organisers default to whoever owns its calendar (the
    outreach ministry's leaders for *Outreach*; the office for *Whole church*), meaning the holders of the
    calendar's edit permission. A regular event can name its own organisers instead: an Access role given at
    that regular event, never a list of people kept by Events. Contacts are shown to people and never reminded.
23. **A regular event says what each date needs** ("each date needs: a place"), ticked when it's set up.
    Nothing by default, so ordinary Sundays never remind anyone.
24. **Reminders 7 days and 2 days before** while something needed is missing, adjustable per regular event,
    by inbox and push. Email when Notifications has an email channel (the kind allows it). The admin panel
    lists **Needs details**, so nothing depends on a notification being opened.
25. **An event shows before its details are in,** saying what's missing in plain words: *Place to be
    announced*.

### Subscribing from a calendar app (owner, 2026-11-08)

26. **One standard way for every calendar: iCal links served by the gateway.** The owner approved this
    non-gRPC path in chat on 2026-11-08 (AGENTS.md's hard rule): calendar apps (iPhone, Google, Outlook) only
    fetch calendars over plain HTTP, so it can't be a gRPC method. One method for every calendar, public or
    private, lets ministries put their events wherever suits them; nothing a ministry should put in a calendar
    is a major risk. A static file in R2 (as the podcast feed is) and writing into the phone's own calendar
    were considered; neither serves private calendars to every calendar app.
27. **Subscribe to one calendar, to all of yours, or to everything public.** A calendar's own link gives that
    calendar; *All my calendars* gives one subscription holding everything you've added and been given; *All
    public calendars* gives every calendar meant for everyone, for anyone who doesn't want to think about it. Private calendars are
    reached only through your own link, which carries a secret, works for a signed-out phone as well as a
    person, and can be reset (the old link stops working).

Built 2026-11-08: the paths below are in AGENTS.md's approved list and `EndpointRules`' allowlist (the owner approved
all four on 2026-11-08):

| Path | Host | Returns |
|---|---|---|
| `GET /calendars/{calendar}.ics` | Gateway | A calendar for everyone (no secret needed) |
| `GET /calendars/public.ics` | Gateway | Every calendar for everyone, in one (`public` and `mine` can't be a calendar's name in a link) |
| `GET /calendars/mine/{secret}.ics` | Gateway | Everything the link's holder has added and been given |
| `GET /calendars/mine/{secret}/{calendar}.ics` | Gateway | One calendar through the holder's link (how a private one is subscribed to alone) |

### Who edits, and publishing (owner, 2026-11-08)

28. **Permissions** (Access, [permissions.md](../architecture/permissions.md)): `events.edit` at a calendar
    (a new scope kind, *calendar*, in Platform.Permissions) adds and changes events and regular events in it;
    `events.calendars.manage`, everywhere, creates calendars, chooses who each is for and which are defaults,
    and keeps the named breaks. A **Calendar editor** role is given at a calendar, and its editors can give it
    to others. A ministry's leaders get it on the ministry's calendars when Ministries is built, and so can
    anyone else the ministry chooses (its admins, who do the paperwork, or one member), because editing is a
    role at the calendar, not tied to being a leader.
29. **Shown, or not yet shown.** An event or regular event is saved for later, shown now, or shown from a date.
    Not drafts (drafts.md, decision 1).
30. **Ask to publish is built, and off everywhere.** A calendar can be set to need a publisher
    (`events.publish` at the calendar): its editors without that permission get *Ask to publish*, listed for
    publishers and notified as drafts' *waiting to publish* is. Which calendars use it is the church's call
    later (probably none).
31. **Changing a shown event is quiet,** but it's marked **Changed** for a few days with what changed ("Now in
    the Fellowship Area"). Beside Save, **Tell people** offers a notification that opens the event, to everyone
    who has its calendar. Cancelling one date works the same way: it shows as not on, with *Tell people*
    offered.

### The phone and the admin panel (owner, 2026-11-08)

32. **Phones keep 30 days back and 12 months ahead.** The server works out each regular event's dates within
    that window, so the phone has no repeat logic and the calendar works fully offline. The iCal links cover
    the same window.
33. **The calendar page has a week view and a month view.** *This week* (`/week`, already Home's tile) opens on
    the day rail and agenda with *Coming up*; the month view is a tap away for planning ahead.
34. **Whether it's a tab is the church's choice:** the owner wants the app's tab bar configurable by staff in
    the admin panel, through drafts, as the Hub is. That's a Configuration change of its own (below); the
    calendar page is a place that can be a tab.
35. **Full event editing on the phone too** (owner, 2026-11-08, the yes AGENTS.md asks for before putting
    staff screens in the app): a meeting moved or cancelled at the last minute is handled by whoever is there,
    and they may not have a laptop. Editors edit events and regular events on the phone as in the admin panel,
    from the same components, gated by `events.edit` at the calendar (repo-layout: a management screen lives in
    the feature's Client). **Calendar setup stays in the admin panel** (owner, 2026-11-08): creating calendars,
    who each is for, the defaults and the named breaks aren't done on the fly.
36. **Designed with impeccable's whole process,** every screen, phone and admin panel alike: interview,
    decision page, surface brief, detector and finish review.

### The design (impeccable)

- **The week on the phone: *One long week*** (owner, 2026-11-08, on the decision page over *The noticeboard*
  and *What's different*; sketches in `.impeccable/mocks/decision/events/`). The masthead holds *This week*,
  the date range, Week | Month and the seven-day rail; the calendar chips sit under it; then one continuous
  list from today onward, a heading per day, and scrolling past Saturday carries on into next week with the
  rail following. Regular events are quiet single lines (time and name, lighter); a special or one-off is a
  fuller row with its label and place; a date that's not on keeps its line, with an icon and "Not on this
  week · back on 20 November" underneath, never a suffix on its name. A changed event keeps its orange
  *Changed* mark until you've opened it.
- **An event, opened: *The date block*** (owner, 2026-11-08, over a sheet over the week and a map-led page;
  `.impeccable/mocks/decision/events-page/`). A pushed page (`/event/{id}`, back to where you came from). Its navy
  head sets a white date block (weekday, day, month) beside the label, the title and the time with how it repeats
  ("8:30–11:30 · every second Saturday"); a picture, when there is one, sits under the head. Then *Add to calendar*
  (primary), *Register* when there is one, and *Share*; the change note in an orange-tinted box; the place and
  contacts as facts (an address opens Maps, a site-map place the map); *About*; *Other dates* of the regular event.
  Missing details read in plain words where they'd be (*Place to be announced*).
- **Changing an event: *Edit where you read it*** (owner, 2026-11-08, over a step-by-step flow and an editable
  sentence; `.impeccable/mocks/decision/events-edit/`). *Edit* on the event page turns it into its editing state:
  the date block, title and time line are outlined and open their pickers when tapped; the facts become fields (the
  place is a search over site-map places and addresses used before); fields the page hides when empty (Register,
  picture, links, contacts) appear as *Add…* rows. A foot bar holds, for a date of a regular event, *This date
  only | Every …* (the regular event's own words), and *Save*; saving a shown event offers *Tell people*. A new
  event opens the same page empty, starting on its calendar and title. Tapping the time line of a regular event
  opens how it repeats, its breaks and what each date needs. One component (Events.Client), on the phone and in the
  admin panel.
- **The admin panel's Calendar page: *Workbench*** (owner, 2026-11-08, over lanes by calendar and a needs-you
  desk; `.impeccable/mocks/decision/events-admin/`). Three panes, as drafts' workbench: a sand column with *All
  your calendars*, *Needs details* and *Asked to publish* (each with its count, the latter only when a calendar
  uses it), then each calendar you can edit with a switch and a settings button, *+ New calendar*, and *Breaks*;
  the middle pane is Agenda | Month of the chosen calendars, in the phone's rows, with *+ New event*; the right
  pane is the chosen event in the same editor as the phone (*This date only | Every …*, *Save*, *Not on this
  date*).
- **Calendar setup lives in the workbench too** (owner, 2026-11-08: "settings per calendar on the workbench, breaks
  & etc"; `workbench-settings.html`), not on a page of its own. A calendar's settings button opens them in the right
  pane: description, who it's for (Hub tiles' picker), *Shown on new phones*, *Needs a publisher*, who can edit it
  (Calendar editor holders at that calendar, given through Access), an iCal address to copy from for a read-only
  calendar, and its link for calendar apps. *Breaks* lists the named breaks in the middle pane (dates, and which
  regular events use each) and opens the chosen one on the right. Setup controls show only to holders of
  `events.calendars.manage`.
- **The month** (owner, 2026-11-08; `month.html`): the masthead names the month with Week | Month and arrows; the
  chips stay; a grid of days, each with up to three small marks (regular events pale, a special or one-off in
  accent, changed in marker, not on as an open ring) and a key under it; tapping a day lists its events below in
  the week's rows. Today is filled, past days fade.
- **Browse calendars** (owner, 2026-11-08; `browse.html`), from *+ Browse* beside the chips: *In your week* (a
  switch per calendar; one given to you is marked *For you*), *More calendars* with *Add*, and *In your phone's
  calendar* with *All my calendars* and *Everything public*. A calendar's name opens its page: what it's for,
  what's next in it, and its own *Subscribe*.

- **Surface briefs** with their direction contracts, for whoever builds Events:
  `.impeccable/surfaces/events-gsbc-churchapp-features-events-app-76ed283e.md` (the phone) and
  `.impeccable/surfaces/ents-gsbc-churchapp-features-events-admin-755b7618.md` (the admin panel).

### Pinned for later

- **A configurable tab bar** (owner, 2026-11-08): staff choose the app's tabs in the admin panel, through
  drafts. Not part of Events; Configuration's to design.

- **Which tool sends *What's Next* today,** to ask the office; it decides what *Copy for email* produces.

- **The website shows the app's calendar** (owner, 2026-11-07): replace the website's events page with an
  embed from the app. To discuss another time. Whatever it serves will need the same care as iCal out (an
  HTTP path needs the owner's yes; a static file may not).

## Status

**Events core (2026-11-08, Station 1):** the data, rules and services; the screens, iCal and reminders build on it.

- **Contracts** (`Features.Events.Contracts`): the `Events` service for phones (`[AppInstall]` add or turn off a calendar,
  and which you've added), `Editing` for editors (`[Requires(events.edit)]` at the calendar, with `IScopedRequest`; the
  workbench's lists are `[SignedIn]` and filtered per calendar), and `Setup` (`[Requires(events.calendars.manage)]`:
  calendars and named breaks). Dates in patterns and editing are the church's (`yyyy-MM-dd`, minutes after midnight);
  a dated row's start and end are UTC.
- **Storage** (schema `events`): calendars, one-off events, regular events (weekly on days, every second week, monthly
  by weekday or date, a list of dates; start and optional end), specials, named and own breaks, showings on other
  calendars, and the rows last published. An iCal-copied calendar is read-only (`ICalSource`; events carry `SourceUid`).
- **Dated rows:** `EventsBook` loads the calendar and `Expansion` works out each regular event's dates within 30 days
  back and 12 months ahead; a special overrides only what it changed; a break's first 14 days show as not on with its
  name and when it's back. `EventsPublisher` publishes on every change and every 15 minutes (the window slides), and a
  date leaving the window is sent as a delete.
- **Feeds:** `events.calendars` and `events.public` (public); `events.calendars.private`, `events.private` and
  `events.contacts` (each row carries its audience, below); `events.admin` (markers for the workbench, per calendar).
- **Access:** scope kinds `calendar` and `regular-event`; nodes `events.edit`, `events.publish` (at a calendar),
  `events.calendars.manage` (everywhere), `events.organise` (at a regular event); system roles *Calendar editor* (its
  holders give it at that calendar), *Calendar publisher*, *Calendars manager*, *Event organiser*, and the
  phone-holdable *Calendar added* and *Calendar turned off*.
- **Places:** `/week` (moved from Coming soon), `/event/{id}` (an event's date) and `/calendar/{id}`, with destination
  sources; `Events.App` has a placeholder page for the three routes until the screens replace it.
- **Notifications:** *Tell people* (`events.told`, per calendar), *waiting to publish* and *asked to show on your
  calendar* (both in the admin panel).
- **Sample calendar** in Development only (made-up names): Sunday services with an Anniversary special and a
  Christmas break, Whole church, Kids (with a camp weekend off), Kids team, Outreach every second Saturday needing a
  place, Church members.
- **Tests, split by whether they need storage:** `Events.Unit.Tests` touches no database, container or network
  (patterns, breaks, specials, the window, which feed and audience each row gets, contact details, audiences and
  widening; `EventsBook` is built from plain lists and `EventsPublisher.Rows` is pure). `Events.Api.Tests` runs against
  Postgres only for rules that are about storage: publishing deletes, per-calendar permission checks, Ask to publish,
  this date only vs every date, adding a calendar and *Tell people*, and showing on another calendar. Sync's per-row
  audiences are `RowAudienceFeedTests` in Platform.Api.Tests (Postgres). The split follows `testing.md` once it lands.

**Events screens (2026-11-08, Station 2):** every screen, on the phone and in the admin panel, built with impeccable from
the locked sketches and finish-reviewed (`.impeccable/review/events/`).

- **The phone** (`Events.App`): `/week` is *One long week* (masthead with Week | Month, the day rail in a sticky band
  that follows the list, chips, Browse; regular events quiet, specials and one-offs fuller, not on with an icon and a
  line under, *Changed* until opened); `/week?view=month` is the month (marks, key, the chosen day's rows);
  `/event/{id}` is *The date block* (Add to calendar, Register, Share, the change note, place and contacts as facts,
  About, Other dates, missing details in plain words); `/week/calendars` is Browse (in your week, more calendars,
  subscribe to all yours or everything public); `/calendar/{id}` is a calendar's page with its own Subscribe;
  `/event/new` starts a new event.
- **Editing** (`Events.Client`, shared): *Edit where you read it* (`EventEditor`): the date block, title and time line
  open their sheets; the place searches places used before; contacts, About, Register, links and a picture; how a
  regular event repeats, its breaks, what each date needs and when organisers are reminded; *This date only | Every …*;
  *Not on this date*; *Tell people* after saving something already shown; *Ask to publish*; *Also shown on* with the
  widening sentence; delete. On the phone for holders of `events.edit` at the calendar.
- **The admin panel** (`Events.Admin`): Calendar is the workbench (your calendars with switches and settings, Needs
  details, Asked to publish with *Asked to show on your calendar* and its widening sentence, + New calendar, Breaks;
  Agenda | Month; the editor, a calendar's settings or a break on the right). Its Needs details and Asked to publish
  feed the office week and the sidebar's count.
- **Home:** *This week* (`hub.week`, same key and layouts) moved from Configuration's waiting tiles into Events as
  `EventsTiles.Week`, showing *Coming up*.
- **Shared components** added to Platform.Ui: `AgendaRow`, `WeekRail`, `ToggleChips`; `Masthead` gained a `Beside`
  slot, `Segmented` a `Class`, `Page` an `OnBack`.
- **Tests:** `Events.Client.Tests` (unit, no I/O): Coming up, this date only vs every date, the not-on line, the widening
  sentence.


**iCal and reminders (2026-11-08, Station 3):** calendar apps, copying calendars in, and reminding organisers.

- **iCal out** (decisions 26, 27): the four approved paths on the gateway (`Features.Events.Gateway`), each asking the
  API's `CalendarFiles.Read` (`[Public]`) for the file. `{calendar}` is the calendar's link name (its slug). A private
  calendar is served only through the secret link of someone it's for, checked through Access when the file is
  asked for; a calendar the holder can no longer see is left out of *All my calendars* and answers 404 alone.
- **Secret links:** `CalendarFiles.Link` and `ResetLink` (`[AppInstall]`: the person when signed in, otherwise the
  phone) and `PersonLink` and `ResetPersonLink` (`[SignedIn]`, for the admin panel). `CalendarLinks` builds the paths
  and the `webcal://` address for the Subscribe buttons (`CalendarLinks.Subscribe(origin, path)`). The contract calls
  the link's secret its **key** (`CalendarLink.Key`, `CalendarFileRequest.Key`), since client code may not use the
  word "secret" (`SessionSecretRules`); the server and the approved paths keep `{secret}`.
- **iCal in** (decision 8): every calendar with an iCal address is copied every 30 minutes and straight after an
  address is set (`ICalSchedule`, one replica at a time), with ETag/If-Modified-Since; its status is the calendar's
  `ICalReadAt` and `ICalProblem` in `CalendarSettings`, and an `events.admin` marker (the calendar's id) tells the
  workbench to look again.
- **Reminders** (decisions 22-25): `events.details.needed` (inbox and push, per calendar), raised by `DetailsReminders`
  on the 15-minute schedule (one replica at a time).
- **Tests.** `Events.Unit.Tests`, no I/O: reading an iCal calendar (a weekly repeat with a skipped and a changed
  date, in another time zone, and an all-day camp) into dated events; the ETag on the next read; an address on a
  private network never read; writing a calendar file (escaping, folding, all-day ends); a reminder due once at 7 and
  once at 2 days, and when it settles. `Events.Api.Tests`, against Postgres because the secret links live there: a private calendar never
  served without a valid link of someone it's for (nor after their role is taken away, nor to a signed-out phone
  holding a stale *Calendar added*); a reset link refused. `EndpointRules` allows exactly the four paths.

**Not built yet:** the digest (decision 18).

## Decided while building (Station 1, 2026-11-08)

- **Showing an event on another calendar waits for the home event** (owner, 2026-11-08: "nah, any calendars linking
  to the core just wait until the core event is published. Can't be published until then."). Agreeing to show it isn't
  publishing and the copy has no publish of its own: it appears on the other calendar only while the home event is
  published, and goes when that event is saved for later again or deleted. Agreeing needs only edit rights on the
  other calendar.
- **A shown event reaches the union of its calendars' audiences, contacts included** (owner, 2026-11-08, over keeping
  contacts at the home calendar's audience). Before agreeing, the agreeing editor sees one plain sentence when it
  would widen ("This will show to everyone with the app, not only Kids team."): a pending showing carries `Widens`,
  `NewlyReaches` ("everyone with the app", "anyone signed in") and `NewlyReachesRoleIds` (the screen names the roles).

- **Feeds respect calendars' audiences row by row.** A calendar's audience is Hub tiles' shape (everyone, anyone
  signed in, chosen roles), so Sync gained `FeedAudience.EachRow`: each row carries its audience, Sync sends a row only
  to people it includes (through the person's grants, so role membership is Access's), a signed-out phone gets none
  of these feeds, and the feeds reset for a person when their grants change, as permission feeds do. A row someone isn't in
  reaches them as a deletion of that record (only its id), so a phone that lost access drops the row even after
  retention has pruned the publisher's tombstone. The publisher still writes a tombstone before a row whose audience
  changes. An event moving between the public and private feeds leaves a tombstone in the old feed, which is the
  newest row for it there and so isn't pruned until deletions expire (then the feed resets). A contacts row's audience
  is the event's own, narrowed to people signed in, never wider. Rows of calendars
  for everyone go in the public feeds; nothing private is ever written there.
- **An event shown on other calendars** has the audience of all its calendars together (any for everyone makes it
  public). An editor of both calendars agrees at once; otherwise the other calendar's editors are asked. Asking to show
  a shown event elsewhere goes through its home calendar's publisher rule, as any change to it does.
- **"Given" means a calendar for chosen roles you hold.** A calendar for anyone signed in is listed to add, like one
  for everyone. In your week = added, or (not turned off and (shown on new phones or given)): `CalendarChoices`, used by
  the phone and by iCal's *All my calendars*.
- **Only a calendar that's for you can be added.** A signed-out phone may add only calendars for everyone; a person,
  only calendars whose audience includes them. Holding *Calendar added* never shows anyone a row its audience
  excludes (the feeds decide), and iCal's *All my calendars* must check the audience too (Station 3).
- **Adding and turning off are roles** (*Calendar added*, *Calendar turned off*), held by a signed-out phone and moved to
  the person when they sign in, as following a sermon series is. Turning off is only recorded for a calendar you'd
  see anyway (a default or given one).
- **Tell people is never wider than the calendar.** A calendar for chosen roles: those roles' holders. A calendar for
  anyone signed in: everyone signed in (never the phones that added it, so it's never written as a public
  notification). A calendar for everyone: everyone if it's shown on new phones (someone who turned it off is still
  told and can mute *News about an event* for that calendar), otherwise those who added it.
- **Ask to publish:** on a calendar that needs a publisher, an editor without `events.publish` saves for later and
  asks; once it's shown, only a publisher changes or deletes it.
- **This date only** sends the date's whole values; the server keeps only what differs from the regular event, so a
  field put back to the regular event's value keeps in step again, and a date that matches in everything goes back to
  being an ordinary date. **Every date** first returns what the specials keep (*Anniversary Sunday keeps its own time,
  9am–12pm*) and saves when confirmed.
- **Changed** stays on a row for 7 days with its note ("Now at Teralba Park"); the phone shows it until it's opened.
  Notes are only made for an event that was already shown.
- **Organisers** are holders of *Event organiser* at the regular event (scope kind `regular-event`); when nobody holds
  it, the holders of `events.edit` at its calendar (for Station 3's reminders).
- **Contacts:** names travel with the event; phone and email only in `events.contacts`, to people signed in who may see
  the event.
- **The admin panel's menu** honours permissions held at one calendar (`CanSomewhere`), so a calendar's editors see the
  Calendar page.
- **Sync's reset for these feeds** follows only the grants that can decide role membership, so adding or turning off a
  calendar doesn't reset them.
- **Sample data:** *Kids team* and *Church members* are for anyone signed in, because there's no *Church member* role
  yet (the office creates it in Access and chooses it on the calendar).

## Decided while building (Station 2, screens, 2026-11-08)

- **The week starts on Sunday** on the phone and in the admin panel's month, as the sketches do.
- **Routes:** Week and Month are one page (`/week`, `/week?view=month`, switched in place); Browse is `/week/calendars`;
  a new event is `/event/new?calendar=…`. In the month view Week | Month sits on the year line beside the arrows, so a
  long month name keeps the title on one line.
- **Changed until opened** is remembered on the phone per date and change (occurrence id and change time), so a second
  change lights it again. Hiding a calendar with its chip is for the moment: it isn't remembered once the app closes.
- **Add to calendar** saves one event as an `.ics` file for the phone's calendar; **Share** uses the phone's share
  sheet, or copies a line (title, date, time, place) where there isn't one.
- **A site-map place** shows its name with "On the church site" and no link until the site map exists; an address
  opens Maps.
- **Subscribe** buttons open `webcal://` addresses on the gateway's host, built with `CalendarLinks`. *All my calendars*
  and a private calendar's own link use the person's link: `IYourCalendarLink` is `PhoneCalendarLink` on the phone
  (`CalendarFiles.Link`/`ResetLink`) and `PersonCalendarLink` in the admin panel (`PersonLink`/`ResetPersonLink`).
  *Make a new link* resets it; offline, the screen says the link needs a connection instead of showing a broken button.
- **Who can edit it** in a calendar's settings explains and links to Access's *Who can do what*, where Calendar editor
  is given at the calendar. Listing its holders there needs a Platform lookup of who holds a role at a scope (Open).
- **This date only | Every …** keeps two working copies: switching shows that scope's own values, so a special's title
  never leaks into every date by accident.
- **The editor reads the calendar's publishing rules from the desk** (`Editing.Desk`), on the phone too, so an editor
  of a calendar that needs a publisher saves for later and gets *Ask to publish*.
- **Home's tile on the admin canvas** reads the default calendars through `Editing.Agenda` (staff are signed in), so
  no public method was added. The Contracts are unchanged by this station.
- **The agenda's time column** is 4.75rem, not 66px: "10:30am" didn't fit.
- **Not shown on the office rail:** dates that aren't on (the rail has no way to show them other than a suffix).
- **Checked headlessly:** the owner's AppHost held the fixed ports, so the screens were checked by rendering the real
  components with sample data and the real CSS (light and dark), not on dev-app.baptist.com.au or the Simulator.

## Decided while building (Station 3, 2026-11-08)

- **Calendar apps get each date as its own event,** not RRULE/EXDATE: the server already works out every date for
  phones, so a special, a changed time and a date that's not on reach a calendar app exactly as they reach the phone,
  with nothing for each app to get wrong. Each date's UID is `{occurrence id}@calendar.gsbc-churchapp`, so it's
  updated in place. Times go out in UTC, all-day dates as dates. A date that's not on is left out (decision 11).
- **What an event carries in a calendar app:** title, place (a site-map place's name, or the address), the
  description, what's still missing in plain words (*Place to be announced*), the Register link (as `URL` and in the
  description) and other links, and the label as a category. **Contacts are left out**: their details are only for
  people signed in, and a calendar file is fetched by an app, not a person.
- **The gateway asks the API over gRPC.** The gateway keeps no Events data; its four routes call `CalendarFiles.Read`,
  a `[Public]` method that returns no more than the links themselves would.
- **A link's secret** is 24 random bytes (32 characters, URL-safe), kept as it is so the Subscribe buttons can show
  it again. A person or a signed-out phone holds one; reset replaces every link the holder has. **A signed-out
  phone's link becomes the person's when that phone signs in** (on its next *which calendars have I added* call), so a
  calendar subscribed before signing in keeps working and now holds the person's calendars, which moved with them.
- **Caching** (the plan before deploying, ~/.claude/CLAUDE.md): a calendar for everyone and *All public calendars*
  send `Cache-Control: public, max-age=900` (the publisher refreshes every 15 minutes); a secret link sends
  `private, no-store`, so Cloudflare never keeps it at the edge; a 404 sends `no-store`. Dev and preview send
  `no-store` for everything (the gateway's rule). Cloudflare doesn't cache `.ics` at the edge by default.
- **Copying an iCal calendar in:** each date becomes a read-only one-off event keyed by its UID (`UID/date` for a
  repeat), shown at once and quietly (no *Changed* marks); a date gone from the source is removed; a failure keeps
  what was copied and records the problem. Repeats understood: daily, weekly, monthly and yearly with interval,
  count, until, days (with "first", "last" and so on), days of the month and months; EXDATE, RDATE and changed dates
  (RECURRENCE-ID, which carry only their own details, as iCal says). Other rule parts are ignored. Times in any time
  zone are moved into church time. The source is read in full once a day (the window moves), otherwise only when it
  has changed. Only addresses on the public internet are read (an address that resolves to a private network is
  refused), up to 8 MB.
- **Reminders:** sent from the 15-minute schedule; a date coming up inside both 7 and 2 days at once (a regular event
  made late) gets one reminder, not two. They go to the regular event's *Event organiser* holders, or the calendar's
  editors when it has none. **Settled** on the next run after the details arrive, the date is set as not on, or the
  date passes. Once a reminder has gone for a date, removing the details again doesn't send it again. Email follows
  when Notifications gains an email channel; nothing in Events needs to change for it.
- **A failed read keeps what was copied** (the copier detaches its unsaved changes and records the problem). It has
  no test of its own: it isn't a Postgres rule, and testing it without a database means splitting the copier further
  than it's worth today.

## What the owner sets up

- **Nothing new in Cloudflare for dev.** For production, the zone's Browser Cache TTL set to *Respect existing headers*
  (already the plan for every app), and no Cache Rule that caches everything under `/calendars/mine/` (a "cache
  everything" rule would override `private, no-store`).

## Open

- **Checking the iCal links' caching live** through `https://dev-app.baptist.com.au` (and production once deployed):
  Station 3 couldn't run its own backend (the main checkout's AppHost held the ports). Check with
  `curl -sD - -o /dev/null https://dev-app.baptist.com.au/calendars/public.ics` for `no-store` on dev, and on
  production for `public, max-age=900` on the public links and `private, no-store` with no `cf-cache-status: HIT` on
  a secret link.
- **The digest proof of concept** (decision 18): mocks made (`.impeccable/mocks/digest/`); to show staff.
- **A ministry's calendars for its team** (decision 15) waits for Ministries: calendars carry `MinistryId` for it.
- **An app person as a contact** keeps the name, phone and email entered with their id; keeping them current needs a
  Platform lookup of a person's details, which doesn't exist yet.
- **A calendar for a role a phone can hold** (such as *Calendar added*) would let *Tell people* reach phones and be
  written as a public notification. Nobody would choose one, but the calendar settings don't refuse it yet; that
  needs Access to say which roles are phone-holdable.
- **Removing a calendar** isn't built (calendars can be renamed and changed).
- **Who can edit a calendar** can't be listed on its settings yet: it needs a Platform lookup of who holds a role at a
  scope, which Access would provide. Until then the settings link to *Who can do what*.
- **Choosing site-map places** in the editor's Where waits for the site map; the place search offers places used before.
- **Checking the screens live** in the Simulator and through dev-app.baptist.com.au, once a backend can run with them.
