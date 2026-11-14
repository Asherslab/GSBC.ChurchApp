---
version: 1
slug: "p-platform-ui-content-richtextfield-razor-0569e626"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/Content/RichTextField.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/News/PostEditor.razor","src/Platform/GSBC.ChurchApp.Platform.Ui/News/PostView.razor"]
---

# The one post editor and the reading page

Scope: the shared `RichTextField` (live Markdown on CodeMirror 6) with its bar and Insert, as a post's whole body in
`PostEditor` on the phone and in the admin panel's writing page; media shown in place in the editor; and `PostView`,
the post's reading page on the phone and in *Preview on a phone*. Mode: Operate (editor), Read (post page). Owner's
words and interview 2026-11-14 (docs/features/news.md, *One editor, everything in it*); #86, #87, #88.

- **Who:** church staff at a desk, ministry leaders on the phone; many don't know Markdown. Readers: everyone.
- **Interview (owner):** galleries as now; captions typed under the picture; headings as Markdown (# biggest).
- **Untouched:** the stored format's safety (no raw HTML, every save checked), Sync and the outbox, Access, the
  writing page's layout (settings column, save bar).

## Direction contract

Picks: bar round (operate) "Formatting in the bar, one Insert button for everything else" (owner's pick); post page
round unanswered, default "A quiet band when there's no picture" (owner to review).

THESIS: One editor that writes like a document. Formatting lives in one row of quiet icons; everything that goes *into*
the post is behind one navy Insert. Refuses an Add row of chips under the editor, block cards with handles, and a bar
of thirty icons.

OWN-WORLD: The admin panel and app unchanged: sand field for the editor, 36px quiet icon buttons (44px on the
keyboard), a text-style select at the left, hairline dividers between groups, the navy primary Insert at the right
(first on the phone). Menus are the shared `Menu` with group titles; sheets on the phone. Reading page: navy band in
place of a missing picture, sand bar between cards.

STORY: Staff type the title, pick Heading 1, type, press ⌘B, choose Insert → Picture, pick or upload in Media's window,
type a caption under the picture, add === for a second card, look at Preview on a phone, Post.

FIRST VIEWPORT: Writing page: Write / Preview tabs, Title, the bar (Text ▾ | B I S H | lists quote link code ⋯ ...
Insert), the editor showing formatted text. Phone post page: navy band with the title, who and when, the text.

FORM: seed:ca4abc76845a49bd · insert-button (dealt), with band post page (seed same, followup).
Signature: media in place in the text, each with its caption line and its library path (`~/Main Photos/x.jpg`).
Blocks (2026-11-14, #92 to #96): every block moves by its pill's handle with a pointer drag (mouse or finger) onto an aqua drop line between blocks, or Alt ↑ ↓ on the handle; one caption line for every block. A gallery's pictures each carry a ⋯ at their bottom right (on hover at a desk, always on the phone) opening the shared menu: Move earlier, Move later, Replace, Take this picture out (owner's pick on the decision page, `.impeccable/mocks/gallery-pictures/`, seed:c7d012e2e0addc82, option 3 dealt).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict and DESIGN.md
