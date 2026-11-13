# News

Status: interviewed, decided on the decision page and built 2026-11-13 (`feature/church-news`).

News for the whole church, not under a ministry, in its own place in the app and the admin panel; ministry posts put into
it; and one post editor and renderer, with full formatting and media, for church news and ministry news alike. The owner,
2026-11-13, the last piece before UAT: "News, currently only a feature under ministries. i also want an app wide News tab
specifically for the church as a whole (i.e. not under a ministry), it should allow (by staff) posting a news post from a
ministry's news to the church wide one if wanted. news will be expanded to allow full formatting and media to be
embedded/etc in them".

## The interview (owner, 2026-11-13)

1. **Church news replaces announcements.** "announcements was a way to send a pure notification. now news posts should be
   used for those, notifications center and everything still exists, but now what would have been an announcement with no
   link to anything, will now just be a news post, with a notification sent out linking to the news post." The
   notifications list, push, quiet hours and preferences stay as they are; what staff send is a church news post, and
   posting one can send a notification that opens it.
2. **The Announcements page goes** (owner's pick: "Page goes; posts link out"). Church news takes its place in the admin
   panel's sidebar. A post can carry a link to something else (an event, a sermon); its notification opens the post.
   Announcements already sent stay as read-only history, and those can still be recalled.
3. **A post's notification is on by default.** Its title and line come from the post and can be changed, it can be
   urgent (skipping quiet hours), it goes when the post is published (now or at a scheduled time), and it can be tried on
   your own phone first. Deleting a post recalls its notification.
4. **Service cancellations are urgent on the day** (owner, in the same answer: "service cancellations should be urgent if
   it's same day, always").
5. **Each post chooses who it's for**, Everyone by default: everyone (signed out too), everyone signed in, or people in
   chosen roles, with the audience picker Home's tiles and announcements use.
6. **Video and audio, linked or uploaded.** A YouTube or Vimeo link shows as a player (it needs the internet to play); a
   video or audio file can be uploaded into the Media library. **Up to 100 MB for anyone who can post; up to 2 GB with a new
   role for staff** ("allow a certain role that allows larger upload sizes for longer videos and such, nothing crazy, but
   staff can be more responsible with those things"). **Larger videos download on demand and don't stay offline**
   (anything over 25 MB plays from the internet when tapped); smaller ones are kept on the phone like pictures.
7. **Leaders ask, staff share.** A ministry post's *Ask to send it to the whole church* becomes *Ask for church news*; staff
   who publish church news see the asks and share a post with one tap, or say no. They can also share any for-everyone post
   without an ask.
8. **The News page** (decision page, 2026-11-13; the owner re-rolled the first hand with a steer): "pictures lead like
   front page for top post, other post move to image on side. include tab for your ministries (combined feed), hide tabs if
   person has no ministries". The newest post leads with its picture; the rest are rows with their picture at the side; a
   *Your ministries* tab gathers the news of every ministry you're in, and the tabs aren't shown to someone in no ministry.
   **Picked** (the owner's pick on the re-rolled hand): *What's urgent today first, then the lead*. A post sent as urgent
   today sits above everything on a marker band; then the newest post leads with its picture; then rows with the
   picture at the right (`.impeccable/mocks/church-news/r-c-today-first.html`).
9. **Reading one post** (owner's pick on the decision page, 2026-11-13: *Its pictures and videos lead, as a strip*): the
   post's pictures and videos lead at the top as one swipeable strip (1 of 5); then the title, who posted it and when,
   and the text, where each still shows in place (`.impeccable/mocks/church-news/p-a-strip.html`).
10. **Writing a post** (owner's pick on the decision page, 2026-11-13: *The post as blocks you add and move*): the post is a
    list of blocks (text, a heading, a gallery with its caption, a video, a file), each moved by its handle or from its ⋯
    menu, with an Add row of Text, Picture, Video, Audio and File; who it's for, the notification and when sit on one row
    that opens their settings (`.impeccable/mocks/church-news/e-b-blocks.html`). The admin panel uses the same editor.
    Replaced on 2026-11-14 by one editor (*One editor, everything in it*, below).

## Decided while building (2026-11-13)

- **A strip needs two.** A post's strip leads only when it holds two or more pictures or videos; with one, it shows in
  place in the text. This is the risk the owner's chosen card named ("a post with one picture shows it twice").
- **Who may put a ministry's post in church news:** only a post for everyone, in a ministry that isn't hidden or
  archived and has News on (*default*). A post that stops qualifying (its audience narrowed, the ministry hidden,
  archived, moved under a hidden one, or News turned off) is taken out of church news by itself within five minutes;
  narrowing it in its sheet or deleting it takes it out at once.
- **Edits flow through without copying by hand.** Church news keeps a copy of a shared post that the ministry
  refreshes on every save (`IChurchNews.ChangedAsync`); the ministry's post is the only one anyone edits. Taking it off
  church news leaves the ministry's post; deleting the ministry's post takes it out of both.
- **Old asks are carried over.** A post that had asked "to send to the whole church" before this becomes an ask for
  church news the first time the ministries' feeds are rebuilt.
- **Pictures in church news are public files** (unguessable addresses), like Home's tiles; a ministry post for fewer
  people keeps its pictures private to its audience. A picture added while a ministry post was for fewer people can't
  go in a post for everyone (the server says to add it again); the phone and the desk upload only when the post is
  saved, so changing who it's for first is enough.
- **Notification kind:** church news posts use the announcements' kind key, so everyone's on/off choice for
  announcements carries over, now named *Church news*. Its line is the start of the post unless the writer types one.
- **Ministry posts' important notifications** still open the ministry's News tab; a ministry post has its own page on
  the phone (`/ministries/{id}/news/{post}`) one tap from there. A post can't be a place of its own in the app yet.
- **News is a starting tab** (Home, News, Sermons, More); staff move or remove it under App styling → Tab bar.

## Writing on a page, and formatting as you type (owner, 2026-11-13)

Feedback #83, #84 and #85 on the admin panel's *New post*, with an interview and two rounds on the decision page.

- **Writing is a full page in the admin panel** (#83: "make new post a full page thing, not a sidebar"). *New post*
  and opening any post you can change on the Church news desk open `/news/write` or `/news/{id}/change`; a
  ministry's *New post*, changing its post and *Tell the group about a Sunday* open
  `/ministries/{id}/news/write` or `/ministries/{id}/news/{post}/change`. The desk stays the list; ministries' asks
  and shared posts still open in its pane, where they're answered rather than written.
- **The post as a page, its settings beside it, and they fold away** (owner's pick on the decision page, with the
  steer "allow collapsing settings"; `.impeccable/mocks/post-editor/b-page-and-side.html`). Folded, the settings sum
  up in one line beside the tabs; under 1228px they sit under the post. Shared as `WritingDesk` (Platform.Ui). A new
  post has no ⋯ menu in its title band: there is nothing to delete or take back to writing until it's saved.
- **Preview on a phone is a tab** beside Write (owner: "tab it, like is often for markdown editors").
- **Text formats as you type: live Markdown** (owner's pick of three on the decision page; "c is fine most likely,
  but the important part there is it needs to be usable by staff that don't know markdown"). Bold looks bold and
  lists look like lists; the marks show faintly only where the cursor is. The bar and ⌘B, ⌘I and ⌘K do everything,
  typing `- `, `# ` or `> ` starts a list, heading or quote for those who know, and pasting from Word or Google Docs
  keeps bold, italic, lists and links (everything else becomes plain text, and pasted symbols stay symbols). The
  stored format is unchanged, so every existing post still works, and the server checks every save as before.
  Built as `RichTextField` (Platform.Ui) on CodeMirror 6, vendored by `tools/editor` and loaded only when an editor
  opens (about 100 KB gzipped). No maintained library hides the marks, so that part is ours (`rich-text-field.js`).
- **One standard editor** (owner: "this editor ... will likely be used in more places, so will need to be a
  standardised component ... especially needs to be mobile ready"). `RichTextField` is the shared component for
  formatted text, with `Headings` for places that allow them; posts use it inside their blocks, which add pictures,
  video, audio and files. On the phone its bar sits on the keyboard, with 44px buttons.
- **The phone keeps its full-screen writing page and sheet** (owner, in the interview), with the new editor.
- **Not moved yet:** events' *About* and *About this date*, a ministry's *About* and the app's formatted settings keep
  `RichTextEditor` (the Markdown box with its preview) until the owner decides (owner: "we'll discuss that later").
  Moving one is a swap of the component.
- **Icons centred, blocks rounded** (#84, #85): the bar's buttons are drawn icons centred in 36px squares; blocks
  take the 14px radius, shaded only while you're in them, with no field drawn inside the text block.

## A News tile on Home (2026-11-13)

Owner, 2026-11-13: "do a small impeccable design pass for a proper news tile for the hub. not sure if it'll work or look
good, but it might". The decision page (`.impeccable/mocks/news-tile/`) put four directions on dev's Home: a front page in
miniature, quiet until it matters, headlines, and no tile at all. The owner: "i love all three of A, B, and C, give me all
three as variants/layouts of the tile". They are the tile's layouts (Front page by default, What's new, Headlines); see
[home.md](home.md), *Tiles*.

## One editor, everything in it (owner, 2026-11-14)

The owner, with a screenshot of the block editor: "the add row doesn't feel a part of the editor, it feels like a
different other thing ... i'd like all this to be done into the actual editor itself ... i don't actually see any
reason that the extra cards on a post are needed anymore, just a title and proper markdown that lets you embed
images/etc into it." Also: "photo embeds (use proper media upload/management component modal) ... same for videos.
offer a different icon for an 'external' video ... same for files. audio we can skip", and "a decent amount of
markdown ... plenty of it could be under a 'extras' dropdown".

- **A post is a title and one `RichTextField`.** The blocks, their handles and the Add row are gone (`PostBlocks` and
  `PostTextBlock` removed). Nothing needed converting: the stored Markdown already held every picture, gallery, video,
  file and video link, and they now show in place. Audio in existing posts still plays; it isn't offered.
- **The bar: formatting in it, one Insert button for everything else** (the owner's pick on the decision page,
  `.impeccable/mocks/markdown-editor/b-insert-button.html`). A text style menu (Text, Heading 1, 2, 3), bold, italic,
  strikethrough, highlight, bulleted and numbered lists, quote, link, code, and ⋯ for superscript and subscript; at
  the right a navy **Insert** whose menu has *Pictures and media* (Picture, Video, Video link, File), *Layout* (Split
  into cards, Line across, Table, Code block) and *In the text* (Footnote, Emoji). On the phone Insert comes first in
  the bar, which slides sideways and sits on the keyboard, and opens a sheet of tiles; the style and ⋯ menus are
  sheets too. Shortcuts: ⌘B, ⌘I, ⌘K, ⌘⇧X, ⌘⇧H, ⌘E, ⌘⌥0–3, ⌘⇧7, ⌘⇧8, ⌘⇧9.
- **Interview (owner, 2026-11-14):** pictures chosen together sit as a gallery, as before ("A gallery, as now"); a
  caption is typed right under its picture or video; headings work "the way that standard markdown works, more #
  makes it smaller" (`#` is the biggest heading in a post, below its title).
- **Pictures, videos and files come from Media's own window** (*Choose a picture*, *a video*, *a file*, with
  Upload), the library on the desk and on the phone (online, as approved); on the phone Upload takes a photo from the
  phone and it waits in the outbox with the save. A ministry post for its team uploads privately to the team, as
  before. In the editor each shows in place (the picture, a video tile, a file row) with its caption under it, a
  handle to drag it, *Add a picture beside it* (a gallery), *Replace* and *Take it out*; cut and paste moves it too.
- **No ids in sight** (owner, 2026-11-14: "i don't want the media links to look super ugly in markdown ... handle that
  invisibly ... something like ~/Main Photos/church-ministry.jpg"). The stored line keeps `media:<id>`, and the editor
  never shows it: each file reads as its place in the library (`~/Main Photos/church-ministry.jpg`, from `MediaRef.Folder`,
  set when it's chosen; files chosen before this show `~/name`).
- **Split into cards** (owner, 2026-11-14: "we could use something similar to horizontal rule to specify a 'card split'
  instead, e.g. `===`"): `===` on a line of its own where a block starts. Under a line of text it stays text (never a
  setext heading). On the post page each part is separated by a sand bar across the page; a notification's line and a
  list's excerpt come from the first part with text.
- **The post page** (feedback #88, "incredibly barren looking"): *default (owner to review)*, as the owner went to bed
  before answering the second question on the decision page. Built **A quiet band when there's no picture**
  (`.impeccable/mocks/markdown-editor/p-a-band.html`): with no strip leading, the title sits on a navy band that
  continues the top bar; then who and when, the text with real paragraph spacing and type scale, and the sand bar
  between cards. The alternatives were *A reading page* (type only, a larger lead paragraph, dots between cards) and
  *Each part a card* (the post on sand, one white card per part). Chosen because it gives the picture-less posts the
  presence the owner asked for while matching the picture-led posts they already chose. The admin panel's *Preview
  on a phone* shows the same.
- **Not yet:** the owner's app-specific embeds (events, sermons, ministries, Life Groups) are the next round; their
  spelling is reserved (*rich-text.md*). Emoji show as boxes in the dev Simulator only (it has no emoji font); phones
  show them.

## How it's built

- **News feature** (schema `news`): `posts` holds church news and ministries' posts in it (`source`, `source_id`).
  States: Draft, Waiting to publish (a writer asked), Scheduled, Published, Asked (a ministry asked), Declined (kept in
  the ministry). `NewsDesk` does the work; `NewsSchedule` publishes due posts each minute, claiming each with one update
  so two API servers never publish or notify twice. Deleting a post recalls its notification
  (`INotifications.RecallAsync(subject)`).
- **Feeds:** `news.posts` (everyone, published for everyone), `news.posts.private` (each row's audience), `news.desk`
  (holders of `news.write`). The phone reads them (`StoredNews`); the admin panel reads the desk online (`OnlineNews`).
- **Permissions and roles:** `news.write` and `news.publish`. *Church news Writer* and *Church news Publisher* replace
  the Announcements Writer and Sender (their holders carry over) and keep seeing earlier announcements.
- **Ministries** reach it through `IChurchNews` (ask, share, changed, withdraw) and hear answers through
  `ISharedPostSource` (Platform.Api). Leaders' *Ask for church news* is in the post's settings; church news publishers
  put a post in or take it off from its ⋯ menu (`Leading.ShareToChurchNews`, `TakeOffChurchNews`).
- **Writing a post:** `PostEditor` (Platform.Ui) is a title and a `RichTextField` for both features; `PostView`,
  `MediaStrip`, `NewsList` and `NewsStory` read the same rich text. Files
  chosen on the phone wait in the outbox with the save and are named `media:upload-<sha256>` until the server swaps in
  the library's file (`UploadedMedia`); at the desk they upload when the post is saved.
- **Your ministries** comes from the Ministries store through `INewsSource` (Platform.Client); the tab isn't shown to
  someone in no ministry.
- **Tests:** `ChurchNewsTests` and `SharedMinistryPostTests` (News.Api.Tests), the ministry side and the plain-post
  migration in Ministries.Api.Tests, the rich text tests (`RichTextTests`, `RichTextFormatTests`, Platform.Contracts.Tests),
  `CancellationUrgencyTests` (Events).

## Not yet

- The admin panel's office week doesn't show scheduled church news yet (earlier announcements still show there).
- Vimeo videos open in the browser rather than playing in the post; YouTube plays in place.
- **Offline:** the newest 30 posts' pictures, video and audio are fetched to the phone in the background, within the
  phone cache's limits: a file over 5 MB is fetched when it's first played or shown, not before (stricter than the 25 MB
  the owner allowed; raising it means changing the native cache's limit for every feature).
