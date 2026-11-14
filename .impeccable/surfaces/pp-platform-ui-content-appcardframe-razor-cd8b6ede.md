---
version: 1
slug: "pp-platform-ui-content-appcardframe-razor-cd8b6ede"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/Content/AppCardFrame.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/Content/RichTextField.razor"]
---

# App cards in a post

Mode: Read (a card sits inside a post's reading column). Established world: The Sunday Bulletin (DESIGN.md).

## Job and audience
People reading a church news or ministry post on the phone (and staff previewing it at a desk) meet an event, a sermon, a
ministry or a Life Group named in the text, and act on it without leaving: add the event to their calendar, open it,
open the sermon, join or ask to join. Writers put them in from Insert and see the card itself while writing.

## Decisions (owner, 2026-11-14)
- All three sketches ship as looks the writer chooses per card: Side rule (default), Timetable line, Box
  (`.impeccable/mocks/app-embeds/`). Stored as `look=` on the directive line; Side rule writes nothing.
- A post may be for more people than can see a card: it saves, and the editor says so under the card.
- A regular event: the writer chooses one date, the next date rolling on, or the pattern.
- A sermon card opens the sermon (no play in place); it shows progress when started.

## Direction contract
THESIS: an app thing named in a post is drawn as the bulletin would set it, in three registers of presence the writer
picks; it refuses the generic "link preview" box with a favicon and a grey summary.
OWN-WORLD: navy, deep aqua and sand of the bulletin; Montserrat at row and caption sizes; the date block, the 4px kind
rule (the owner's pick overrides the floor's border-left default), sand box at the 14px radius, 16:7 cover.
STORY: the reader sees what it is and when, sees if it changed or was cancelled, and does the one thing it asks.
FIRST VIEWPORT: inside a post: title, a meta line with the kind's icon and date under it, details lines, then actions;
Side rule with its picture square at the right, Line with the date block or artwork at the left, Box with the cover on top.
FORM: three looks of one frame (`AppCardFrame`), chosen per card. seed: 9f7adf60ec4befdf (surface roll, dealt 6, 5, 1).
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## States
Changed (marker-ink note), cancelled (title struck, danger line), over (quiet "This was on …" line), gone or not
visible: nothing for readers; in the editor a quiet "isn't in the app any more" card with Take it out. A finished
sermon says Heard. Writers see "Only people who can see this … will see its card." when the item isn't for everyone.

## Constraints
Phone reads only the synced store (Sync rule); the admin panel reads online. No kicker above the title (floor ban): the
kind and date sit on a meta line under it. Platform.Ui draws; features fill it.
