---
version: 1
slug: "ures-notifications-client-inboxview-razor-001fb91d"
primary_target: "src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.Client/InboxView.razor"
related_targets: ["src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.App","src/Platform/GSBC.ChurchApp.Platform.Ui/InboxBell.razor"]
---

# Notifications inbox (app)

Scope: the phone's notifications inbox (`/notifications`, opened from the bell on every tab root and
from a tapped push) and the bell's badge. Mode: operate. Phone (390pt), light and dark, both palettes,
Dynamic Type.

Audience and job (owner, 2026-11-03): members of every age, mostly rereading what the church sent (an
announcement they swiped away); leaders and staff also get things that wait for them (a draft to
publish, later a roster). Waiting things come first, then what the church sent.

Content and ranges: 0 to 100 rows; a title (up to 80 characters) and a body (up to 600); a link that
opens the thing; unread on the row (signed in) or on the phone (signed out); urgent and test items.
Waiting items: most members have none; staff have 0 to 3. A waiting item is from a kind that asks
something of you, and it leaves Waiting when the thing is done (the draft is published or deleted),
wherever it was done.

## Direction contract

THESIS: The inbox opens on what waits for you: a Waiting | Everything switch under the masthead, on
Waiting while anything waits, and gone when nothing does. It refuses the flat, undifferentiated
notification feed.

OWN-WORLD: The phone bulletin: navy masthead with "Notifications" in display type and a beige note
("2 waiting for you · 3 new"), the mark's ring at 7%; a sand segmented control with an orange count
capsule; hairline reading rows; warm tiles (orange at 30%, orange-ink icon) for waiting items, quiet sand
tiles for notices; the 9px orange unread dot; a deep-aqua text link naming the action.

STORY: I tap the bell, see the one or two things that need me and what to do about each, do one and it
leaves Waiting; I switch to Everything to reread what the church sent, newest first, and mark it all read.

FIRST VIEWPORT: Top bar with back; masthead with title and the count note; the segmented switch
"Waiting (2) | Everything"; Waiting's rows, each with title, a line of detail, when, and a text link
("Open the draft"). With nothing waiting, no switch: Everything's rows directly, "Mark all as read" at
the section's end.

FORM: dealt lead, structure 4 of 7 ("Waiting and Everything"), chosen by the owner 2026-11-03;
seed:1c2ea59ba2bca8ba. Signature move: the switch opens on Waiting with its orange count and disappears
when nothing waits; something dealt with elsewhere leaves Waiting live.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
