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

### Pinned for later

- **The website shows the app's calendar** (owner, 2026-11-07): replace the website's events page with an
  embed from the app. To discuss another time. Whatever it serves will need the same care as iCal out (an
  HTTP path needs the owner's yes; a static file may not).

## Open

- **Reading iCal in,** calendar by calendar (recommended, not yet answered).
- **Who edits a calendar:** a permission scoped to the calendar, and how a ministry's leaders come to hold it.
- Regular events, specials and breaks: the rules.
- Not overloading people: what someone sees by default, following, Home, filters, a digest.
- What an event carries: place (Site map), people responsible, links, picture (Media), description.
- Details filled in later, and reminding whoever is responsible.
- Subscribing from a phone's calendar app (iCal out; needs the owner's decision on an HTTP path or a static
  file), and reading iCal or Elvanto in.
- Drafts and publishing for events; notifications that link to an event.
- Offline and how far ahead and back phones keep events.
- The admin panel and the phone: which screens go where.
