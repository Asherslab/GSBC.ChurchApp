# Prayer

Detail behind [DESIGN.md](../../DESIGN.md) for the Prayer desk, its settings and a prayer request's page. Read DESIGN.md first; its rules win.

## Prayer desk (admin)
`/prayer`, a `Workbench` under the title band "Prayer". The rail: search ("Words or a name", hidden in Settings) over one `FilterChips Rail` group: New and For the wall (counted), On the wall, Followed up, Answered, Everything, then Settings. The pastoral team sees all of them; a wall approver only the wall's. Each request in the list: its words cut to 70 characters over its pills (New, Asks for the wall in accent, Under 18 in marker, Get in touch quiet), who it's from and how long ago.
- **Head:** a `PaneHead` titled by who sent it ("Sam S.", or no name), its lead when it was sent; Delete in its ⋯.
- **Body:** an Under 18 pill when it applies, the status as the care steps (`StagePipeline`; a wall approver gets one strong status line instead), their words at lead size (up to 66ch) and any warning notice.
- **Groups** (`Section Grouped`): The prayer wall, Getting in touch, Notes for the pastoral team (each note its words over author and age, deleted from its own ⋯; the note field and *Add the note* in the foot), and What's happened.
- **Marking it answered** first asks in a sheet for optional words for the wall (up to 500 characters), shown under the request on the wall.

## Prayer desk settings (admin)
*Settings*, the desk's last chip, for the pastoral team and whoever may change prayer settings, sets when people hear that someone prayed for their request (owner's pick on the decision page, 2026-11-12). It replaces the search and list with the settings in the work area (up to 680px).
- **Head:** "When people hear someone prayed", then a lead.
- **Timeline:** one wrapping row of stops (never scrolling), each a dot over a tabular label: "On the wall" (an accent ring), each chosen time as an accent dot ("15m", "1h", "2d"), spaced by the square root of its gap so longer waits sit further apart, ending at "Then every day at 12pm" (an ink ring) when the daily time is on.
- **Changing it:** "A few times after it goes on the wall" over a hint and the chip picker of times, written out ("2 hours"); then *Then once a day* and, when on, *Each day at* (7am to 8:30pm by half hours, "Church time."). The `SaveBar` won't save with no time chosen; a quiet *Back to the usual times* shows when they differ.
- **Read only:** someone who may see but not change it gets the timeline and the notice "Only the pastoral team can change these."; everyone gets the closing line "People can turn these off in Notification settings."

## A prayer request's page (phone)
A solid top bar and no masthead. Under it, a full-width strong-sand band:
- **Meta line:** when it was sent, who it's from ("Sam S.") and an Under 18 pill (marker); on the person's own page, when it was sent and how the wall shows them.
- **Status:** for the pastoral team, the care steps (New, Followed up, Answered) as a pressable `StagePipeline`; for a wall approver, one strong line. The person sees their own request read only (Sent, On the wall, Answered; Sent and Answered when they didn't ask for the wall), with a ⋯ at the meta line's right holding Take it off the prayer wall and Delete. Until it has sent, the band shows its `SaveStatus` instead of the steps.
- **Then** their words at lead size, and inset groups: The prayer wall (the words as the wall shows them, the name as a segmented choice, *Shows until* as a date field; Save or *Put it on the wall* as the foot's main action, *Not for the wall* or *Take it off the wall* as its quiet one), Getting in touch, and Notes for the pastoral team.
- **Quiet buttons in a group's foot** keep the group colour as their fill, so they still read as buttons on the group ground.
- **The person's page** ends with a block *It was answered* button and a hint; it asks for a few words of thanks in a sheet.
