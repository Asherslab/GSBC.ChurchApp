# App tabs: the tab bar the church chooses

The app's tab bar is chosen by staff in the admin panel, in a draft, the way they arrange Home
([home.md](home.md)), and published to phones without a release. Asked for by the owner on 2026-11-08
while designing Events ([events.md](events.md), decision 34): whether the calendar is a tab is the
church's choice. Built on Configuration's surfaces and drafts ([drafts.md](../architecture/drafts.md));
there is no store of its own. Until staff publish a change, the tabs are Home, News, Sermons and More (News joined the starting tabs on 2026-11-13 with church news, [news.md](news.md)).

## What people can do

| Who | Can |
|---|---|
| Everyone (the phone) | Use the tab bar as published: Home, up to three chosen tabs and More, each opening its place; offline and live |
| `config.tabs.edit` | In a draft: add tabs, choose the place each opens, its word and icon, move them, choose who sees each and when, take them off the bar |
| `config.tabs.publish` | Publish the draft, or turn it on for an event |

The *App Publisher* role (`config.*`) holds both; *App Editor* holds edit (2026-11-09).

## Decided (owner, 2026-11-08)

1. **Any place can be a tab.** A tab is a link to a place in the app ([destinations.md](../architecture/destinations.md)),
   like any other link: a page (Home, Sermons, the calendar's week), or one thing (a sermon series).
2. **Five tabs at most, More always last and always there.** Staff choose four. *Changed 2026-11-08 by decision 5:*
   Home is the first of the five, so staff choose three.
3. **Who sees which tabs can vary,** as Home's tiles do: everyone, people signed in, or people in roles, checked through
   Access ([permissions.md](../architecture/permissions.md)).
4. **Taking a tab off the bar removes a link, not a page.** The page is still there and still reached from its other
   links (Home's tiles, More, search, notifications). A phone that is on that page when the change arrives stays on it;
   only the tab goes. Places that aren't tabs are listed in More, as now.
5. **The app always opens on Home, and Home is always the first tab** (owner, 2026-11-08: "always home, and home must
   always be a tab"). Like More, Home can't be taken off the bar, moved, dated, given to some people only, or made to
   open somewhere else; staff choose the tabs between Home and More. This settles the open question of opening on the
   first tab: the first tab is always Home.

## How it works

### Tabs are placements

The tab bar is a surface area, `app.tabs` (*Tab bar*), in Platform.Contracts (`TabBarSurfaces`). Each tab is one
placement with three settings, the same for every tab (`TabSettings`): **Opens** (a place in the app), **Word** (the
label under the icon, up to 12 characters, so it fits a fifth of a phone's width) and **Icon**. Its order, who sees
it, its dates and whether it's shown are the placement's own, as for a tile. Drafts, the event layer, previews on a
phone, history and undo all work on it unchanged.

- **The two tabs the app starts with** are surfaces that exist without a placement (`app.tab.home` opening `/`, and
  `app.tab.sermons` opening `/sermons`), so a phone with nothing published draws Home, Sermons and More, as before.
  Staff change Sermons like any other tab; taking it off the bar hides it, and *Put back* shows it again.
- **Home is fixed** (decision 5). `TabBar.For` always draws Home first, from its own defaults, whatever is stored for
  it, then up to three of the rest (`TabBar.Chosen`); a tab that opens Home again is dropped. On the server,
  `PlacementRules` refuses any Home that isn't Home as it always is (on the bar, for everyone, no dates, its own place,
  word and icon), refuses another tab that opens Home, and keeps every other tab's order after Home's. A draft or
  published tab bar saved before this (Home hidden, moved or changed) still loads with Home first, and such a draft
  still publishes: its change to Home is left out (tested in `TabBarTests` and `TabBarDraftTests`).
- **Every tab added after** is a repeatable `app.tab` placement.

### What a tab may open

A place in the app that opens inside the app: a page or one thing of a kind. Not a web page, not More itself (it is
always last), and not a kind that opens outside (a file opens in the browser, which isn't somewhere to *be*). The
server refuses anything else on save (`PlacementRules`, through `DestinationCatalog`), and the picker only offers
places everyone the tab is for can open, as for an announcement (`OpenableBy`).

### On the phone

- **The tab bar** (`AppTabBar`, Platform.Ui) asks `ISurfaceLayout` for the area, as Home does, and draws Home, then
  the first three other tabs shown to this person in order (`TabBar.For`, Platform.Contracts), then More. Fewer is fine.
- **More** lists every place the app's modules list (Account, Notification settings, What's new, Calendar …) that isn't
  a tab for this person right now. Taking Sermons off the bar puts Sermons into More; making the calendar a tab takes
  it out. `NavPlacement` is gone: a feature no longer says whether it's a tab.
- **The chosen tab** is the one whose place you're on, or under: Sermons stays chosen on a sermon, as before
  (`NavEntry.AlsoUnder`, read for the entry at the tab's place). Tapping the chosen tab scrolls to the top.
- **It follows changes live.** The bar redraws when `ISurfaceLayout` changes: a publish, an event layer starting or
  ending, a draft being previewed or edited, signing in or out, a role given. It never navigates: a tab that goes away
  leaves you where you are.
- **Offline:** the tabs travel in the public placements feed, so the bar is drawn from the phone's own copy.

### In the admin panel

App styling → **Tab bar** (in a draft; it was Drafts → a draft → Tab bar until 2026-11-09), desktop first, built from the Hub canvas's parts (*The phone is the canvas*, chosen on
`.impeccable/mocks/decision/app-tabs/`):

- **The phone** draws the draft's Home with the draft's tab bar. Click a tab to choose it (the orange marker ring);
  Home and More are drawn faded, as they can't be chosen.
- **Beside it,** a rail (`TabsCanvas`): *Add a tab*, *Preview the draft as* (a visitor, someone signed in, someone in a
  role a tab is for), *Preview the draft on* (a date, to see dated tabs before they start), then **On the bar, in order**: Home
  first with a lock and "Always first", each chosen tab numbered, with an orange dot when this draft changed it
  and *Move left* and *Move right*, and More last with a lock and "Always last". On the phone, Home and More are drawn
  faded, as they can't be chosen. Under it, the tabs that aren't on the
  bar for that person, each with why: off the bar, dated, for someone else, not finished, another tab already opens
  that place (or it opens Home), or past the third; and tabs taken off in this draft, with *Put back*.
- **The settings panel** for the chosen tab (`TabSettingsPanel`): Opens (the place field, offering only places a tab
  can open that no other tab already opens), its word and its icon (filled in from the place when one is chosen, then the staff's to change), who
  sees it and when, *On the bar*, and *Take it off the bar*. A tab added in this draft says what it still needs and
  joins the bar once it has a place, a word and an icon.
- **Every phone preview** in the admin panel (the Hub canvas, the draft's phone preview) draws the draft's tabs now,
  not a fixed Home and More.

## Decided while building (2026-11-08)

- **Placements, not one list setting.** One placement per tab gives each tab its own audience, dates and
  "changed for everyone" check, and reuses the tile editor's audience picker and the drafts machinery as they are.
- **The starting tabs are surfaces without a placement,** so nothing has to be seeded and every phone already
  agrees on the defaults.
- **More than three chosen for one person:** the phone draws the first three after Home and the rest aren't tabs for
  them; the editor lists them as *Past the third*. Refusing a fifth wouldn't work, because who sees a tab varies.
- **The same place twice for one person:** the first one wins.
- **Icons are the app's own, not uploaded SVGs.** A tab bar reads as one set of 24-point line icons; uploaded
  drawings vary in weight and fill. Tiles keep their uploads.
- **The word is required and short** (12 characters). Longer words would be cut on a 375-point phone.
- **The app still opens on Home.** Settled by decision 5: Home is always the first tab.
- **Permissions of its own** (`config.tabs.edit`, `config.tabs.publish`), as Colours has, so the office can be given
  Home without the tab bar.
- **Analytics** count taps on places already; there's no separate tab-tap measure.
- **Moving tabs is by buttons, not dragging.** Five cells on a true-size phone are small targets for a drag, and the
  rail's numbered list with *Move left* and *Move right* works the same with the mouse and the keyboard. Moving a tab
  changes only the tabs that moved, so the rest aren't marked as changed.
- **Taking off a starting tab** turns *On the bar* off (it has no placement to remove); taking off an added one
  removes it, with *Put back*.
- **Every phone preview in the admin panel draws the draft's tabs** (the Hub canvas, the phone preview, the pop-out),
  for the person it's showing.
- **A station can run its own backend beside the owner's** to check this: `Dev__PortOffset=100` moves the AppHost's
  fixed ports up by 100 (the gateway on 5400), skips the Cloudflare tunnel and gives the media store its own volume.

## Decided with decision 5 (2026-11-08)

- **Turning Home into a fixed tab keeps its surface,** so placements and draft changes stored for it still load; they
  are ignored on the phone and left out when a draft publishes, which puts Home back first without anyone undoing
  anything.
- **A tab ordered before Home** (saved before this) is still drawn after Home; the next time it's saved its order is
  moved after Home's.

## Open

- A place's *where it is* line ("More → What's new") is written by its feature and doesn't follow the tabs.
- Tests: `TabBarTests` (Platform.Contracts.Tests), the tab rules in `PlacementRulesTests` (Configuration.Api.Tests),
  and the starting tabs' places in `DestinationRules` (Architecture.Tests).
