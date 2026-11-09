# Drafts, event layers and previews

How staff prepare changes to what the app shows, look at them on their own phone, and put them out
to everyone, either for good or for an event. Decided with the owner on 2026-11-02, before Home was
built, so everything staff arrange is built on it. Notifications for "waiting to publish" and "shared with
you" are built ([notifications.md](notifications.md)).

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
- **Preview controls say they preview** (owner, 2026-11-09, after a meeting with staff: "Show Home for:
  Everyone" read as if it decided who sees Home). Every control that changes only what a preview shows is
  worded as previewing: *Preview the draft as*, *Preview in* (light or dark), *Preview the draft on*, *Preview
  on* (the phone model), and on the phone *Preview it as* and *Preview on a date*. Never "Show … for".

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
  `config.drafts.preview` at that scope, and a system role *Reviewer* (of a draft) that Configuration gives
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
- **Until 2026-11-09 the admin panel's drafts page was a three-pane workbench** (owner's choice then; replaced by *App styling*, below): drafts down the left (the
  event layer as one navy band at the top, then *Waiting to publish*, *Open*, *Published recently*, each
  opening a sheet to undo it or publish it again), the chosen draft in the middle (its name, "Saved a moment ago", *Share*, *More*, *Publish now*
  or *Ask to publish*, area tabs, rows with its changes marked), and on the right the chosen row's settings
  or a phone-size preview. A **Church year** view of the left column shows the event layer and dated
  changes across the coming weeks, for scheduling. A row changed for everyone since the draft touched it
  stays marked until someone settles it.
- **See it two ways** (owner): the optional phone preview beside the editor, and your real phone. The
  preview draws Home through the app's own `HomeScreen`, and other areas' surfaces generically.
- **The phone** gets More → Previews (*What
  everyone sees* or a draft; *Preview it as* Me, A visitor, A member; *Preview on a date*), and the banner, docked to
  the top of the tab bar on every screen: *Previewing "X" · as a visitor · date · Exit*.
- **Drafts are made and edited in the admin panel only;** the app has no Drafts screen (owner,
  2026-11-03: admin work stays in the admin panel).
- **Home screen in a draft is a phone canvas** (2026-11-04): the draft's Home drawn at true width, where
  tiles are moved, resized and chosen, with the chosen tile's settings beside it
  ([home.md](../features/home.md), *The admin canvas*).
- **Home screen and Colours** in the admin panel become read-only views of what everyone sees; *Change it*
  starts a draft, or carries on your open one.

## App styling (owner, 2026-11-09)

The owner, after using the workbench: "drafts is incredibly overwhelming", the page jumped around between Colours,
Home screen and Tab bar, *Published recently* filled the column, Church year "feels out of the way or useless", and
there was no way to "simply, quickly, look at the existing preview of what it is now". Direction: "drafts shouldn't
be the front and center feature of it, the intent is to view & modify the styling, drafts is a vehicle". Shaped with
the impeccable process: an interview, two dealt rounds (the owner steered the first: "one stage, three areas, but it
still feels very similar to the current version, which could still be a bit busy?"), and the states confirmed. The
sketches are in `.impeccable/mocks/decision/app-styling/`; the direction contract in the drafts surface brief.

- **The page is App styling** (`/app-styling`; `/drafts` links still work). It opens on the app as everyone sees it:
  a sentence ("Everyone sees the church's brand, 11 tiles on Home and 4 tabs", with the event layer first when one is
  on), the phone drawn by the app's own `HomeScreen`, and one 400px column: *What everyone sees* (Colours, Home screen,
  Tab bar) and *Coming up*.
- **One layout that never moves:** the phone on the left with its preview choices under it, the column on the right.
  Choosing a part swaps what's in the column; the header keeps its height and its buttons stay where they are.
