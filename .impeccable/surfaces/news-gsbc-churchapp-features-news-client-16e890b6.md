---
version: 1
slug: "news-gsbc-churchapp-features-news-client-16e890b6"
primary_target: "src/Features/News/GSBC.ChurchApp.Features.News.Client"
related_targets: ["src/Features/News/GSBC.ChurchApp.Features.News.App","src/Features/News/GSBC.ChurchApp.Features.News.Admin","src/Platform/GSBC.ChurchApp.Platform.Ui/News"]
---

# News: church news and ministry news (phone and admin panel)

Scope: the phone's News tab (`/news`, `/news/yours`), one post's page (church `/news/{id}`, ministry
`/ministries/{id}/news/{post}`), writing a post (church and ministry, one shared block editor), and the admin panel's
Church news desk (`/news`), which replaces Announcements. Modes: Read (News, a post), Operate (writing, the desk).
Interview and three decision-page picks by the owner, 2026-11-13 (docs/features/news.md).

- **Who:** everyone reads (signed out too); ministry members read their ministries' news; church staff (Church news
  Writer and Publisher) and ministry leaders write, mostly on the phone, sometimes at a desk.
- **Content:** titles up to 120 characters; text with headings, lists, quotes, links; pictures with captions,
  galleries, uploaded video and audio (100 MB, 2 GB with Large uploads), YouTube or Vimeo links, files; a link button.
  A post's notification is on by default and can be urgent. Range: 0 to hundreds of posts; most have one picture.
- **Untouched:** Sync and the outbox (reads from the store, writes queued); Access decides who sees and does; the
  rich text format (restricted Markdown, safe rendering).

## Direction contract

seed: f42e806a1b28652396dd057f13a01e8d (surface, read; re-rolled with the owner's steer, "today first" chosen);
504ef5f942f33e230f352c829edf0dad (read, "strip" chosen); 926aacf6ed93486eae0982a701fa96f6 (operate, "blocks" chosen)

THESIS: The church's news as its own front page. What's urgent today first on a marker band, the newest post leading
with its picture, the rest as rows with the picture at the side; one post leads with its pictures and videos as a
strip; writing is a column of blocks you add and move. Refuses a feed of identical cards and a Markdown box with
symbols showing.

OWN-WORLD: The app's Sunday Bulletin world unchanged: navy masthead on the tab root, white page, hairline-divided
rows, sand only where the bulletin would shade, marker for today's urgency, deep aqua for "From a ministry" and
links. The admin panel's Workbench, TitleBand, PaneHead and FilterChips Rail.

STORY: Open News: today's urgent notice, then the lead story, then the rest; switch to Your ministries; open a post:
swipe its pictures, read, follow its link. A leader writes: title, add text, add a gallery from the phone, caption
it, move it up, check who and when on the settings row, Post. Staff answer a ministry's ask from the post or the desk.

FIRST VIEWPORT: Phone: masthead "News", tabs (The church, Your ministries), the marker band, the lead's 16:9
picture and title. Post: back to News, the strip with "1 of 5" and dots, title, From and date. Editor: title, the
first blocks, the Add row, the settings row, Cancel and Post at the foot.

SIGNATURE: the strip that leads a post, and the block column with its Add row.

GUARDRAILS: Pattern map components (Masthead, TabStrip, ListGroup/ListRow, Sheet, SaveBar + UnsavedGuard, Menu,
ReorderList for moving blocks, DropBox/FilePick for files, PictureViewer, Workbench, PaneHead, FilterChips Rail);
Delete in the item's ⋯ menu then a Sheet; no coloured left borders; tokens only for spacing; light and dark; the
phone at 375 to 430 points, the admin panel at 1440 and 1360 and under.
