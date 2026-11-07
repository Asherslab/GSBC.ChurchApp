# Events: the church calendar

What's on at the church, in calendars people choose to follow, without drowning anyone in everything. Asked for
by the owner on 2026-11-07; SPEC §3 (*Events*). Being designed with the owner before any code; this doc records
each decision as it settles.

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
    admin page, the digest in the app and the email (2026-11-08).

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

When it's built, the paths below go into AGENTS.md's approved list and `EndpointRules`' allowlist in the same
commit as the code (the owner approved all four on 2026-11-08):

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

## Open

- **Caching of the iCal links** behind Cloudflare: decide each path's `Cache-Control` before it's deployed
  (a secret link must never be cached at the edge) and check it through the public hostname.
- **The digest proof of concept** (decision 18): mocks made (`.impeccable/mocks/digest/`); to show staff.
