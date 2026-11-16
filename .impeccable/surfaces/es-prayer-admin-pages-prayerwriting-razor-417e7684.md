---
version: 1
slug: "es-prayer-admin-pages-prayerwriting-razor-417e7684"
primary_target: "src/Features/Prayer/GSBC.ChurchApp.Features.Prayer.Admin/Pages/PrayerWriting.razor"
related_targets: ["src/Features/Prayer/GSBC.ChurchApp.Features.Prayer.App/Pages/AskPage.razor"]
---

# Prayer: writing a request in the admin panel

Scope: Operate. `/prayer/write` in the admin panel, opened by *Write a request* at the top right of Prayer's title band;
the phone's *Ask for prayer* gains the church's name as a choice for the same staff.

- **Audience and job:** a prayer wall approver at a desk (UAT feedback #9, 2026-11-16) putting a prayer point on the wall:
  their own, one someone rang or emailed in, or one from the church.
- **Answers (owner, 2026-11-16, interview):** it goes straight onto the wall (or only to the pastoral team); the name is
  *My name*, *Someone else* (typed), *No name* or the church's name; any prayer wall staff can change, take down or
  delete a request from the church (or one written for someone else).
- **Chosen (owner, decision page 2026-11-16):** "A page to write on" (`.impeccable/mocks/prayer-write/page.html`).
- **The church's name** is a Prayer setting ("GSBC" to start), changed under Prayer → Settings.

## Direction contract

THESIS: writing a prayer point is writing, not filling a record: the words get a page of their own, the choices sit
beside them. Refuses squeezing a new request into the inbox's record pane.

OWN-WORLD: the admin bulletin world unchanged: title band with a way back to Prayer, `WritingDesk` (white page on sand,
the settings column on the right at 360px), field fills in sand, radio rows with a sentence each (`Segmented Rows`),
`Segmented` for who reads it, `DateField`, the wall's own card as the preview, navy primary.

STORY: the approver types the words, chooses whose name the wall shows, sees the card as people will, and puts it up.

FIRST VIEWPORT: title band "Write a prayer request"; the words field on the white page; the column: From (four rows),
Who reads it, Shows until, How the wall shows it; Cancel bottom left, *Put it on the wall* bottom right.

FORM: dealt structure 6 of 7 (a page to write on); seed:e0b3ba5b32592ed3.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
