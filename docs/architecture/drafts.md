# Drafts, event layers and previews

How staff prepare changes to what the app shows, look at them on their own phone, and put them out
to everyone, either for good or for an event. Decided with the owner on 2026-11-02, before Home is
built, so everything staff arrange is built on it from the start.

## What staff need

- **Several pieces of work at once.** One person spends weeks redoing the Hub and colours for the
  conference; meanwhile someone reports a typo on a Hub tile, and it's fixed and out the same day.
  Neither waits for the other.
- **See it on their own phone before anyone else does,** and watch it change as they edit in the
  admin panel: a save (or a short pause while typing) shows on the phone within a second or two, like
  sync does today. This is a requirement, not a nicety.
- **Show a draft to others** (the senior pastor, the conference team) on their phones.
- **Switch the app into a temporary state and back:** conference week puts the site map front and
  centre, then the app returns to normal, including any fixes made during the week. Next year starts
  from this year's.
- **Someone else publishes.** Preparing a change and putting it out to everyone are separate
  permissions.

## Decisions (owner, 2026-11-02)

1. **What drafts cover:** everything staff arrange through Configuration: the Hub, colours and theme,
   ministry pages, settings, rich-text content held in settings, the site-map tile. A feature's own
   records (events, sermons, notices) keep their own publish dates and are not part of drafts; a draft
   may link to one that isn't out yet.
2. **Publishing is its own permission,** separate from editing.
3. **Drafts can be shared** with named people, who can preview them.
4. **One event layer at a time.**
5. **Live preview while editing:** the phone previewing a draft updates as the draft is saved. Saving
   can be the save button, or automatic after a short pause; it needn't be every keystroke.

## The model: changes on top of Live, never copies

A **draft** is a named set of changes against Live: "this tile now says X", "add this tile", "remove
that one", "palette: year-2027". It is never a copy of the app. What a draft shows is always *Live as
it is now, plus the draft's changes*.

That one choice is what keeps parallel work tenable:

- A typo fixed in Live appears in every open draft at once, with nothing to merge, unless the draft
  changed that same tile.
- Turning an event layer off leaves Live exactly as it is now, with the week's fixes, rather than
  restoring a snapshot from before the event.
- A conflict only exists when Live and a draft changed the **same field of the same placement**.
  It's shown on that row as "Changed in Live since you edited it", with *Keep mine* and *Use Live's*,
  and it must be settled before publishing.

Each change records the placement it changes and the placement's revision it was based on, which is
how conflicts are found (the same revision check `Save` uses today).

### What a draft can become

| Action | Who | Effect |
|---|---|---|
| **Publish** | Publish permission for every area it touches | Its changes become Live in one batch, recorded in the history under the draft's name and undoable as a whole. The draft is then closed. |
| **Turn on for an event** | Same | It becomes the event layer: Live plus its changes for everyone, now or between dates. **Turn off** (or its end date) removes the layer; Live is untouched. It stays saved. |
| **Make a copy** | Anyone who can see it | A new draft with the same changes: next year's conference. |
| **Share** | Its author, or a publisher | Named people can preview it. |
| **Delete** | Its author, or a publisher | Gone; nothing reached Live. |

**One path for edits.** Every edit happens in a draft. A small fix is a draft of one change with
*Publish now* next to the save button, for people holding the publish permission, so it's a click,
not a process. Someone without it gets *Ask to publish*, which lists the draft for publishers.

**Drafts can span areas** (the conference touches colours, the Hub and a ministry page). Each change
is checked against the area's edit permission when it's made, and publishing needs the publish
permission for every area touched.

### The event layer

- At most one at a time. Turning one on while another is on asks to replace it.
- It can be on now, or scheduled with a start and an end. **A scheduled layer reaches phones in
  advance** (it travels in the public placements feed with its dates), so a phone switches at the
  start time even offline, exactly as tile dates work now.
- Live keeps being edited and published underneath it. If a published change removes a placement the
  layer changes, the layer's change is dropped and the layer shows a warning to publishers.
- The layer is validated again in full each time it's turned on or scheduled.

## Previewing on a phone

- The drafts you can see (yours, shared with you, or all of them if you can publish) sync to your
  phone through a private feed, so previews work offline and update live.
- **More → Previews** lists them. Picking one shows the app as Live plus that draft, with a banner that
  stays visible on every screen: *Previewing "Conference 2027" · Exit*. Exiting returns to normal
  at once. The choice stays on that phone only.
- **While you edit the draft in the admin panel,** each save travels through sync to the phone, which
  re-composes the screen (`ClientSignals` and `LiveData`, as everything else does). The editor saves a
  draft automatically about a second after you stop typing, so there's no save button to remember in
  a draft; publishing is the deliberate step.