- **Changing starts from the app, not from a draft** (owner's pick of the recommended option): clicking a part of the
  phone, or a part in the column, carries on in your one open draft, starts one named after what you change ("Home
  screen, Mon 9 Nov") when you have none, or asks which when you have several. A draft started that way and left with
  nothing in it is deleted when you leave it, so looking around leaves nothing behind. Inside a draft the phone is the
  Home canvas (drag and resize tiles; click a tab to change it), and *Back to what everyone sees* leaves the draft
  saved.
- **Drafts are behind one menu** at the top right: on for an event or publishing on a date, waiting to publish, open
  (+ New draft), then *Published recently*, folded until opened (undo and publish again as before).
- **Church year is folded into Coming up** (owner's pick): a dated list of what changes in the next six months (an
  event layer going on or off, a draft publishing for good, a tile or tab with dates, a theme with dates). Clicking one
  shows the app on that day on the phone, and opens its draft if it's in one. The phone's own *Preview the app on* shows
  any day.
- **Colours in plain words:** "The app's colours: the theme the app uses all year, for everyone", as cards (every
  theme, the seeded church's brand and 2026 theme included, each with *Change it*, *Copy* and *Delete*), plus *Make a
  theme* ([data-driven.md](data-driven.md), *Themes*). The owner
  (2026-11-09): "we'll likely do a year long theme colours, changing yearly. with many a brief change for events", so
  the year's theme is the everyday choice (set up in a draft and published on 1 January), and an event's colours go in a
  draft turned on between dates. A theme choice that still has dates says so, with *Change the dates*.
- **The phone acts in its own part (owner, 2026-11-09):** dragging or resizing a Home tile on the phone opens the Home
  screen part, and dragging a tab along the phone's tab bar (Home and More stay put) opens the Tab bar part, whichever
  part was open; the move arrows stay as the keyboard way. A change is always saved in its surface's own area
  (`DraftSession` takes the area from the surface), so a tile can't land in Colours.
- **Preview choices read as a sentence** under the phone: "Preview as a visitor in light mode on an iPhone 15 Pro on
  (day)". They change only the phone.
- **Kept as they were:** saving, undo and redo, sharing, *Publish now* / *Ask to publish*, turning on for everyone or
  publishing on a date, settling "changed for everyone" (now a notice at the top of the column, with *Keep mine* and
  *Use everyone's* per item), the pop-out preview window (under More), and phones previewing a draft live.

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
  everywhere: see every draft), the system roles *Reviewer* (given and taken by Configuration
  when a draft is shared or unshared, and to its author; Share is the shared People sheet since 2026-11-09),
  *App Editor* (edit and see every draft) and *App Publisher* (`config.*`).
- **Sync:** a private feed resets for a session whose grants for it changed (`sync.audience_marks`),
  so unsharing takes a draft off that phone. Tested end to end in `DraftEditorTests`.
- **Feeds:** `config.drafts` (private, per draft) and the event layer as the `event-layer` record in
  `config.placements`, with its dates.
- **Admin panel:** the drafts workbench (`/drafts`, App styling since 2026-11-09), the church-year view (now *Coming up*), the phone preview (the
  app's own Home on a chosen iPhone, true size, scaled to fit, with a pop-out window at
  `/drafts/{id}/preview`), and read-only Home screen and Colours pages whose *Change it* starts a
  draft.
- **App:** More → Previews (a draft or what everyone sees, *Preview it as*, *Preview on a date*), the banner
  (hideable until the app next comes to the foreground), and Home as the Hub's tiles
  ([home.md](../features/home.md)). The phone previews from the signed-in account.

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
- **Undoing is deliberate, and can be taken back** (owner, 2026-11-04, note 20). A row under *Published
  recently* opens a sheet that says in plain words what will change ("Home screen goes back to how it
  was before “Link tiles and a library” was published, for everyone") with *Undo this publish*; there's
  no one-click button on the row. An undone publish stays in the list marked *Undone*, and its sheet
  offers *Publish it again*, which rolls back the undo batch (the undo's `Before` states are exactly what
  the publish made). Chosen over turning it back into a draft: a draft would need re-checking against
  everything published since and a second publish step, while rolling back the undo is exact, audited
  like any rollback, and one action. The list works out *Undone* from the history and from what's live: a
  publish is undone when its latest rollback hasn't itself been rolled back **and** the live placements no
  longer match what it left (a later publish can bring the same changes back; owner's bug on UAT,
  2026-11-04). A rollback that would change nothing writes no batch and is refused in plain words ("Nothing to
  change: the Home screen already looks as “Hub Fixes” left it."), shown under *Published recently*.
- **Inside a draft, undo and redo** (note 20): every change to the draft is a step in the editor's
  history (`DraftSession.StepBack`/`StepForward`), with Undo and Redo beside Share, ⌘Z and ⇧⌘Z (Ctrl
  on Windows; ignored while typing in a field or with a sheet open, so a field's own undo works), and a
  short "Undone. Redo" by the save status. Typing in one field is one step, and one gesture that changes
  several tiles (a drag) is one step. *Undo changes to this* on a tile and *Put back* are steps too, so
  they say what they did and offer Undo. The history lasts while the draft is open.
- **The draft's header never wraps its buttons** (owner, 2026-11-04): the buttons stay one row, beside the
  name or, when the name needs the room, under it. Below 640px of pane Share and More show as icons (with
  labels for screen readers and tooltips); below 380px Undo and Redo leave the header for More, where they
  are always listed, and the shortcuts still work. A draft waiting for a publisher shows *Waiting to publish*.
- Deleting a draft that's on for an event is refused until it's turned off.
- **The phone preview can't drift from the app:** Home draws through `HomeScreen` and each surface
  through `SurfaceViews` (Platform.Ui), which the preview shares. `PreviewRules` (Architecture.Tests)
  fails if the app's Home stops using `HomeScreen` or a Hub surface has no view. Keep everything a
  draft can change inside `HomeScreen` and register a view for every new surface. Home is only
  `HomeScreen`: the skeleton's Connection section is gone (owner, 2026-11-04), and being offline shows in
  the masthead's `SyncIndicator`, which draws only when Home is live.

## Publishing on a date (owner, 2026-11-09)

The owner: "I want an extra option to be able to schedule when it becomes published, for e.g. so my 2027
theme can be fully published on 01/01/2027." Turning a draft on for everyone confused them, so the sheet
(`ForEveryoneSheet`, opened from More) now offers three clearly different choices, as stacked segments
with a line each:

- **Turn it on now:** everyone sees it until it's turned off, or until an optional time; nothing is published.
- **Turn it on between two dates:** the event layer, scheduled, as before; both dates are needed here.
- **Publish it on a date:** at that moment it's published for good, exactly as *Publish now* would be,
  with nothing to turn off.

**How it works:** `Drafts.SchedulePublish` (gRPC) records `publish_at` and who scheduled it on the draft's
row; `CancelScheduledPublish` clears them. `ScheduledPublishing`, a background service in Configuration.Api,
checks every 30 seconds and publishes each due draft through the same code as *Publish now*, as the person
who scheduled it, so it happens with nobody in the admin panel and survives restarts. Two API replicas
can't publish one draft twice: publishing deletes the draft's row under its revision check, so the second
gets a conflict and finds nothing next time. Tested by `ScheduledPublishTests` on `TestClock`.

**Decided while building:**

- **It publishes what the draft holds at that moment,** including changes saved after scheduling.
- **Scheduling needs the publish permission for every area the draft touches,** and so does editing a
  draft once it's scheduled, since every save will go out to everyone (as with an event layer). The
  author, or anyone who sees every draft, can cancel it.
- **If it can't publish at its time, it doesn't, and it says so:** something it changes was changed for
  everyone since (the same check as *Publish now*), a name clashes, or whoever scheduled it can no longer
  publish an area. The draft stays as it is, keeps its date, shows "It didn't publish on … ." with the reason,
  is listed as *Didn't publish on …*, isn't tried again, and the person who scheduled it gets a notification
  (`config.drafts.not-published`). *Choose a new time* (after settling) or *Dismiss* clears it. Publishing
  over someone else's change silently was rejected: it would undo their work without anyone seeing.
- **A deleted draft** simply isn't published: deleting it cancels the schedule with it.
- **A draft that's also on as the event layer** can be scheduled to publish; publishing turns the layer
  off, as *Publish now* does.
- **Shown** on the draft ("Publishes on Friday 1 January 2027, 12am, for good.", with *Change the time*
  and *Don't publish it then*), in More, and in the Drafts list's own group *Publishing on a date*.

## Open

- **Scoped areas** (a ministry's page) work in the model; App styling lists global areas only.
- Whether a draft can also hold changes for a ministry leader's own ministry page, previewed by that
  leader: yes in the model; check the screens make sense for leaders.
- Drafts for feature records (events, sermons): not now, by decision 1.
