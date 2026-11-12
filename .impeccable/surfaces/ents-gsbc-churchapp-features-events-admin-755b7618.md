---
version: 1
slug: "ents-gsbc-churchapp-features-events-admin-755b7618"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.Admin"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.Client"]
---

# The church calendar in the admin panel

Scope: the admin panel's Calendar page: the workbench of calendars, Needs details, Asked to publish, Agenda | Month,
the event editor (Events.Client's, shared with the phone), a calendar's settings, named breaks, and later the digest
proof of concept. Mode: Operate, on a computer (desktop first). Decisions in docs/features/events.md.

- **Audience and job:** the office and calendar editors entering the term's events, setting up regular events and
  breaks, filling in details before they're due, and setting up calendars (holders of `events.calendars.manage`).
- **Owner's answers (2026-11-08):** the workbench over lanes and a desk; calendar settings and breaks inside the
  workbench, not on a page of their own; Ask to publish built and off.
- **States:** nothing needs details; a calendar copied from iCal (read-only, last checked); a calendar you can't set
  up (no settings button); Ask to publish on for one calendar; a date of a regular event being changed on its own
  (the date's editor, *Edit the regular event ›*); a regular event open across the middle and right panes; saving with Tell people offered.
- **Constraints:** DESIGN.md's admin shell (navy sidebar, title band, workbench rules, Sidebar Offset Rule,
  breakpoints), the shared audience picker and chip picker, roles not hexes.

## Direction contract

THESIS: One workbench where the church's calendars are kept: choose calendars on the left, read them in the middle
in the same rows as the phone, change one thing on the right. Refuses a wall of tool pages and a separate settings
area.

OWN-WORLD: The admin panel as DESIGN.md records it: navy sidebar, sand list column with switches and orange counts,
white agenda pane with navy tabular times and aqua label lines, a lifted white panel on the right headed in navy
with the date block, sand fields; a regular event's run of dates on navy (2026-11-08).

STORY: An editor opens Calendar, sees two events need details, clicks one, adds the place in the right pane and
saves. The office turns on Kids team to check a clash, opens Outreach's settings to add an editor, and fixes the
Christmas break's dates once for every regular event.

FIRST VIEWPORT: Sidebar with Calendar current (count of Needs details). Left sand column: "Calendar", All your
calendars, Needs details 2, Asked to publish, then each calendar with its settings button and switch, + New
calendar, Breaks 3. Middle: Agenda | Month | Regular events (a picked calendar) and + New event (Once or Regularly), then Today, Wednesday… in the phone's rows, the chosen
row on sand. Right: the event's panel (date block head, Where, Contact, About, Save, Not on this date), a date of a
regular event, or a calendar's settings, or a break.

FORM: *Workbench*, 1st on the ranked list, dealt over THE ROLL's lanes (seed:c5796392755f9ab0). Signature move:
the right pane that is always the thing you chose (an event, a calendar, a break), editing in place.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## A calendar's page, and the church's order (2026-11-12)

Owner's picks on the decision page (`.impeccable/mocks/admin-nav-calendar/`, one round with the sidebar and the phone's
Send feedback): #53 "move the settings button out of the side bar, make it a tab on the calendar page, like ministries
does it" → **Events · Regular events · People · Settings** (`cal-tabs-people.html`): the name as a PaneHead with a lead
and its ⋯ menu (Remove this calendar), TabStrip, Agenda/Month stays a Segmented inside Events, People lists who's on it
inline, Settings is one grouped card with Cancel and Save, then Calendar apps. #52 "Allow me to reorder the calendars on
the left side" → **drag by a handle** (`order-drag.html`), the shared `ReorderList` (Alt+↑/↓ on the handle), one order for
the church that phones show too, set by Calendar Managers. #51: the toolbar's items keep their size and wrap.
