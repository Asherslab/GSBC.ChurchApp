# Prayer

Detail behind [DESIGN.md](../../DESIGN.md) for the Prayer desk, its settings and a prayer request's page. Read DESIGN.md first; its rules win.

## Prayer desk (admin)
`/prayer`, a `Workbench` under the title band "Prayer". The rail: search ("Words or a name", hidden in Settings) over one `FilterChips Rail` group: New and For the wall (counted), On the wall, Answered, Everything, then Settings. The pastoral team sees all of them; a wall approver only the wall's. Each request in the list: its words cut to 70 characters over its pills (New, Asks for the wall in accent, Under 18 in marker), who it's from and how long ago.
- **Head:** a `PaneHead` titled by who sent it ("Sam S.", or no name), its lead when it was sent; Delete in its ⋯.
- **Body:** an Under 18 pill when it applies, the status as the care steps (`StagePipeline`; a wall approver gets one strong status line instead), their words at lead size (up to 66ch) and any warning notice.
- **Groups** (`Section Grouped`): The prayer wall, Notes for the pastoral team (each note its words over author and age, deleted from its own ⋯; the note field and *Add the note* in the foot), and History.
- **Marking it answered** first asks in a sheet for optional words for the wall (up to 500 characters), shown under the request on the wall.

## Writing a request (admin)
`/prayer/write`, from *Write a request* (a primary button with a plus) in the desk's title band. A `WritingDesk` (owner's pick, 2026-11-16, `.impeccable/mocks/prayer-write/`) under the title band "Write a prayer request" with a way back to Prayer and the lead "The request goes straight onto the prayer wall, or only to the pastoral team."; a failed send shows as a warning notice under it. Someone without prayer wall access gets an empty state instead ("Only prayer wall staff write requests here").
- **The page:** *Prayer request*, a tall multiline field (18 lines) at lead size, described "Up to 2,000 characters. The prayer wall shows these words exactly as written."; the Preview tab shows it on a phone's prayer wall.
- **The column** ("Who it's from and who reads it", folds to a summary like "From GSBC · on the wall"), as `Section Grouped` blocks: *Name on the prayer wall* as `Segmented Rows` (My name with "Shown as Sam S.", No name, the church's name with "From the church. Any prayer wall staff can change the request or take it down." (the same words as the phone, `PrayerWords.FromTheChurch`), Someone else last so *Their name* follows it); *Who reads the request* (`Segmented`, side by side: The prayer wall or Pastoral team) with *On the wall until* (`DateField`, tomorrow to a year ahead) under it while it's for the wall; and, also only for the wall, *How the prayer wall shows the request*, the wall's own `WallPostRow` card in its `Preview` mode, drawing the Pray pill without acting on it.
- **It starts** from the church's name, for the wall, showing for 30 days.
- **Foot:** Cancel, and *Put it on the wall* or *Send to the pastoral team*; the reason it can't send above. Once sent, the desk opens on the new request.

## Prayer desk settings (admin)
*Settings*, the desk's last chip, for the pastoral team and whoever may change prayer settings, sets when people hear that someone prayed for their request (owner's pick on the decision page, 2026-11-12). It replaces the search and list with the settings in the work area (up to 680px).
- **Head:** "When people hear someone prayed", then a lead.
- **Timeline:** one wrapping row of stops (never scrolling), each a dot over a tabular label: "On the wall" (an accent ring), each chosen time as an accent dot ("15m", "1h", "2d"), spaced by the square root of its gap so longer waits sit further apart, ending at "Then every day at 12pm" (an ink ring) when the daily time is on.
- **Changing it:** "A few times after a request goes on the wall" over a hint and the chip picker of times, written out ("2 hours"); then *Then once a day* and, when on, *Each day at* (7am to 8:30pm by half hours, "In the church's local time."). The `SaveBar` won't save with no time chosen; a quiet *Back to the usual times* shows when they differ. The page asks before losing changes.
- **Read only:** someone who may see but not change it gets the timeline and the notice "Only the pastoral team can change these."; everyone gets the closing line "People can turn these off in Notification settings."
- **The church's name on the prayer wall:** a `Section Grouped` under the times (lead: requests written from the church show this name, and changing it changes them on the wall too), one field and a Save in its foot; read only, the name and the notice "Only the pastoral team can change the church's name."

## A prayer request's page (phone)
A solid top bar and no masthead. Under it, a full-width strong-sand band:
- **Meta line:** when it was sent, who it's from ("Sam S.") and an Under 18 pill (marker); on the person's own page, when it was sent and how the wall shows them.
- **Status:** for the pastoral team, the care steps (New, Answered) as a pressable `StagePipeline`; for a wall approver, one strong line. The person sees their own request read only (Sent, On the wall, Answered; Sent and Answered when they didn't ask for the wall), with a ⋯ at the meta line's right holding Take it off the prayer wall (while it waits for the wall) and Delete. Until it has sent, the band shows its `SaveStatus` instead of the steps.
- **Then** their words at lead size, and inset groups: The prayer wall (the words as the wall shows them, the name as a segmented choice, *On the wall until* as a date field; Save or *Put the request on the wall* as the foot's main action, *Keep it off the wall* or *Take the request off the wall* as its quiet one) and Notes for the pastoral team.
- **Quiet buttons in a group's foot** keep the group colour as their fill, so they still read as buttons on the group ground.
- **The person's page** ends with a block button and a hint: *Remove from prayer wall* while it's on the wall (the sheet asks for optional thanks; its save reads *Remove now* or, with thanks, *Remove in 3 days*), otherwise *Mark as answered*; both ask for a few words of thanks in a sheet.
