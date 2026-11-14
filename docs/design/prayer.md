# Prayer

Detail behind [DESIGN.md](../../DESIGN.md) for the Prayer desk, its settings and a prayer request's page. Read DESIGN.md first; its rules win.

## Prayer desk (admin)
`/prayer` on the Workbench, under a `TitleBand` ("Prayer"). The rail holds the search ("Words or a name", hidden in Settings) over one `FilterChips Rail` group: New and For the wall with counts, On the wall, Followed up, Answered, Everything, then Settings; the pastoral team sees all of them, a wall approver only the wall's. The list is a data table of requests: the words cut to 70 characters at 600 over their pills (New, Asks for the wall in accent, Under 18 in marker, Get in touch quiet), then who it's from (quiet, one line) and how long ago it was sent. Choosing one opens the record pane:
- **Head:** a `PaneHead` titled by who sent it ("Sam S.", or no name), its lead when it was sent with the time; its ⋯ menu holds Delete.
- **Body,** up to 820px with 16px between parts: an Under 18 pill when it applies, the status as the care steps (the stage pipeline; a wall approver gets one strong status line instead), then their words at lead size (1.6 line height, up to 66ch) and any warning notice.
- **Groups,** 20px apart, each a `Section Grouped` card: The prayer wall, Getting in touch, Notes for the pastoral team (each note a hairline row of the words over its author and age in caption ink-3, with its own ⋯ menu to delete it; the note field and *Add the note* in the foot), and What's happened.
- **Marking it answered** asks for words for the wall in a sheet first (optional, up to 500 characters); the wall shows them under the request.

## Prayer desk settings (admin)
`/prayer`: the desk's last filter chip, *Settings*, shown to the pastoral team and to whoever may change prayer settings, sets when people hear that someone prayed for their request (2026-11-12, chosen on the decision page). Choosing it hides the search and the list, and the settings open in the work area, capped at 680px with 22px between parts.
- **Head:** "When people hear someone prayed" in title type at 700 (balanced), then a lead in body ink-2 at 1.55, up to 62ch, 10px under it.
- **Timeline:** one row of stops, each a 12px dot over its label in caption at 600, tabular, in ink-3, joined by a 2px strong-sand line. It starts at "On the wall" (a ring of accent, label ink-2), then each chosen time as an accent dot ("15m", "1h", "2d"), spaced by the square root of its gap so later, longer waits sit further apart, and ends at "Then every day at 12pm" (a 3px ink ring, label ink) when the daily time is on. It wraps rather than scrolling, each stop at least 56px.
- **Changing it:** "A few times after it goes on the wall" at label size and 700 over a caption ink-3 hint and the chip picker of times (written out, "2 hours"; its search up to 360px); then *Then once a day* as a toggle row and, when on, *Each day at* (7am to 8:30pm by half hours, up to 240px, hinted "Church time."). Save (`SaveBar`, which won't save with no time chosen) and a quiet small *Back to the usual times* when they differ. The page asks before losing changes.
- **Read only:** someone who may see it but not change it gets the timeline and a notice, "Only the pastoral team can change these."; everyone gets the closing caption ink-3 line "People can turn these off in Notification settings."

## A prayer request's page (phone)
A request's page has a solid top bar and no masthead. Under it, a full-width band in strong sand as the theme renders it (it reads a cooler sand than the group ground), 16px by the gutter with 12px between its lines:
- **Meta line:** caption at 600 in ink-2, its parts 12px apart and wrapping: when it was sent, who it's from ("Sam S.") and an Under 18 pill (marker); on the person's own page, when it was sent and how the wall shows them.
- **Status:** for the pastoral team, the care steps (New, Followed up, Answered) as a pressable stage pipeline; for a wall approver, one line at row size and 600 in ink. The person sees their own request read only (Sent, On the wall, Answered; Sent and Answered when they didn't ask for the wall) over the same strong line, and a ⋯ menu at the right of the meta line holds Take it off the prayer wall and Delete. Until it has sent, the band shows its `SaveStatus` instead of the steps.
- **Then** their words at lead size (1.6 line height), and the page takes the group ground for its inset groups: The prayer wall (the words as the wall shows them, the name as a segmented choice, *Shows until* as a date field; Save or *Put it on the wall* at the bottom right of the foot, *Not for the wall* or *Take it off the wall* as the bar's quiet action), Getting in touch, and Notes for the pastoral team.
- **Quiet buttons in a group's foot** keep the group colour as their fill, so they still read as buttons on the group ground.
- **The person's page** ends with a block *It was answered* button and a centred caption hint; it asks for a few words of thanks in a sheet.
