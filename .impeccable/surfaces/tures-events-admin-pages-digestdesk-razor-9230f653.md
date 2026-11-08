---
version: 1
slug: "tures-events-admin-pages-digestdesk-razor-9230f653"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Pages/DigestDesk.razor"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Components/DigestItemWords.razor"]
---

# The weekly digest in the admin panel

Scope: the admin panel's Weekly digest page (`/digest`): the list made from the calendar, the chosen item's words, the
live preview (In the app | Email), Copy for email, Send to the app and Send to my phone. Mode: Operate, on a computer.
Decisions in docs/features/events.md, decision 18 and *Decided while building (the weekly digest)*.

- **Audience and job:** the office, once a week, before Sunday: check what the calendar pre-filled for the next two
  weeks, leave out what doesn't belong, put the big things first, reword one or two, copy the email and send it to the
  app. Holders of `events.digest` (Digest maker).
- **Owner's answers:** "build an actual thing, real. doesn't have to be sendable, but a real example" (2026-11-08); the
  app's digest in the app's own design language (2026-11-07); a picture leading each item is fine here (2026-11-08).
- **States:** a fresh digest (pre-filled, "Saves as you change it"); saved, not sent; sent as it is (Send to my phone
  offered); changed since it was sent (Send again); something new on the calendar since (New on the calendar pill); an
  item still needing details; an item from a private calendar; nothing special in these weeks; saving failed.
- **Constraints:** DESIGN.md's admin shell (navy sidebar, title band, SplitView for list + detail), Platform.Ui's
  Segmented, ToggleChips, Pill, SaveStatus, TextField, PhoneFrame; the app preview is the app's own component
  (`WhatsNextView`), never a drawing of it; no notification to anyone but yourself.

## Direction contract

THESIS: The digest is the calendar's next fortnight, edited, not written: a list you tick, order and reword, with the
finished thing beside it exactly as people will get it. Refuses a blank composer and a copy of the calendar.

OWN-WORLD: The approved mock `.impeccable/mocks/digest/admin.html`: white work column with tabular navy when lines and
aqua labels, the chosen row on sand, a lifted white words panel, the phone preview below it, quiet Copy for email beside
primary Send to the app.

STORY: The office opens Weekly digest on Thursday, unticks the men's breakfast, drags the AGM to the top, rewords the
outreach morning ("Fernleigh Park this time"), checks the email preview, copies it into the email tool, and sends it to
the app.

FIRST VIEWPORT: Title band "Weekly digest" with Calendar back; Saved · sent line; One | Two | Three weeks with the
range; calendar chips; the rows (grip, tick, picture, label, title with pills, detail, when). Right: the chosen item's
words, In the app | Email, the phone preview, Copy for email | Send to the app, the reach line.

FORM: SplitView (shared list + sticky detail), as the mock lays it out. Signature move: the preview is the real app
screen, so what staff see is what phones get. seed: none (no concept roll: the direction is the owner-approved mock, 2026-11-08).

FINISH: unreviewed is unfinished; this build ends with the detector and the finish review.
