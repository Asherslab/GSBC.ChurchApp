# Lists, rows and cards

Detail behind [DESIGN.md](../../DESIGN.md) for rows of every kind, boxed lists, facts, callouts, covers and cards. Read DESIGN.md first; its rules win.

## Lists and rows
A row is at least 56px tall: an optional leading tile or thumbnail, a title and optional subtitle, trailing metadata and a chevron (external-link icon when it leaves the app). Hairlines divide rows, one above the list; a pressed row fills with the hairline colour.
- **Leading tiles:** sand with a navy icon by default; aqua with deep aqua for actions; orange with orange-ink for warm items; sand with ink-3 for quiet ones.
- **Thumbnails:** 16:9 (sermon graphics are 1920 by 1080); a sermon in progress shows an orange progress track under the subtitle.
- **Agenda rows:** at least 44px, top-aligned, a tabular time column in primary. A special or one-off has an accent label line over its title; in the app's lists it sits on marker at 14%, its hairlines hidden. A regular row is quiet (no label, ink-2 title, ink-3 time); a regular event's date carries the repeat icon, spoken as how it repeats ("Repeats every Sunday"). A date that isn't on keeps its row in ink-3, title struck through, with a line under it led by the circle-off icon saying why ("Camp weekend · back on 20 November"), never a title suffix. Missing details: a caption in orange ink. Past rows: ink-3. Selected: the Tonal Selection. In a day section the hairline sits above each row.
- **Unread and read:** unread has an orange dot and a 700 title; read, no dot and 500; neither, 600.
- **Inbox:** the masthead note counts what needs you ("2 waiting for you · 3 new"). While something waits, a Waiting / Everything switch (Waiting with the count) tops the list. Waiting rows are reading rows on the warm tile with a deep-aqua action line ("Preview it", or "Do this in the admin panel") and a hint under the list. Everything: warm tiles for what still waits, quiet for the rest, an Accent "Test" pill on tests, and a quiet "Mark all as read" while anything is unread.
- **Hint:** one ink-2 sentence under a list about how it behaves.
- **Toggle rows:** title and caption beside a switch. A consequence changes the caption in place ("Only its own people and staff see it"), never adds a line. In the admin panel a captioned row's switch sits level with the title so it never moves (owner, 2026-11-13, #80); the phone centres it. **Muted** (`Muted`): a row whose thing is off keeps its place in ink-3, its caption saying what off means ("Hidden from everyone, leaders too, until you turn it back on. Nothing in it is deleted.").
- **Row menu** (`ListRow` `Actions`): DESIGN.md, *Pattern map*; it sits before the chevron, or at the right edge on a row that opens nothing.
- **Boxed list:** the same rows in a white, lifted panel.
- **Reading rows:** for entries you read, not tap: top-aligned, roomier, the subtitle at body size in ink up to 65ch; facts or a text link go below.
- **Title extra:** a slot after the title for a pill ("Staff only").
- **Fact list:** an ink-3 label over ink text, on hairlines. With an icon, a fill-on-surface tile with a navy icon. Plain facts use ink-2 and drop the first hairline, reading as a row's detail. A value may be a pill, date or link; an empty fact isn't shown. Admin: two to a line (one at 760px and under); in a settings group, label beside value above 760px, stacked below (#81).
- **Text link:** deep aqua with a chevron and a 44px target, for a "Show me" in a row.

## Cards / Containers
- **Callout:** sand, a bold lead line then text; the bulletin's shaded box ("Your challenge this week").
- **Sermon head:** a sand band with a 16:9 thumbnail, the title and "speaker · watch or listen", linking to the sermon.
- **Feature card:** the only picture card, for upcoming events: a 16:7 cover over a white body, lifted.
- **Covers:** images fill their box; sermon artwork is always 16:9 so its text is never cropped. No image: a navy (or aqua- or orange-tinted) block with the mark at 10% and an optional title.
- **Series cards:** a 16:9 cover, the name and message count, two to a row.
