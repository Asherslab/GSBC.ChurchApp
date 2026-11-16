---
version: 1
slug: "res-news-admin-pages-churchnewsdesk-razor-7a6ff27e"
primary_target: "src/Features/News/GSBC.ChurchApp.Features.News.Admin/Pages/ChurchNewsDesk.razor"
related_targets: ["src/Features/News/GSBC.ChurchApp.Features.News.App/Pages/NewsPage.razor"]
---

# Every ministry's news, for staff (admin desk group and the app's News tab)

Scope: the admin panel's Church news desk (`/news`) gains an *All ministries* group under its own rail heading, and
the app's News (`/news`) a third tab, *All ministries*, both for holders of `news.ministries` (*See every ministry's
news*, given to Church news Publishers). Mode: Operate. UAT feedback #11 and the owner's decision-page pick, 2026-11-16.

- **Who:** church staff who look after church news, going through what the ministries post; publishers may put a
  post in church news from there.
- **Interview (owner):** a new permission (Publisher role to start); only posts for everyone (no team, parents or
  hidden ministries' posts); read, open its ministry, and for publishers *Put it in church news*.
- **Untouched:** Sync (the phone reads the public ministry posts it already has), the desk's other groups, NewsList.

## Direction contract

Picks: surface round (operate), the owner chose "Rows like the rest of the desk" (`.impeccable/mocks/news-all/b-rows.html`).

THESIS: Every ministry's news is one more group of the desk, not a new screen: the same rows, the same pane, the same
pills. Refuses a dense table and per-ministry filters.

OWN-WORLD: The admin desk unchanged: sand rail with a second heading *Every ministry*, white list of `ListRow`s with a
thumbnail, the ministry named in accent, accent pill *In church news*, marker pill *Asking*; the post's bulletin item in
the record pane with its ⋯. On the phone the News masthead, tab strip with three tabs, `NewsList` with "From …" and the
state in marker ink.

STORY: Staff choose All ministries, scan who posted what, open one, read it, and if it's worth the whole church, choose
*Put it in church news…* and say whether to send a notification.

FIRST VIEWPORT: Title band "Church news"; rail: Show (six groups), then *Every ministry* with *All ministries 23*
chosen; the list of rows (title, ministry, by and when, pill, thumbnail); the chosen post in the pane with ⋯ (*Put it in
church news…*, *Open it in <ministry>*). Phone: the News masthead, tabs The church · Your ministries · All ministries,
the lead post and rows.

FORM: seed:36c3c3cc4fbea84c · rows, second dealt card. Signature: the ministry line in accent on every row, and the
pill that says where the post stands with church news.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
