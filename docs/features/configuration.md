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

Besides text, number, yes/no, choice, colour, date and audience, two kinds hold content
(2026-11-02):

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
`GetArea`, `Save` (one batch for one area and scope: placements to create or change, and placements to
remove, which puts a surface back to its default or deletes a repeatable one), `ListChanges` and
`RollBack`. A change based on an old revision, or a second copy of a non-repeatable surface, is
refused with `Aborted` ("someone else changed this"), and the editor reloads.

Management reads go over gRPC, like Access's screens; only the phone's display reads the store.

## The editor

`SurfaceEditor` (Configuration.Client) renders any area from its descriptors: a row per surface, with
show or hide, move up and down, then the settings form (one input per field kind), audience and dates,
and "needs app version X" where it applies. It's the page `configure/{area}` (and
`configure/{area}/{scope}`) in both the app and the admin panel, shown in navigation to those who hold
the Hub's permission. Other features link to it by route rather than embedding it, since they can't
reference Configuration.

## The first consumer

The Hub's **link tile** (`hub.link`): repeatable, with a title, subtitle, link, colour token, a
"wide" switch and an optional picture. Home will render it for tiles no feature provides, such as the Bible link or a one-off
campaign. Home itself isn't built yet; when it is, it reads `ISurfaceLayout.ForAreaAsync(HubSurfaces.Area)`.

## Status (2026-11-02)

**Built:** the descriptors and layout composition in Platform.Contracts; `ISurfaceLayout` and
`AddSurfaces` in Platform.Client and Platform.Api; the `config` schema, the service with validation,
per-area permissions, revisions, audit and rollback, and the `config.placements` feed; the editor and
its Arrange page in the app and the admin panel; the Hub area with its link tile. Tested by
`Features.Configuration.Api.Tests` (Postgres) and `SurfaceLayoutTests` in Platform.Contracts.Tests.
Not yet run end to end through the AppHost or on a phone.

**Open:**

- **Home renders it.** Nothing draws Hub tiles yet; Home reads `ISurfaceLayout` when it's built.
- **The phone holds no grants yet:** Access.Client isn't in the app, so "signed in" and
  permission audiences are hidden there, and the Arrange entry doesn't show, until Access joins the app
  (with its private grants feed, [access.md](access.md)).
- **Choosing a permission as an audience** isn't offered in the editor (an existing one is kept and
  shown as "only people with a particular permission"): picking one needs the plain-language
  permissions from Access's *Usability comes first*.
- **Ministry modules:** Ministries declares its area (`ministries.modules.edit` per ministry) and its
  module surfaces, and links its settings page to `configure/ministry.modules/{id}`.
- **Feed writes by hand:** the editor adds its `sync.feed_changes` rows itself, in the same
  `SaveChanges`. [storage.md](../architecture/storage.md) wants Platform.Data to make that impossible
  to forget; do it when a second feature writes a feed.
- Themes, and a role audience ([data-driven.md](../architecture/data-driven.md)). The media and
  rich-text field kinds were built on 2026-11-02 but haven't been run against the API yet.