- The preview can also show **as someone else**: *as a visitor* (not signed in), *as a member*, and
  **on a date** (to see a scheduled event layer, or tiles with dates, before they start). These change
  only what the phone composes; they grant nothing.

## How it's built

On top of Configuration, with no new feature project:

- **Storage** (schema `config`):
  - `drafts`: id, name, author, created, status (open, waiting to publish, closed), and for the event
    layer whether it's on and its start and end;
  - `draft_changes`: draft id, area, scope, placement (or a new placement's id), the revision it was
    based on, and the change (`jsonb`: new or changed settings, order, hidden, audience, dates, or
    remove).
- **Composition is one function, shared by the server and the phone:** Live placements, then the
  event layer if it's on at that moment, then the previewed draft, with the existing audience and
  date rules applied last. It lives with `ISurfaceLayout`'s composition in Platform.Contracts, so the
  phone, the editor and the server's validation can't disagree about what a draft shows.
- **Feeds:**
  - `config.placements` (public, as now) also carries the event layer's changes once it's on or
    scheduled, with its dates;
  - `config.drafts` (new, private): each draft and its changes, scoped to that draft, so a phone
    receives only drafts its user may see.
- **Sharing uses Access,** not a list of its own. A new scope kind `draft`, a permission
  `config.drafts.preview` at that scope, and a system role *Draft reviewer* that Configuration gives
  and takes away through `IRoleAssignments` when a draft is shared or unshared. Sync's per-scope
  feed filtering then does the rest, and removing someone takes the draft off their phone.
- **Permissions:** the area's existing edit permission (`config.hub.edit` and so on) to change it in
  a draft; a new publish permission per area (`config.hub.publish`, and so on) to publish or turn on an
  event layer.
- **Media:** a picture used by a draft counts as in use (`media.uses`), so it can't be deleted while
  any draft or the event layer needs it.
- **History:** publishing writes the existing `changes` rows with the draft's id and name; turning an
  event layer on or off is recorded too. Rolling back a published draft is one action.
- **App versions:** a draft that uses a surface newer than some phones shows "needs app version X",
  as the editor does now.

## What staff see (designed with the owner, 2026-11-02)

Shaped with the impeccable process: an interview, one round of dealt structures on its decision page, then
a brief the owner confirmed. The direction contracts are in `.impeccable/surfaces/` (the drafts workspace,
and previews on the phone); the dealt sketches are in `.impeccable/mocks/decision/drafts/`.

- **Who:** mostly office staff, every few weeks rather than weekly, since much content (sermons) arrives by
  itself. Each return has to make sense without remembering how it works. Pastors and leaders preview and
  approve on their phones.
- **Must not feel like Git or like ceremony.** On screen there are no branches, diffs, merges, conflicts or
  revisions. Staff read "What everyone sees" (not "Live"), and a field changed underneath a draft is
  "Changed for everyone since you edited it" with *Keep mine* / *Use everyone's*. A typo fix is edit, then
  *Publish now*.
- **The admin panel's drafts page is a three-pane workbench** (owner's choice): drafts down the left (the
  event layer as one navy band at the top, then *Waiting to publish*, *Open*, *Published recently* with
  *Undo*), the chosen draft in the middle (its name, "Saved a moment ago", *Share*, *More*, *Publish now*
  or *Ask to publish*, area tabs, rows with its changes marked), and on the right the chosen row's settings
  or a phone-size preview. A **Church year** view of the left column shows the event layer and dated
  changes across the coming weeks, for scheduling. A row changed for everyone since the draft touched it
  stays marked until someone settles it.
- **See it two ways** (owner): the optional phone preview beside the editor, and your real phone. Until
  Home is built the preview draws an area's surfaces generically.
- **The phone** gets More → Previews (*What
  everyone sees* or a draft; *Show it as* Me, A visitor, A member; *On a date*), and the banner, docked to
  the top of the tab bar on every screen: *Previewing "X" · as a visitor · date · Exit*.
- **Drafts are made and edited in the admin panel only;** the app has no Drafts screen (owner,
  2026-11-03: admin work stays in the admin panel).
- **Home screen in a draft is a phone canvas** (2026-11-04): the draft's Home drawn at true width, where
  tiles are moved, resized and chosen, with the chosen tile's settings beside it
  ([home.md](../features/home.md), *The admin canvas*).
- **Home screen and Colours** in the admin panel become read-only views of what everyone sees; *Change it*
  starts a draft, or carries on your open one.

## Signing in on the phone for previews

Drafts reach a phone only for someone signed in. Since 2026-11-03 that's the real account, with a
passkey in the app ([identity.md](identity.md), *Signing in on the phone*); signed out, More →
Previews offers *Sign in*. The development-only *Link this phone* is gone (owner, 2026-11-03).
Previewing a draft from the signed-in account was proven on the owner's iPhone the same day.



