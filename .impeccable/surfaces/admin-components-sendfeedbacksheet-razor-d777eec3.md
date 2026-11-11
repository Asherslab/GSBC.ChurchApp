---
version: 1
slug: "admin-components-sendfeedbacksheet-razor-d777eec3"
primary_target: "src/Features/Feedback/GSBC.ChurchApp.Features.Feedback.Admin/Components/SendFeedbackSheet.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/ModuleNav.razor"]
---

# Send feedback (admin panel)

Mode: Operate, desktop first, usable in the narrow window. Inherits the admin panel's world (DESIGN.md: navy sidebar,
white work area, the shared `Sheet` as Jump to uses it).

- **Who and when:** anyone signed in to the admin panel who holds `feedback.send` (Owner, Feedback, Feedback Triage),
  mid-task on any admin page, noticing something broken, wrong-looking, or an idea. Owner (2026-11-11): "how do i submit
  feedback in the admin dash? (i.e. for admin dash feedback)"; "send feedback just above account in bottom left."
- **Job:** capture or add screenshots and videos, say what's wrong, send, back to work. Recorded by itself: the page
  (route and title), recent pages, browser and OS, window size, colour scheme, environment, admin build, the account.
- **Owner's answers (2026-11-11):** offer *Capture this page* (the browser asks to share the tab); after sending, thanks
  and close (no ticket number); no list of what you've sent.
- **States:** empty; capturing (waiting on the browser's prompt; refused or unsupported says so quietly); files chosen
  (up to 6, pictures 25 MB, videos 150 MB, too-large says which); dragging over the sheet; sending (uploads, then the
  send); couldn't send (the reason, everything kept); sent (quiet confirmation, sheet closes); over the day's guard.
- **Untouched:** the sidebar's other rows, Jump to, the board.

## Direction contract

THESIS: Feedback in a sheet over the page you're on, the same sheet Jump to uses: capture the page or drop a file,
say what's wrong, send, and you're back where you were. Refuses a separate feedback page that loses your place.
OWN-WORLD: The admin panel's sheet: white at the 22px radius over the 38% scrim, near the top of the window; headline
title with an ink-2 lead; *Capture this page* as a deep-aqua tinted capsule beside a sand *Choose files* capsule;
chosen files as 9px-radius thumbnails with round remove buttons beside the dashed sand paste-and-drop frame; the sand
text field; the info-led "Sent with it" caption; quiet Cancel and navy Send.
STORY: They see a problem, click Send feedback above Account, capture the page, type a sentence, send; a quiet
"Thanks, it's sent" and they're back on the same page.
FIRST VIEWPORT: The dimmed page with the sidebar's Send feedback row current; a 600px sheet: Send feedback, "About the
admin panel: something broken, looking wrong, or an idea?", Capture this page and Choose files, thumbnails beside
"Paste ⌘V or drop screenshots and videos · up to 6", Tell us about it, Sent with it, Cancel and Send.
FORM: "A sheet over the page", 2nd of the three dealt (the roll led with "The page is the screenshot");
seed:3b81bce67fb41dd0. Signature move: Capture this page grabs the page under the sheet as a screenshot, and ⌘V or a
drop anywhere on the sheet adds a file.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
