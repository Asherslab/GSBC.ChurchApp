---
target: admin /digest page (DigestDesk)
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 2
target_identity: "file:/Users/asherp/Personal-Projects/Git/GSBC.ChurchApp.worktrees/feature-events-feedback/src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Pages/DigestDesk.razor"
target_fingerprint: "sha256:f41f28294572d52b577b77ca34b9fa7ffd7893af86281972db1156fa5e09a8ff"
target_path: /Users/asherp/Personal-Projects/Git/GSBC.ChurchApp.worktrees/feature-events-feedback/src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Pages/DigestDesk.razor
timestamp: 2026-11-09T09-15-23Z
slug: tures-events-admin-pages-digestdesk-razor-9230f653
---
Method: dual-agent (A: design review · B: detector + browser evidence), 2026-11-09, on the owner's state (From 16/11, Month) at 1280/1600/1970.

## Design Health Score
| # | Heuristic | Score | Key issue |
|---|---|---|---|
| 1 | Visibility of system status | 3 | Saving after a reorder has no row-level feedback |
| 2 | Match system / real world | 4 | Church's own words throughout |
| 3 | User control and freedom | 3 | No undo after "Take it out of the digest" |
| 4 | Consistency and standards | 2 | Phone toggle chips in the admin; when column differs per row |
| 5 | Error prevention | 3 | Send disabled with nothing ticked |
| 6 | Recognition over recall | 2 | Reorder arrows hidden until hover; range line jumps |
| 7 | Flexibility and efficiency | 2 | Keyboard reorder moves the wrong item (no @key) |
| 8 | Aesthetic and minimalist | 2 | Six solid navy chips; when text collides and spills |
| 9 | Error recovery | 3 | Notice + Retry |
| 10 | Help and documentation | 3 | Lead and hint repeat |
| **Total** | | **27/40** | Acceptable, notable issues |

## Design specificity
Specific in parts (copy, real WhatsNextView preview); the list row lost the mock's composure.
Detector (CLI): 0 findings. Live overlay: text-overflow on `.digest-row__when` (+141px) at every width; low-contrast `.pill--accent` 4.4:1; line-length on lead and hint; straggler on reach; overused-font and nested-cards are false positives (brand face, app preview cards).

## Priority issues
- [P0] When column `flex:none; width:13.5rem; nowrap` overflows past the divider and under the arrows; body squeezed to 53px at 1280. Fix: row grid with a wrapping when column and a fixed arrows track; container query for narrow lists.
- [P1] Calendar chips: phone ToggleChips (36px solid primary, scroll form) — six navy capsules shout and clip at 1280. Fix: wrap, quiet variant, caption label.
- [P1] Keyboard reorder: no @key; focus stays on the index, moving the neighbour next press. Fix: @key, refocus moved item, live announcement.
- [P2] Controls band loosely stacked (field 77px with label vs 42px segmented, range line jumping). Fix: one grid with visible labels and matched heights, range under.
- [P2] Arrows invisible until hover. Fix: always visible at ink-3.
- [P3] Native tick vs mock tick; pill overflow on the title line; segmented without arrow keys.

## Persona red flags
Office admin at 1280 laptop: long two-date items (the ones most often reworded) are the ones that break. Keyboard user: reorder moves the wrong item; segmented radio group is Tab-only.
