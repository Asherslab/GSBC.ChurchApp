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

## What staff see (to be designed before building)

The screens matter as much as the model, and are mocked up and reviewed before the code:

- **Drafts list** (admin panel and app): each draft with its author, what it touches ("Colours, Home
  screen, 3 changes"), who it's shared with, conflicts, and its actions. The event layer is shown on
  its own at the top: on, scheduled, or none.
- **Editing in a draft:** the existing editor with the draft's name in its header, changed rows marked,
  conflicts on their rows, and *Publish* / *Ask to publish*.
- **On the phone:** Previews under More, the preview banner, and *as someone else* / *on a date*.

## Open

- Whether a draft can also hold changes for a ministry leader's own ministry page, previewed by that
  leader: yes in the model; check the screens make sense for leaders.
- Notifications ("Conference 2027 is waiting to publish"): with Push.
- Drafts for feature records (events, sermons): not now, by decision 1.
