# App tabs: the tab bar the church chooses

The app's tab bar is chosen by staff in the admin panel, in a draft, the way they arrange Home
([home.md](home.md)), and published to phones without a release. Asked for by the owner on 2026-11-08
while designing Events ([events.md](events.md), decision 34): whether the calendar is a tab is the
church's choice. Built on Configuration's surfaces and drafts ([drafts.md](../architecture/drafts.md));
there is no store of its own. Until staff publish a change, the tabs are what they were: Home, Sermons, More.

## What people can do

| Who | Can |
|---|---|
| Everyone (the phone) | Use the tab bar as published: up to four tabs and More, each opening its place; offline and live |
| `config.tabs.edit` | In a draft: add tabs, choose the place each opens, its word and icon, move them, choose who sees each and when, take them off the bar |
| `config.tabs.publish` | Publish the draft, or turn it on for an event |

The *App editor* role (`config.*`) holds both.

## Decided (owner, 2026-11-08)

1. **Any place can be a tab.** A tab is a link to a place in the app ([destinations.md](../architecture/destinations.md)),
   like any other link: a page (Home, Sermons, the calendar's week), or one thing (a sermon series).
2. **Five tabs at most, More always last and always there.** Staff choose four.
3. **Who sees which tabs can vary,** as Home's tiles do: everyone, people signed in, or people in roles, checked through
   Access ([permissions.md](../architecture/permissions.md)).
4. **Taking a tab off the bar removes a link, not a page.** The page is still there and still reached from its other
   links (Home's tiles, More, search, notifications). A phone that is on that page when the change arrives stays on it;
   only the tab goes. Places that aren't tabs are listed in More, as now.

## How it works

### Tabs are placements

The tab bar is a surface area, `app.tabs` (*Tab bar*), in Platform.Contracts (`TabBarSurfaces`). Each tab is one
placement with three settings, the same for every tab (`TabSettings`): **Opens** (a place in the app), **Word** (the
label under the icon, up to 12 characters, so it fits a fifth of a phone's width) and **Icon**. Its order, who sees
it, its dates and whether it's shown are the placement's own, as for a tile. Drafts, the event layer, previews on a
phone, history and undo all work on it unchanged.

- **The two tabs the app starts with** are surfaces that exist without a placement (`app.tab.home` opening `/`, and
  `app.tab.sermons` opening `/sermons`), so a phone with nothing published draws Home, Sermons and More, as before.
  Staff change them like any other tab; taking one off the bar hides it, and *Put back* shows it again.
- **Every tab added after** is a repeatable `app.tab` placement.

### What a tab may open

A place in the app that opens inside the app: a page or one thing of a kind. Not a web page, not More itself (it is
always last), and not a kind that opens outside (a file opens in the browser, which isn't somewhere to *be*). The
server refuses anything else on save (`PlacementRules`, through `DestinationCatalog`), and the picker only offers
places everyone the tab is for can open, as for an announcement (`OpenableBy`).

### On the phone

- **The tab bar** (`AppTabBar`, Platform.Ui) asks `ISurfaceLayout` for the area, as Home does, and draws the first
  four tabs shown to this person in order (`TabBar.For`, Platform.Contracts), then More. Fewer than four is fine.
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

Drafts → a draft → **Tab bar**, desktop first, built from the Hub canvas's parts (*The phone is the canvas*, chosen on
`.impeccable/mocks/decision/app-tabs/`):

- **The phone** draws the draft's Home with the draft's tab bar. Click a tab to choose it (the orange marker ring);
  More is drawn locked, with a lock and "Always last".
- **Beside it,** a rail: *Add a tab* (off when four are already on the bar for everyone), *Show it as* (a visitor,
  someone signed in, someone in a role a tab is for), and under it the tabs that aren't on the bar for that person,
  each with why: hidden, dated, for someone else, or past the fourth. The phone's own tabs are listed there too, in
  order, with *Move left* and *Move right*, so the order can be changed with the keyboard as well as by eye.
- **The settings panel** for the chosen tab: Opens (the place field), its word and its icon (filled in from the place
  when one is chosen, then the staff's to change), who sees it and when, *Shown*, and *Take it off the bar*. A plain
  note says what taking it off does: "The page stays in the app, reached from Home, More and links."
- **Every phone preview** in the admin panel (the Hub canvas, the draft's phone preview) draws the draft's tabs now,
  not a fixed Home and More.

## Decided while building (2026-11-08)

- **Placements, not one list setting.** One placement per tab gives each tab its own audience, dates and
  "changed for everyone" check, and reuses the tile editor's audience picker and the drafts machinery as they are.
- **The starting tabs are surfaces without a placement,** so nothing has to be seeded and every phone already
  agrees on the defaults.
- **More than four for one person:** the phone draws the first four and the rest aren't tabs for them; the editor
  lists them as *Past the fourth*. Refusing a fifth wouldn't work, because who sees a tab varies.
- **The same place twice for one person:** the first one wins.
- **Icons are the app's own, not uploaded SVGs.** A tab bar reads as one set of 24-point line icons; uploaded
  drawings vary in weight and fill. Tiles keep their uploads.
- **The word is required and short** (12 characters). Longer words would be cut on a 375-point phone.
- **The app still opens on Home,** even when Home isn't a tab (Home then heads More). Opening on the first tab would
  change cold starts and links; not without the owner.
- **Permissions of its own** (`config.tabs.edit`, `config.tabs.publish`), as Colours has, so the office can be given
  Home without the tab bar.
- **Analytics** count taps on places already; there's no separate tab-tap measure.

## Open

- Whether the app should open on the first tab when it isn't Home.
- A place's *where it is* line ("More → What's new") is written by its feature and doesn't follow the tabs.
- Tests: `TabBarTests` (Platform.Contracts.Tests), the tab rules in `PlacementRulesTests` (Configuration.Api.Tests),
  and the starting tabs' places in `DestinationRules` (Architecture.Tests).
