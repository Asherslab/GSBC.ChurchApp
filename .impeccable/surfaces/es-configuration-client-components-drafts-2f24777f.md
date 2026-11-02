---
version: 1
slug: "es-configuration-client-components-drafts-2f24777f"
primary_target: "src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Drafts"
related_targets: ["src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Admin/Pages","src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.App/Pages"]
---

# GSBC app: Drafts workspace

Scope: the drafts workspace in the admin panel (`/drafts`, desktop first) and its one-column form in the app (More →
Drafts): the event layer, the drafts list, the church-year view, editing a draft, sharing, publishing and turning a
draft on for an event. Also the read-only "What everyone sees" pages for each area (Home screen, Colours), whose
*Change it* starts or continues a draft. Mode: operate.

Audience: mostly office staff, every few weeks rather than weekly (sermons and similar arrive automatically), on a
desktop; each return must make sense without remembering how it works. Pastors and leaders preview and approve on
their phones. Owner, 2026-11-02: it must not feel like Git (no branches, diffs, merges, conflicts, revisions) and
must not feel like ceremony (a typo fix is edit, then Publish now). The owner chose both ways of seeing a draft: an optional phone preview beside the editor, and the real phone.

The model is in docs/architecture/drafts.md. Words on screen: "What everyone sees" (never "Live" where staff read
it), "Changed for everyone since you edited it" with *Keep mine* / *Use everyone's* (never "conflict").

## Direction contract

THESIS: One workbench for the whole job: drafts on the left, the chosen draft in the middle, its row's settings or
a phone preview on the right, with what everyone sees right now always stated at the top. It refuses the list page
that sends you to a separate editor and back, and every version-control idiom.

OWN-WORLD: The app's bulletin system (DESIGN.md): navy for the event-layer band and primary actions, a sand drafts
column beside a white work area, a lifted white settings panel, hairline rows, orange pills for "changed in this
draft" and for "changed for everyone since", aqua-deep for actions, Montserrat 400/600/700.

STORY: I come back after three weeks, see at the top what everyone sees now (and any event layer), find my draft or
start one, change a tile, watch it update in the preview and on my phone, then Publish now or Ask to publish.

FIRST VIEWPORT: Admin top navigation. Left column (about 340px, sand): Drafts / Church year switch; the event-layer
band (on, scheduled, none); Waiting to publish; Open (+ New); Published recently with Undo. Middle: draft name,
save state, Share, More, Publish now; area tabs with counts; rows with changes marked and the "changed for everyone"
note on its row. Right (about 400px): Settings / Phone preview switch, the selected row's form. Church year opens
the event layer and dated tiles across the coming eight weeks, full width.

FORM: the owner's steer after one dealt round: the three-pane workbench (dealt structure 1) as the main workspace,
with the church-year timeline (the dealt lead) as an optional view; raises from the gate-board challenger (a row
changed for everyone stays marked until seen) and the signage challenger (the event layer as one band);
seed:ccfdd5c4ad633b2e.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
