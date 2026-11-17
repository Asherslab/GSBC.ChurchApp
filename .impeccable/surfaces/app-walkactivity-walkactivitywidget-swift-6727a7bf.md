---
version: 1
slug: "app-walkactivity-walkactivitywidget-swift-6727a7bf"
primary_target: "src/Hosts/App/shell/ios/App/WalkActivity/WalkActivityWidget.swift"
related_targets: []
---

# Walk Live Activity (Lock Screen and Dynamic Island)

Scope: the iOS Live Activity for an outreach walk in progress, in the widget extension (SwiftUI). Visitor mode: Operate.

- **Audience and job:** a walker delivering letterboxes with the phone locked or in a pocket. They glance to know where
  they are, what side, how many houses until the turn, and tap a mark without unlocking.
- **Owner's answers (2026-11-17):** Finish opens the app on the walk's end screen; the phone lights up and buzzes only
  when the next step is a turn; a mark shows a line with Undo for about 12 seconds.
- **Content:** street; side and houses to go; the next turn and its letterboxes; letterboxes and distance so far; walking
  time. States: walking, paused, finding you (no fix yet), the route done, doorknocking (At a door opens the app), a mark's
  line with Undo.
- **Constraints:** Lock Screen card at most 160pt; Montserrat bundled in the extension; buttons are App Intents running in
  the app; Dynamic Island on black.

## Direction contract

THESIS: A navigation sign for a letterbox walk: the next turn and the houses until it sit in one white sign, so a glance
says how long this street lasts. It refuses the generic progress-bar card with a title and a row of grey pills.

OWN-WORLD: The masthead's navy card, white type, beige for the next-turn line, white-76% for the totals line. The sign is a
white rounded square with a navy turn arrow over the count. Buttons are capsules in white at 14% on navy; Finish is the
solid white round one with a navy tick. In the island the sign and Finish turn bright aqua on black.

STORY: The walker sees the street and side, sees how many houses before the turn, taps No junk mail or Spoke to someone
and sees the line with Undo, pauses or finishes.

FIRST VIEWPORT: Lock Screen card: a top line (Walking · 38 min left; 112 of 348 letterboxes · 1.2 km right); the 58pt sign
left with the street at title size, side · to go, and the next turn in beige beside it; four buttons in one row (two wide
capsules, Pause and Finish round). Expanded island: sign, street and side, to-go count right; the line; the buttons.
Compact: arrow and count left, street right. Minimal: the arrow.

FORM: Turn sign, number 4 of 7 on the ranked list. seed:43100c197a5dc895

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