## Status (2026-11-02)

**Built and proven on the owner's iPhone 15 Pro, with the owner watching:** a title changed in the admin
panel's draft showed on the phone, previewing that draft, within a second of "Saved".

- **The model:** `PlacementPatch` and `DraftComposition` in Platform.Contracts (published, then the event
  layer if it's on at that moment, then the previewed draft; "changed for everyone since" per field),
  used by the server's publish and the phone's layout. Tested by `DraftCompositionTests`.
- **Configuration:** `config.drafts` and `config.draft_changes` (migration `Drafts`), the
  `gsbc.config.Drafts` service (list, get, create or copy, rename, save, settle, ask to publish,
  publish, turn on or off for an event, share, unshare, delete), history rows tagged with the draft,
  a draft's pictures held in use as `config.draft/{id}`, and per-area publish permissions
  (`config.hub.publish`, `config.appearance.publish`). The direct `Configuration.Save` is gone: every
  edit is made in a draft. Tested by `DraftEditorTests` and the reworked `PlacementEditorTests`.
- **Access:** a `draft` scope kind, `config.drafts.preview` (held at a draft: preview it; held
  everywhere: see every draft), the system roles *Draft reviewer* (given and taken by Configuration
  when a draft is shared or unshared, and to its author) and *App editor* (`config.*`).
- **Sync:** a private feed resets for a session whose grants for it changed (`sync.audience_marks`),
  so unsharing takes a draft off that phone. Tested end to end in `DraftEditorTests`.
- **Feeds:** `config.drafts` (private, per draft) and the event layer as the `event-layer` record in
  `config.placements`, with its dates.
- **Admin panel:** the drafts workbench (`/drafts`), the church-year view, the phone preview (the
  app's own Home on a chosen iPhone, true size, scaled to fit, with a pop-out window at
  `/drafts/{id}/preview`), and read-only Home screen and Colours pages whose *Change it* starts a
  draft.
- **App:** More → Previews (a draft or what everyone sees, *Show it as*, *On a date*), the banner
  (hideable until the app next comes to the foreground), and Home listing the Hub's
  link tiles. The phone previews from the signed-in account.

### Live in the admin panel (2026-11-03)

The drafts page follows `config.drafts` and `config.placements` through the admin panel's live stream
([offline-and-caching.md](offline-and-caching.md), *The admin panel*), so someone else's save shows
within a second or two:

- **The draft you have open** takes in their changes as they save. Saves are per row, so two people
  working on different rows of one draft don't disturb each other.
- **The row you're editing** keeps what you've typed. If their save changed that same row, it says
  *Someone else changed this while you were editing it. What you type next saves over their change*,
  with *Keep mine* (saves yours now, over theirs) and *Use theirs* (drops your unsaved typing for that row and shows theirs). If the draft is published or deleted while you're in it, the notice says which, and who published it.
- **What everyone sees** (Live) is refreshed when you close the row you're editing, so the form doesn't
  shift under you; a field changed for everyone then shows as usual, to settle before publishing.
- **A draft published or deleted by someone else** closes, with a note saying so.

**Decided while building (no owner question needed):**

- A draft is changed by its author or by someone who sees every draft; people it's shared with only
  preview it. Editing a draft that's on for an event needs the publish permission, since everyone
  sees each save.
- Asking to publish puts a draft under *Waiting to publish*; any later edit takes it back out.
- *Keep mine* re-bases the change on what everyone sees now; *Use everyone's* drops the fields that
  changed. Publishing is refused while any are unsettled.
- Undo of a published draft needs the publish permission for every area it touched.
- Deleting a draft that's on for an event is refused until it's turned off.
- **The phone preview can't drift from the app:** Home draws through `HomeScreen` and each surface
  through `SurfaceViews` (Platform.Ui), which the preview shares. `PreviewRules` (Architecture.Tests)
  fails if the app's Home stops using `HomeScreen` or a Hub surface has no view. When Home is built,
  keep everything a draft can change inside `HomeScreen` and register a view for every new surface.
  Home is only `HomeScreen` now: the skeleton's Connection section is gone (owner, 2026-11-04, note 10),
  and being offline shows in the masthead's `SyncIndicator`, which draws only when Home is live.

## Open

- **Scoped areas** (a ministry's page) work in the model; the workbench lists global areas only.
- Whether a draft can also hold changes for a ministry leader's own ministry page, previewed by that
  leader: yes in the model; check the screens make sense for leaders.
- Notifications ("Conference 2027 is waiting to publish"): with Push.
- Drafts for feature records (events, sermons): not now, by decision 1.
