---
version: 1
slug: "churchapp-platform-ui-news-postview-razor-74054d19"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/News/PostView.razor"
related_targets: ["src/Features/News/GSBC.ChurchApp.Features.News.App/Pages/NewsPostPage.razor","src/Platform/GSBC.ChurchApp.Platform.Ui/News/MediaStrip.razor"]
---

# A post's page: reading one church or ministry post (phone, and the admin panel's preview)

Scope: `PostView` as people read it: church news `/news/{id}`, a ministry's post `/ministries/{id}/news/{post}`, both
*Preview on a phone* panes and the desk's record pane. Mode: Read. Owner, 2026-11-14 (feedback #88 "incredibly barren
looking"; "still seems a little barren, it's missing something, idk").

- **Interview (owner, 2026-11-14):** a post should feel like *a noticeboard item*; real posts are all kinds (short
  notices with no picture, one picture and a few paragraphs, long updates, posts pointing at an event or sermon); the
  reading app they like is *Apple News*. Nothing more at the end of a post; no masthead of who it's from.
- **What was missing** (critique on dev, 2026-11-14): a short notice was two-thirds empty white with no end; picture and
  picture-less posts looked like different apps; the head was timid ("Saturday 14 November" for today); the first
  paragraph looked like the rest; a post leading with several pictures showed them twice.
- **Untouched:** Sync (reads from the store), the rich text format and its renderer's blocks, app cards, the editor.

## Direction contract

THESIS: Each post is an item in the church's bulletin: a navy rule opens it, a date block sits beside a confident title,
the first paragraph carries the notice, and a sand foot closes it. Refuses a blank page under a short notice and two
looks for picture and picture-less posts.

OWN-WORLD: The Sunday Bulletin world: navy (primary) 3px rule, sand date block at the 12px radius (day word in accent
caps at pill size, the day number at 28px 700, the month in ink-2 caps), headline-size title at 700, caption meta,
white reading column, sand foot to the bottom of the screen.

STORY: Open a post from News, Home or a notification: see at once it's today's, what it is and who it's from, read the
lead paragraph and act on it (its card, its link); the sand foot says it's finished and who posted it for whom.

FIRST VIEWPORT: Solid top bar (‹ News, ⋯; its title appears only once the post's title scrolls under it); 22px down the navy rule across the column; under it the date block (62px) at
the left, which is the when, and the title beside it with who under it (amended after the finish review: the long
date under the title repeated the block and wrapped); then the opening picture, gallery strip or the lead
paragraph at lead size; the text follows; on a short post the sand foot fills the rest of the screen.

FORM: The bulletin item, 4th of 7 on my ordered list (THE ROLL). seed:f6ee390dfbcdb703

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
