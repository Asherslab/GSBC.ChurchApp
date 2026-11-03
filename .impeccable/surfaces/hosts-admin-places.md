---
version: 1
slug: "hosts-admin-places"
primary_target: "src/Hosts/GSBC.ChurchApp.Hosts.Admin/Pages/Places.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/Destinations","src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.Client/AnnouncementsDesk.razor"]
---

# Places in the app (admin panel)

Scope: the admin panel's Places in the app page (`/places`, `/places?at=<path>`), and the picker that reuses
its directory wherever staff choose what something opens (Announcements → Opens first; Home tiles and links in
rich text later). Mode: operate. Desktop first (1280 to 1920), light and dark.

Audience and job (owner, 2026-11-03): church office staff, not technical, who need to send people to a place in
the app without knowing its address: a page (Home, What's new, Notifications), or one thing in it (an event, a
file; sermons later). Owner's words: "a good overview for exploring and modifying specific things". From a place
they copy its link, announce it, or go and change it (Home in a draft, a file in Media). Search and browsing
by area count equally. Events, files and pages are what they link to most.

Content and ranges: Pages about 10 today (Home, Notifications, Notification settings, More, What's new, Help
improve the app, Appearance, Previews, Account); files 0 to a few hundred; events (when built) 10 to 40 coming
up. Each place has a name, an icon, its area, where it is in the app ("More → What's new"), who can open it
(Everyone, Everyone signed in, people with a permission), and for items a date. States: loading, nothing found,
no places in an area yet, a place someone can't open, a place that's gone.

Decisions: links are shared as `https://<app host>/open/<path>` (owner chose the app host, 2026-11-03); without
the app, that address shows an "Open in the app" page. The picker lists only what the announcement's audience
can open. Members' sharing waits (staff now, members later).

## Direction contract

THESIS: Places in the app is the app laid out on the desk: a searchable directory of every place, grouped by area,
beside a phone showing the chosen place, with what you can do with it under the phone. It refuses a bare link
list and a table of addresses.

OWN-WORLD: The admin bulletin desk: navy sidebar, white work area, title band; a 420px sand search field and the
shared filter chips with counts; the directory as hairline rows with 34px tiles (sand for pages, aqua for events
and files), the chosen row on Selected Fill; a phone frame at true size on the right; actions as capsules under
it (quiet Copy link and Announce it, primary for the place's own change action); who can open it as one caption.

STORY: I open Places, see Pages first, then what's coming up and what was added lately; I type "bible", choose the
study night, see it in the phone, copy its link for the bulletin or press Announce it and land on a new
announcement that already opens it.

FIRST VIEWPORT: Title band "Places in the app" with a one-line lead. A toolbar: search (420px) and area chips
(All, Pages, Events, Files with counts). Then two columns, 1fr and 400px with a 40px gap: the directory grouped
by area with uppercase micro group heads, and the phone column, sticky 24px from the top: the phone (scaled to
fit), the place's name and where it is, who can open it, then the actions. At 1280x800 the first six rows and
the whole phone show.

FORM: dealt lead, structure 4 of 7 ("Explore it beside a phone"), chosen by the owner 2026-11-03 after a steer
toward a more useful site directory; seed:bd8fa568041edd7e. Signature move: choosing a place puts it in the
phone; the phone draws it live where a feature can (Home through HomeScreen), otherwise a summary card in the
app's own style (masthead with the place's name and where it is).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
