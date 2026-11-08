---
version: 1
slug: "es-events-client-regulareventeditor-razor-c571ae1f"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.Client/RegularEventEditor.razor"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.Client/DateEditor.razor","src/Features/Events/GSBC.ChurchApp.Features.Events.Admin"]
---

# A regular event, and a date of one

Scope: a regular event's own page (`RegularEventEditor`, Events.Client, on the phone and in the admin panel's workbench),
a date of a regular event (`DateEditor`), New event's *Once | Regularly*, and each calendar's *Regular events* list.
Mode: Operate. Decisions in docs/features/events.md (*Regular events have their own page*, owner 2026-11-08).

- **Audience and job:** calendar editors setting up and keeping a regular event (Morning service, Outreach every second
  Saturday), and whoever changes one date of it at the last minute, on a computer or the phone.
- **Owner's answers (2026-11-08):** the editor that did both one date and the whole regular event behind a *This date
  only | Every …* switch was "trash": a regular event is its own thing with its own page; a date is only that date;
  regular events are visible as such; New event asks Once or Regularly first. The run of dates was chosen on the
  decision page over settings rows and a repeat block.
- **States:** a new regular event (no dates yet); dates changed (orange), not on (open ring, a break's name), today; a
  date of a regular event that differs from it (*Usually 10:30am. Put it back*); a calendar with no regular events; a
  calendar that needs a publisher.
- **Constraints:** DESIGN.md (Sunday Bulletin world, roles not hexes), Platform.Ui reused (EventHead's date block,
  Sheet, Segmented, TextField, ListRow), one component for phone and admin panel, Dynamic Type, AOT.

## Direction contract

THESIS: A regular event is shown as what it is, a run of dates: its page opens on them, and a date opened from it is
only that date. Refuses the one editor that switched between a date and the whole series.

OWN-WORLD: The event page's navy head (calendar label in beige, title, the pattern line with a repeat mark), and under
it on the same navy a strip of white date tiles (weekday, day, month): changed tiles tinted orange with a dot, dates not
on as open rings, today underlined in orange. Then sand fields and hairline rows on white.

STORY: An editor opens Sunday services' Regular events, picks Morning service, sees 15 Nov is changed and 27 Dec is
off, clicks 15 Nov to set Anniversary Sunday's time, comes back and adds Whole church under Also shown on.

FIRST VIEWPORT: Head: Sunday services / Morning service / Every Sunday · 10:30am–12pm, Save at the right (admin) or in
the top bar (phone). The strip of the next dates, a key line under it. Then *Every date* (time, place, contact, about)
and *How it runs* (repeats, breaks, each date needs, reminders, also shown on, shown in the app) side by side on a
computer, stacked on the phone; *Delete the regular event* last.

FORM: *The run of dates*, 3rd on the ranked list, THE ROLL (seed:d76caa2eeb8c015e). Signature move: the date strip,
whose tiles are the dates themselves; tapping one opens that date alone.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
