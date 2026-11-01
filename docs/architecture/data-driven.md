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
