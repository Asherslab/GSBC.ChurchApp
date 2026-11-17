---
version: 1
slug: "urchapp-hosts-admin-pages-openinapp-razor-fff84fd5"
primary_target: "src/Hosts/GSBC.ChurchApp.Hosts.Admin/Pages/OpenInApp.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/News/PostView.razor"]
---

# A link opened in a browser: `/open/{path}` on the app host (admin panel)

Scope: the open page as someone without the app meets it, from the What's Next email or a shared link, on a computer
(1440, 760) or a phone browser (390). Mode: Read. Owner, 2026-11-17: "if we're including post links in the email going out
-> posts need to open on desktop properly"; "should also be usable on mobile, for a person that does not have the app on
mobile, but opens the link on their mobile email client".

- **Interview (owner, 2026-11-17):** the app line keeps "Ask the church office" (no store links yet); cards in a post show
  and open the website's public page for that event or sermon (the embed host); anything not public shows nothing.
- **Post:** a church news post the reader may see shows in full, through `PostView` (no copy). Not for them or gone keeps
  today's card wording. Other places keep today's card. *See all events* (`/week`) goes on to the website's Events page.
- **Untouched:** the post's own bulletin-item look (`PostView`), the app, Sync, the sign-in stage.

## Direction contract

THESIS: The church's page for a link: a short navy band names Good Shepherd and what this is, then the post reads exactly
as the app sets it. Refuses an app-download card standing between the reader and the post.

OWN-WORLD: The Sunday Bulletin: navy (masthead) band with the white mark, "Good Shepherd" at 700 and a beige note at the
right ("Church news"); white page; `PostView`'s navy rule, sand date block, headline title and sand foot; the app line in
ink-2 caption inside the foot.

STORY: Tap *Read the post* in the email: see at once it's the church's, read the whole post with its pictures, and learn
at the end it's also in the app and how to get it.

FIRST VIEWPORT: Navy band across the window (16px 28px; 14px 20px on a phone). Under it the post in a 680px column
centred, 22px down: navy rule, date block beside the title, who; the opening picture; the lead. The sand foot runs to
the window's bottom across the full width, its words in the column: "Posted today by … for everyone." then the app line.

FORM: The navy band, 3rd of 7 on my ordered list (dealt card c). seed:3b2fe84af6986d60

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
