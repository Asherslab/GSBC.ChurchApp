# Data-driven by design

Staff should be able to reshape how the app is used without a release (requirement G): what shows
where, for whom, in what order, under what name, and which modules a ministry uses. They don't build
new features; code decides what kinds of things exist, and data decides how they're used.

## The line we hold (store rules)

| Data may change | Only a release changes |
|---|---|
| Which tiles, cards and modules show, their order, audience and dates | What a tile, card or module is and does |
| Which modules each kind of ministry has, and their defaults | A new kind of module |
| Labels, help text, church-specific wording | Layout, components, navigation structure |
| Taxonomies: ministry kinds, roles, topics, rounds, rooms, map places | Screens |
| Which theme (brand or yearly) and its token values | New CSS, fonts, logo files |
| Form fields on "I'm interested" and sign-up forms | New field types |

Nothing downloaded is executed: no scripts, no remote layout language that amounts to code. This is
the same line SPEC §4 draws, applied everywhere.

## How

**1. Code registers surfaces; data composes them.** A feature declares the places it can appear,
each with a key, a typed settings record and its defaults:

- a Hub tile (the favoured home);
- a home card;
- a module on a ministry (news, calendar, roster, chat, resources, site-map places);
- a settings section.

Staff place surfaces in the admin panel: order, visibility, audience (everyone, signed in, a role,
a permission), start and end dates, and settings values.

**2. Settings are typed and described.** Each surface's settings are a contract with explicit
defaults, plus a descriptor (field kinds: text, number, yes/no, choice, date, audience, colour token,
media). The admin panel renders one generic editor from descriptors, so a new setting needs no new
admin page. Descriptors are declared, not reflected, so they work under AOT.

**3. Taxonomies are records.** Ministry kinds, roles, notification topics, rounds, rooms and places
are rows staff manage, never enums in code. Code only enumerates what it must handle differently.

**4. Configuration travels as sync feeds** ([offline-and-caching.md](offline-and-caching.md)): it is
offline, live, and needs no new mechanism.

## Older app versions

A surface declares the app version that introduced it. An older app ignores keys it doesn't know,
and the admin panel shows "needs app version X" next to anything recent, so staff know who sees it.
Settings contracts only grow (SPEC §5's additive rule).

## Guard rails

- The server validates every configuration change against its descriptor.
- Changes are audited (who, when, before and after) and can be rolled back.
- Editing configuration is itself a permission, per area (`config.hub.edit`, a ministry's
  `ministries.modules.edit` at that ministry), so a leader can arrange their own ministry and nothing
  else.

## Decided when building it (2026-11-02)

How the above is built is in [features/configuration.md](../features/configuration.md). The doc left
these open; none split into materially different designs, so they were decided and recorded here.

- **Descriptors live in Platform.Contracts** (`Platform.Contracts.Configuration`), not in the
  Configuration feature. Features can't reference each other, and Ministries, Sermons and the rest
  must declare surfaces. The Configuration feature owns storage, the service, the feed and the editor.
- **An area is where surfaces go:** a key, the scope kind it's arranged at, and the permission that
  edits it (the Hub: global, `config.hub.edit`; a ministry's modules: per ministry,
  `ministries.modules.edit`). Areas are declared beside the surfaces that use them. The **Hub area is
  in Platform** because every feature with a tile names it.
- **Settings are a string map, typed by the descriptor.** Each field is an object
  (`TextSetting`, `NumberSetting`, `YesNoSetting`, `ChoiceSetting`, `DateSetting`, `ColourSetting`,
  `AudienceSetting`, and since 2026-11-02 `MediaSetting` and `RichTextSetting`) that validates a raw value and reads it with its default
  (`values.Get(LinkTile.Title)`). That's the typed contract, without a protobuf message per surface,
  and it grows additively: a new field has a default, and an unknown key is ignored by the phone and
  rejected by the server.
- **Defaults are not seeded.** A placement row exists only once staff change something. The phone
  merges each known surface's declared defaults (shown or not, order) with the stored placements, so a
  new surface appears after an app update with no migration, and scoped areas (every ministry) need no
  rows at all.
- **One placement per area, scope and surface,** unless the surface is *repeatable* (staff-made link
  tiles), whose instances exist only as rows.
- **Audience is display, not secrecy.** The placements feed is public: it says what to show, not
  content. A surface for "signed in" or "holds a permission" is hidden from others on the phone, but
  its settings can be read by anyone, so settings never hold anything private. Content behind a
  surface keeps its own feed audience.
- **Audience "a role" is deferred.** The phone holds grants, not roles. Everyone, signed in, and
  "holds a permission" (at the placement's scope) are built; a role audience comes with Access's private
  grants feed, if staff need it.
- **Rollback is per save.** Every save is one change batch, audited with each placement's before and
  after; rolling back restores the batch's before state, validated against today's descriptors, as a
  new audited save. A save carries the revision it was based on and is refused if someone else saved
  since.
- **"Needs app version X"** is shown for surfaces introduced after the first store release (1.0).
- **Not yet:** taxonomies, which are each feature's own records. The media field kind and a
  rich-text one were added on 2026-11-02 ([configuration.md](../features/configuration.md)).

## Themes (decided 2026-11-02)

Which palette everyone sees, the church's brand or the yearly theme, is staff's choice and needs no
release. It is the smallest design that fits the machinery above:

- **One area, one surface.** `AppearanceSurfaces` (Platform.Contracts) declares the area `app.appearance`
  ("Colours", global, edited with `config.appearance.edit`) and one non-repeatable surface `app.theme`
  with a choice setting `palette`: `brand` (the default) or `year-2026`. Staff edit it with the same
  editor as the Hub (admin panel → Colours). It needs no new service, table or feed, and its dates let a
  theme run for a season. Hidden, outside its dates, or a palette this app version doesn't know: the
  brand.
- **Applied at start-up and live.** The phone reads the area from the placements feed
  (`ISurfaceLayout`), sets `data-theme`, and remembers it, so the next launch starts in the right
  colours before Blazor runs; a change arrives through sync like any placement.
- **Previewing.** Anyone holding `config.appearance.edit` can try a palette on their own phone
  (More → Appearance) without changing it for everyone, then "Change for everyone" opens the editor.
  Until people can sign in on the phone (passkeys wait on the Apple enrolment), development builds
  offer the preview too; drop that once staff can sign in on the phone.
- **Light, dark or automatic** is each person's own, kept on the phone.
- **The palettes themselves are code.** Token values live in `tokens.css` and change with a release
  (SPEC §4); next year's theme is a new palette id in a release, then staff switch to it. Token values
  as data stays open until there is a reason for it.
