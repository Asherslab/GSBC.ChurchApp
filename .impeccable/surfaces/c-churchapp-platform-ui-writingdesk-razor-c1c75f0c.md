---
version: 1
slug: "c-churchapp-platform-ui-writingdesk-razor-c1c75f0c"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/WritingDesk.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/Content/RichTextField.razor","src/Features/News/GSBC.ChurchApp.Features.News.Admin/Pages/ChurchNewsWriting.razor","src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Admin/Pages/MinistryPostWriting.razor"]
---

# Writing a post as a full page (admin) and the shared text editor

Scope: the admin panel's page for writing or changing a post (church news `/news/write`, `/news/{id}/change`; a
ministry's `/ministries/{id}/news/write`, `/ministries/{id}/news/{post}/change`), the shared `WritingDesk` layout, and
the shared `RichTextField` (live Markdown) used by every post's text blocks on the phone and the desk. Mode: Operate.
Owner's feedback #83, #84, #85 and the interview and two decision-page picks, 2026-11-13.

- **Who:** church staff (Church news Writer and Publisher) at a desk, ministry leaders; the same editor on the phone
  for leaders who never open a computer. Many don't know Markdown.
- **Interview (owner):** the phone preview is a tab, "like is often for markdown editors"; events' About this date
  waits ("we'll discuss that later"); the phone keeps its full-screen writing page. Later: the editor becomes the
  standard for formatted text (a ministry's About, event descriptions), phone first.
- **Untouched:** the stored format (restricted Markdown, no raw HTML), Sync and the outbox, Access, the desk as the list.

## Direction contract

Picks: on the surface round (operate) the owner chose "The post as a page, its settings beside it" with the steer
"allow collapsing settings", then text editing (c) "Live Markdown, marks fade away" on round 2)

THESIS: Writing is a page, not a pane. The post sits as a white page on sand at a reading width, its settings open in a
column beside it that folds away; text formats as you type, the marks hiding unless the cursor is in them. Refuses a
cramped side pane, a Markdown box with symbols and a "How it looks" preview.

OWN-WORLD: The admin panel unchanged: navy sidebar, title band with Back, sand ground, white 14px page with a hairline
and lift, grouped setting cards, the save bar flush at the foot. Format bar of 36px icon buttons, navy when on.

STORY: Staff open New post, type the title and the text, press B or ⌘B, paste from Word with its bold and lists kept,
add a gallery, check who and when in the column (or fold it and read the summary beside the tabs), look at Preview on
a phone, Post.

FIRST VIEWPORT: Title band "New post" with ‹ Church news and, once the post exists, the ⋯ menu (a new post has nothing in it to do: no delete, no back to writing); under it the sand desk: the page (Write and
Preview on a phone tabs, the title in headline type, the first text block with its bar, the Add row) and the 360px
settings column (its head with the fold arrow, Who it's for, Telling people, When, A link); Cancel bottom left and Post
bottom right in the column's foot.

FORM: seed:5b4558f5c4aad0a8 · page-and-side, third on the ordered list, plus fold; text as live Markdown on CodeMirror 6 (our own mark hiding).
Signature: the marks that fade away, and the settings column that folds into a one-line summary.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
