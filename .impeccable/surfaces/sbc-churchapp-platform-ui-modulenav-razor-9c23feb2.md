---
version: 1
slug: "sbc-churchapp-platform-ui-modulenav-razor-9c23feb2"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/ModuleNav.razor"
related_targets: ["src/Hosts/GSBC.ChurchApp.Hosts.Admin/Layout/MainLayout.razor"]
---

# The admin sidebar, organised by area

Scope: the admin panel's sidebar (`ModuleNav`, the admin shell in `MainLayout`), Jump to (⌘K), pins, and the
narrow-window form. Mode: Operate, on a computer (desktop first), still usable on a narrow window.

- **Audience and job:** staff moving between admin pages. The Owner sees every page; several staff (pastors, the
  office) hold broad roles and see most of it (owner, 2026-11-09), others see a handful. Pages roughly double from
  today's 14.
- **Owner's answers (2026-11-09):** pinned pages, then areas that fold and remember; areas by job (Sundays &
  sermons, Calendar & news, The app, People, Church life later, You at the foot); Jump to alongside; pins saved
  with the account (gRPC), fold state per computer.
- **States:** signed out (Home, Account); nothing pinned (no Pinned section); a pin to a page your role no longer
  allows (hidden); an area folded with things waiting (orange count on the area); the area you're in (opens by
  itself); a few pages only (areas start open); Jump to with no match; narrow window (menu folds into a strip).
- **Constraints:** DESIGN.md's admin sidebar (navy, beige micro labels, orange marker and counts, 38px links),
  Sidebar Offset Rule, features declare their own entries and area, no per-feature code in the host, Access for
  every "who sees it".

## Direction contract

THESIS: The sidebar is a desk: what you use is pinned on top, everything else waits in drawers by job that fold
and remember, and anything is a ⌘K away. Refuses one ever-growing list and a separate settings maze.

OWN-WORLD: DESIGN.md's admin sidebar: masthead navy, beige uppercase micro labels, white-76% links with an orange
marker bar, orange count capsules; area heads as beige-chevron fold buttons in white 700; Jump to as a
masthead-tint field.

STORY: The office opens the admin, sees Announcements (2) pinned, sends one; checks Calendar & news's folded count;
presses ⌘K, types "health", lands on App health; its area opens by itself.

FIRST VIEWPORT: Mark and name; Jump to ⌘K; Home; PINNED (4 pages); EVERYTHING: five areas, one open, others folded
with counts; Account at the foot.

FORM: Your pages, then areas folded; position 7 of 7; seed:f7e60a5ef83688e2

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
