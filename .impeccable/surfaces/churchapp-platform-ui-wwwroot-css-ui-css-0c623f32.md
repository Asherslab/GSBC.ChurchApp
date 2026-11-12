---
version: 1
slug: "churchapp-platform-ui-wwwroot-css-ui-css-0c623f32"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css/ui.css"
related_targets: ["src/Hosts/GSBC.ChurchApp.Hosts.Admin/wwwroot/css/admin.css"]
---

# One design language: the app and the admin panel

Mode: Operate. Desktop first for the admin panel, phone first for the app; they need not look alike, only do the same
job with the same data (owner, 2026-11-12). Inherits DESIGN.md's world: navy sidebar, white and sand work areas,
Montserrat, the shared tokens. Variants are parameters on one component, about two per pattern, never copies.

- **Who and when:** every staff member and leader, on every admin page and phone screen. Owner: "buttons placed in the
  same places"; "i don't want to have to review the entire app again"; "make a proper rule or something to catch these".
- **Job:** each kind of task done one way, held by a shared component, a pattern map at the top of DESIGN.md, drift
  tests (ratchets) and a staff-only catalogue at /gallery on both hosts.
- **Owner's picks (decision page, 2026-11-12), in order:**
  1. List and record: filters, list and record (three panes); the filter rail only where a page has filters.
  2. Boards: one merged board for Feedback and Life Groups (r2-board-merged).
  3. Admin settings sections: each section on its own card (r3-sections-cards).
  4. Phone sections: inset grouped.
  5. Switching views: the underline rule tabs, everywhere.
  6. Toolbar: in the side rail, not above the list.
  7. Actions: Cancel bottom left and Save bottom right of anything that saves; Delete is never in the foot, it is an
     action on the item (the ⋯ menu in the item's head).
  8. Delete: always asks once, saying what goes with it; Keep it left, red Delete right; red first in the phone's stack.
  9. Adding: the new record opens in the record pane beside the list.
  10. Role (one of several): a role menu, a button showing the role that opens a menu of roles with what each allows,
      Remove in red. The role stays "Team" (owner, 2026-11-12: "team is fine").
  11. Steps a thing moves through: the pipeline.
  12. Icons: one icon per kind of thing (r12 "things"), checked by a test.
  13. Admin date and time: type it in the field, or the button at its right opens a calendar or time popover. The
      phone keeps the native wheel.
  14. A picture opened bigger: full window, near-black backdrop, arrows, filmstrip, Download.
  15. Admin light or dark: in the Account menu that opens from the sidebar's Account row.
- **Untouched by this station:** Ministries, Life Groups, Rosters, Prayer and Chats screens (wave 2), except free shared
  fixes.

## Direction contract

THESIS: One way per job, carried by Platform.Ui components and checked by tests; refuses per-feature layouts that
make each admin area feel like a new tool.
OWN-WORLD: The existing tokens and type; hairline-ruled three-pane workbench with a sand filter rail; white cards for
settings sections; underline tabs; navy primary at the bottom right, quiet Cancel bottom left; red only for delete,
inside the ⋯ menu and its confirm.
STORY: Staff learn a page once: filter on the left, choose in the list, work in the record on the right, Save bottom
right, more in ⋯; the next page works the same.
FIRST VIEWPORT: Sidebar, title band with the page's one add action, sand rail with filters and tools where needed, the
list, the record pane with its title, ⋯ menu and Cancel/Save foot.
FORM: "Filters, list and record", third of the three dealt (the roll led with "A pane that can open as a page");
seed:e2b3ec5db04589fe.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
