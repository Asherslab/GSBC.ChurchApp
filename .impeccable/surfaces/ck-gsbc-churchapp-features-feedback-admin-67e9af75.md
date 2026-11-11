---
version: 1
slug: "ck-gsbc-churchapp-features-feedback-admin-67e9af75"
primary_target: "src/Features/Feedback/GSBC.ChurchApp.Features.Feedback.Admin"
related_targets: ["src/Features/Feedback/GSBC.ChurchApp.Features.Feedback.Client"]
---

# The Feedback desk (admin panel)

Mode: Operate, desktop first. Inherits the admin panel's world (DESIGN.md: navy sidebar, white work area, title band).

- **Who:** mostly the owner, with Claude reading the same tickets through `tools/feedback/feedback.sh`; a staff member
  or two later. Looking at what came in and moving it along, not a busy team queue (owner, 2026-11-11).
- **Job:** every piece of feedback is a numbered ticket. See its pictures and videos, the words, what the phone sent
  with it; move it New → In progress → Fixed, to check → Resolved, or Not doing; keep notes; block a phone (and unblock
  it); delete a ticket. Pictures and videos are deleted when it's Resolved or Not doing.
- **Owner's steer (2026-11-11):** "more kanban, more GitHub like"; then "build the board with ticket docked, but make
  sure a command click opens the ticket as a page in a new tab".
- **States:** empty board; a column with nothing; ticket with no pictures; pictures deleted (closed); video; blocked
  phone; a phone-data copy (only for `feedback.phone-data.read`, never shown, downloadable); live arrival of a ticket.

## Direction contract

THESIS: A GitHub-Projects-style board of numbered tickets with the chosen ticket docked beside it; every card is also a
real link to the ticket's own page, so ⌘-click opens it in a new tab. Refuses the inbox list and the one-at-a-time triage.
OWN-WORLD: The admin panel's bulletin at the desk: navy sidebar with the orange current bar, white work area, status
columns as sand tracks at the 14px radius holding white 10px cards (#number in ink-3, the first line at 600, small
labels for build, platform and pictures), orange marker ring on the chosen card, the docked panel ruled off by a hairline.
STORY: They see what's new at a glance, open a card to look at its screenshots beside the board, read what came with
it, move it on and note why; a ticket they want to keep open becomes its own tab.
FIRST VIEWPORT: Title band "Feedback" with counts and a filter field; four status columns with counts (New, In
progress, Fixed to check, Resolved narrowed), Not doing folded to a vertical strip; the docked panel (about 420px) on
the right holding #number and title, the status control, the pictures, the words, the timeline of notes and status
changes, and the sent-with-it facts with Block this phone.
FORM: "Board with the ticket docked beside it", 4th of 7 on the second re-roll's list, fused with the 2nd ("each ticket
is a page") at the owner's word; seed:64dc00db6566c533. Signature move: dragging a card between columns moves its
status, and the card is a link: a click docks it, ⌘/Ctrl or middle click opens /feedback/{number} as a full page.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
