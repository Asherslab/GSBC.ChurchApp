---
version: 1
slug: "es-configuration-app-pages-previews-razor-be987925"
primary_target: "src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.App/Pages/Previews.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/PreviewBanner.razor"]
---

# GSBC app: Previews on the phone

Scope: More → Previews in the app, the preview banner on every screen, and (development builds only) linking the
phone to an account with a code from the admin panel. Mode: operate. Extends the existing app shell; no concept
round. The model is in docs/architecture/drafts.md.

Audience: staff, pastors and people a draft is shared with, holding their own phone, often while someone edits in
the admin panel; also checking a scheduled event layer or dated tiles before they start.

## Direction contract

THESIS: The phone shows the app exactly as it will be, and never lets you forget you're previewing. It refuses a
separate "preview mode" app or screenshots: it is the real app, re-composed.

OWN-WORLD: The existing app shell (DESIGN.md): masthead pages, hairline rows, segmented control, toggle rows. The
banner is the one new element: an orange marker strip docked to the top of the tab bar, ink on orange, with a
dark capsule Exit.

STORY: I pick "Christmas 2026" under More → Previews, the app re-draws at once, the banner says what I'm seeing;
as a colleague types in the admin panel my Home screen changes within a second or two; Exit puts it back.

FIRST VIEWPORT: Previews page: masthead "Previews", note "See the app as it will be, on this phone only"; rows:
What everyone sees, then each draft (yours, shared with you) with a check on the chosen one; Show it as (Me, A
visitor, A member); On a date (toggle and date). Banner above the tab bar on every screen.

FORM: extension of the existing shell, shaped with the owner 2026-11-02; seed: none (unavailable).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
