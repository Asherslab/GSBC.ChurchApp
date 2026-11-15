---
version: 1
slug: "each-admin-components-campaignsdesk-razor-e291a688"
primary_target: "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/CampaignsDesk.razor"
related_targets: []
---

# Admin panel: Outreach campaigns board

Scope: the Campaigns tab of the admin Outreach page (`/outreach`), the board and the campaign's record. Mode: operate.
Audience: the Outreach ministry's leaders and the Outreach Manager at a desk, a few times a month: to set up the next
campaign, see how the one on now is going and when it ends, and look back. Interviewed 2026-11-15 (#108, #100): cards
show dates and countdown, how far it's got, the next outreach day and what's handed out with its bundle size; the
wrap-up notice goes to everyone as an *Outreach announcements* kind (on by default); Preparing campaigns are hidden from
walkers. Decision page: `.impeccable/mocks/outreach-campaign-stages/` (A time lines with the record on its own page,
B cards open in place, C list by stage); the owner picked **B**.

## Direction contract

THESIS: Campaigns move left to right through their life (Preparing, Ongoing, Wrapping up, Finished) like the Life
Groups board, and a campaign opens where it sits: its column widens and the card becomes the record, so the board never
leaves the screen. It refuses the plain table and a record that hides the board.

OWN-WORLD: The admin bulletin: navy sidebar, white page, sand board columns with counts, 14px white cards on hairlines,
aqua for what's on now and the time line's progress, marker orange for wrap-up and for what's missing (no outreach
day), Montserrat, tabular figures. Shared Board, StagePipeline, fields, SaveBar; no new colours.

STORY: A leader sees Carols in Ongoing, 22% through with its wrap-up date, opens it in place, sets bundles of 40 and
7 days' wrap-up, saves; Remembrance flyers sits in Wrapping up with "Church told 13 Nov" and no day before it ends in
marker; Gympie waits in Preparing with no area yet.

FIRST VIEWPORT: Title band with New campaign; tabs; the General outreach strip (Always on, what's handed out and its
bundles, letterboxes this month, next day); four columns with counts; finished as lines with letterboxes; one card
open in a widened column with its stage line, three figures, fields, Cancel and Save.

SIGNATURE: the card's time line, start to end with today's dot and the wrap-up stretch in marker, so how long is left
reads before any words.

FORM: seed:00eec220f16c5747, dealt structure 2 of 3 (cards open in place), chosen by the owner.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
