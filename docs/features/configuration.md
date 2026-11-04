# Configuration

Surfaces, settings descriptors and the generic editor: how staff arrange what the app shows without a
release. The design is [data-driven.md](../architecture/data-driven.md); this is how it's built.

## What people can do (v1)

| Who | Can |
|---|---|
| Everyone (the phone) | See each area as staff arranged it, offline and live, through the placements feed |
| The area's edit permission, at its scope | Show, hide, reorder and date surfaces; set their audience and settings; add repeatable ones; see the history and roll back a save |

The Hub needs `config.hub.edit`. A ministry's modules will need `ministries.modules.edit` at that
ministry, so a leader arranges their own ministry and nothing else.

## Declaring

A feature declares its areas and surfaces in its Contracts, with the types in
`Platform.Contracts.Configuration`:

```csharp
public static class HubSurfaces
{
    public static readonly Permission Edit = new("config.hub.edit", ScopeKind.Global, "Arrange the app's Home screen");
    public static readonly SurfaceArea Area = new("home.hub", "Home screen", "The tiles on Home", ScopeKind.Global, Edit);

    public static class LinkTile
    {
        public static readonly TextSetting Title = new("title", "Title", required: true, maxLength: 40);
        public static readonly Surface Surface = new("hub.link", Area, "Link tile", "A tile that opens a web page",
            [Title, Subtitle, Link, Colour, Wide], defaultOrder: 1000, repeatable: true);
    }
}
```

Besides text, number, yes/no, choice, colour, date and audience, Home added (2026-11-04) a link
(`LinkSetting`: a place in the app or an https page), an icon (`IconSetting`: built in or
`custom:<id>`), a focal point (`FocusSetting`) and a cleaned SVG drawing (`SvgIconSetting`, which the
server normalises on every save through `INormalises`). Two kinds hold content (2026-11-02):

- **`MediaSetting`**: an image (or, with `imagesOnly: false`, any file) from the media library, stored
  as its `MediaRef` in JSON. Settings are public, so it must be a public file.
- **`RichTextSetting`**: formatted text ([rich-text.md](../architecture/rich-text.md)), stored as JSON.

On save, the server replaces every `MediaRef` in these with the library's own and refuses forged ones
(`IMediaReferences`, [media.md](media.md)), checks rich text against its rules, and records which
placement uses which file once the save commits, so a file still on a tile can't be deleted. The
editor shows a thumbnail with Choose, Change and Remove, and the rich-text editor; on the phone, the
placements feed's pictures are prefetched for offline use.

It registers them on both sides with `AddSurfaces(...)` (Platform.Api for validation, Platform.Client for
the editor and the layout), the same way it registers permissions.

## Reading, on the phone

`ISurfaceLayout` (Platform.Client) gives a feature an area's placed surfaces for the person holding the
phone right now: known surfaces only, defaults merged in, hidden ones, ones out of their dates, and
ones whose audience excludes this person left out, in order. It reads the store, never the network,
and raises `Changed` when the feed does. Home calls it for the Hub; a ministry page will call it for
its modules.

## Storing

Schema `config`:

- `placements`: id, area, scope object, surface, order, hidden, audience, starts and ends, the
  settings (`jsonb`), a revision, who and when.
- `changes`: one row per placement per save, sharing a batch id: who, when, before and after (`jsonb`).

Each save writes the placements, their `changes` rows and their `config.placements` feed changes in one
transaction, so Sync's trigger announces it to every open phone.

## The service

`gsbc.config.Configuration`, every method `[SignedIn]` with the area's permission checked in the method
(the node depends on the area, so it can't be a static `[Requires]`):
`GetArea`, `ListChanges` and `RollBack` (undoing a published draft, which needs the publish permission
for every area in it). Changes are made through `gsbc.config.Drafts` ([drafts.md](../architecture/drafts.md)). A change based on an old revision, or a second copy of a non-repeatable surface, is
refused with `Aborted` ("someone else changed this"), and the editor reloads.

Management reads go over gRPC, like Access's screens; only the phone's display reads the store.

## The editor

The drafts workbench (`DraftsWorkspace`, Configuration.Client) renders any area from its descriptors: a
row per surface, with show or hide, move up and down where the area's order matters, then the settings
form (one input per field kind), audience and dates, and "needs app version X" where it applies. It's
the admin panel's Drafts page (`/drafts`); the app has no editor, only More → Previews (admin work stays
in the admin panel, AGENTS.md). Other features link to it by route rather than embedding it, since they
can't reference Configuration.

## The first consumer: Home

The Hub area holds Home's tiles, its grid and uploaded icons: plain tiles, feature tiles that features
declare (`HubTileKind`, registered with `AddHubTile<TView>`), `hub.grid` and `hub.icon`. Home draws them
through `HomeScreen` and the tile views, and the drafts workbench arranges them on a phone canvas. All of
it is in [home.md](home.md). The older link tile (`hub.link`) still draws but isn't offered for new tiles.

## The second consumer

The app's **theme** (`app.appearance`: an `app.theme` surface with a palette choice, and repeatable
`app.custom-theme` surfaces for themes staff make): staff choose the church's brand, the yearly theme
or one they made, for everyone, and the phone applies it live. See
[data-driven.md](../architecture/data-driven.md), *Themes*. It's edited under Colours in a draft.

## Status

**Built:** the descriptors and layout composition in Platform.Contracts; `ISurfaceLayout` and
`AddSurfaces` in Platform.Client and Platform.Api; the `config` schema, the service with validation,
per-area permissions, revisions, audit and rollback, and the `config.placements` feed; the editor in
the admin panel's Drafts page; the Hub area. Tested by
`Features.Configuration.Api.Tests` (Postgres) and `SurfaceLayoutTests` in Platform.Contracts.Tests.

**Built 2026-11-02: drafts, the event layer and previews** ([drafts.md](../architecture/drafts.md),
*Status*). Every edit is made in a draft: the service's direct `Save` is gone, replaced by
`gsbc.config.Drafts`, and publishing needs the area's publish permission (`config.hub.publish`,
`config.appearance.publish`). The separate "Home screen" and "Colours" pages are gone (owner,
2026-11-03): Drafts shows what everyone sees, its history and *Undo*. Run end to end
through the AppHost and on the owner's iPhone.

**Open:**

- ~~Home renders it properly~~: built 2026-11-04 ([home.md](home.md)).
- **Choosing a permission as an audience** isn't offered in the editor (an existing one is kept and
  shown as "only people with a particular permission"): picking one needs the plain-language
  permissions from Access's *Usability comes first*.
- **Ministry modules:** Ministries declares its area (`ministries.modules.edit` per ministry) and its
  module surfaces, and links its settings page to Drafts on that area, once the workbench handles
  areas scoped to one ministry (open, [drafts.md](../architecture/drafts.md)).
- **Feed writes by hand:** the editor adds its `sync.feed_changes` rows itself, in the same
  `SaveChanges`. [storage.md](../architecture/storage.md) wants Platform.Data to make that impossible
  to forget; do it when a second feature writes a feed.
- ~~A role audience~~: built 2026-11-04 through Access's membership grants ([home.md](home.md), *Who sees it*).

