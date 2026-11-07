---
version: 1
slug: "events-gsbc-churchapp-features-events-app-76ed283e"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.App"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.Client","src/Platform/GSBC.ChurchApp.Platform.Ui/DayRail.razor"]
---

# The church calendar on the phone

Scope: the calendar (`/week`, Week and Month), an event (`/event/{id}`), Browse calendars and a calendar's page, and
editing an event or a regular event on the phone (the editor is Events.Client's, shared with the admin panel). Mode:
Operate (a member checking the week; an editor fixing a last-minute change). Decisions in docs/features/events.md.

- **Audience and job:** the congregation, on their own phones, checking what's on this week and whether anything is
  different; families planning a month; an organiser on a Saturday morning moving the place without a laptop.
- **Owner's answers (2026-11-08):** this week at a glance first; text first (few events will have a picture);
  wrong would be a feed of identical cards, too many colours, or pages that read like a form. A dense month grid is
  acceptable. Phone editing is required (approved staff UI in the app); calendar setup stays in the admin panel.
- **Content:** calendars and events from Sync (30 days back, 12 months ahead, regular events already expanded);
  made-up names in fixtures.
- **States:** first open with only the default calendars; nothing this week; a date not on (icon and line under,
  never a suffix); changed (marked until opened); details missing (*Place to be announced*); a private calendar given
  to you (*For you*); offline; an event gone; signed out (contacts hidden).
- **Constraints:** DESIGN.md (the Sunday Bulletin world, Montserrat, roles not hexes), Dynamic Type, offline, AOT;
  Platform.Ui's DayRail, rows, fact list, segmented control, sheets and chips reused, not copied.

## Direction contract

THESIS: The week is one running diary from today, where the ordinary stays quiet and only what's different speaks.
Refuses the hour-grid calendar app and the feed of event cards.

OWN-WORLD: The Sunday Bulletin as DESIGN.md records it: navy masthead with the mark's ring, white page, hairline
agenda rows with a navy tabular time column, regular events in lighter ink, an aqua label line on specials, orange
only for Changed and today, sand fields; the event page's white date block on navy.

STORY: A member opens the calendar, sees today and the rest of the week at a glance, notices the special service, the
moved outreach and that Kids is off, and taps one for where and when. An editor taps Edit on the same page, changes
the place for this date only, saves and tells people.

FIRST VIEWPORT: Masthead "This week", beige range "Sunday 8 – Saturday 14 November", Week | Month at its right, the
seven-day rail inside it (today a white tile with an orange dot). Under it the calendar chips and "+ Browse". Then
"Today" and each day as a heading with its rows. The event page: navy head with the SAT/14/NOV block beside label,
title and "8:30–11:30 · every second Saturday"; Add to calendar (primary), Register, Share; change note; facts;
About; Other dates.

FORM: Three locked rounds: the week, *One long week*, 4th on the ranked list, THE ROLL (seed:2dbdda5d5183d2b9);
the event, *The date block*, 4th on its list, dealt (seed:0fe58c64dfcdf28e); editing, *Edit where you read it*,
3rd on its list, THE ROLL (seed:3a665141cb59d1aa). Signature move: the day rail that follows the running list as
it scrolls, and Changed that stays lit until you've opened it (from the gate-board challenger).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
