---
version: 1
slug: "ures-rosters-app-pages-daysawaypage-razor-495bd555"
primary_target: "src/Features/Rosters/GSBC.ChurchApp.Features.Rosters.App/Pages/DaysAwayPage.razor"
related_targets: ["src/Features/Rosters/GSBC.ChurchApp.Features.Rosters.Client/Components/DaysAway.razor","src/Features/Rosters/GSBC.ChurchApp.Features.Rosters.Client/Components/RosterArea.razor"]
---

# Days away (phone page)

Owner feedback #68 (2026-11-12): "This and submitting them should be a page, not just at the bottom of rosters." Interview answers: a leader's roster row opens their own days away; a second row (leaders only) opens the team's. Account opens the same page. The shared month editor (`DaysAway`, `MonthDays`, `AwaySheet`) stays. Decision page (2026-11-12): the owner chose **Month or list** (`.impeccable/mocks/days-away/b-month-or-list.html`).

- **Job:** a volunteer says when they (or their child) can't serve, and checks what they've said; a leader does the same for anyone on the team. Operate mode, phone first.
- **Shape:** a page `/away` (yours, every team) and `/ministries/{id}/away` (the team's, leaders). Masthead "Days away" / "The team's days away" with a one-line note on who sees them. Whose (Segmented or select) when more than one person, then `Month | List` (Segmented, up to 22rem). Month: `MonthDays` with its key, tap a day to add or change. List: repeating rules, then one-off days, soonest first, as hairline rows with chevrons; an empty list says so. Both end with the accent *Add days away* row.
- **Entry points:** Roster tab: a hairline row after the roster section, "Your days away" with the next one as its line ("Next: Sat 21 to Mon 23 Nov" / "None coming up"); for leaders a second row "The team's days away" ("2 people away in the next month"). Account (phone): one row in the Rosters account section opening `/away`. The admin panel keeps the Days away tab of a person and is not changed.
- **States:** loading (`LiveData`), not on the phone yet, nobody (no team), offline writes waiting to send (outbox), skipped days.
- **Boundaries:** no new components beyond what the map has; reads only through Sync; writes through `RosterWrites` (outbox). No change to the editor sheet.

## Direction contract

THESIS: Days away is its own place, not a footnote under the roster; the month is the tool, the list is the record, one switch between them. Refuses the stacked "everything at the bottom of another page" arrangement.
OWN-WORLD: the app's established world: navy masthead, sand fills, Montserrat, marker-orange shading for days away on `MonthDays` tiles, warm repeat tiles, accent add row.
STORY: a volunteer sees the month with their days shaded, taps one to say they're away, or flips to List to check everything they've said.
FIRST VIEWPORT: masthead "Days away" with the note; Whose (if several); `Month | List` segmented at the top of the content; the month grid with its key; Add days away under it.
FORM: Month or list, position 6 of 7 on the grounded list. seed:4c940e80463366fc
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
