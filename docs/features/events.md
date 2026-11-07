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
16. **Regular events stay in the background.** The week view shows them as quiet single lines; **Coming up**
    lists only one-off events, specials and dates that are not on; the Home tile shows the next few of those.
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

### Pinned for later

- **Which tool sends *What's Next* today,** to ask the office; it decides what *Copy for email* produces.
### Pinned for later

- **Which tool sends *What's Next* today,** to ask the office; it decides what *Copy for email* produces.

- **The website shows the app's calendar** (owner, 2026-11-07): replace the website's events page with an
  embed from the app. To discuss another time. Whatever it serves will need the same care as iCal out (an
  HTTP path needs the owner's yes; a static file may not).

## Open

- **Who edits a calendar:** a permission scoped to the calendar, and how a ministry's leaders come to hold it.
- Not overloading people: what someone sees by default, following, Home, filters, a digest.
- Details filled in later, and reminding whoever is responsible.
- Subscribing from a phone's calendar app (iCal out; needs the owner's decision on an HTTP path or a static
  file).
- Drafts and publishing for events; notifications that link to an event.
- Offline and how far ahead and back phones keep events.
- The admin panel and the phone: which screens go where.
