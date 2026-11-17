---
version: 1
slug: "tures-events-admin-pages-digestdesk-razor-9230f653"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Pages/DigestDesk.razor"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Components/DigestItemEditor.razor", "src/Features/Events/GSBC.ChurchApp.Features.Events.Admin/Components/DigestAddSheet.razor", "src/Features/Events/GSBC.ChurchApp.Features.Events.Client/DigestEmail.cs"]
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

## Round 2 (UAT #22, #25, #26, #27, 2026-11-17)

Owner's answers: everything is added by hand (from the calendar, a church news post, or written for the digest);
the calendar picker lists everything in the dates by day, special things marked, filtered by calendar inside the
picker; the item editor saves with Cancel and Save. Decision page `.impeccable/mocks/digest-2/`: desk A (running
order beside a big preview), dialog B (fields beside how it will look, `e-with-look.html`), app A (a post is the same card as an event);
the email: staff choose per item (picture beside or across) and an optional photo across the top (owner in chat).

## Round 3 (UAT #29, 2026-11-17)

The email's top under a photo that already says What's Next. Decision page `.impeccable/mocks/digest-banner/`: the
dates alone as a dateline (A, `a-dateline.html`); the choice in a *Top of the email* box with the photo (H,
`h-top-group.html`). Built with the admin panel's `Segmented` for the one small choice in place of the sketch's inline
radios (the pattern map's control for it), stacked when the box is narrow. Only shown once there's a photo.

## Direction contract

THESIS: The digest is a short running order the office builds by hand from the calendar, church news and its own
words, beside the finished thing at a readable size. Refuses a pre-filled list to prune and a cramped side preview.

OWN-WORLD: The admin shell (navy sidebar, title band); a running-order list (grip, picture, where it came from,
label, title, when, ⋯) on the page's surface; the preview on a sand canvas with In the app | Email; aqua primary
Send, quiet Copy for email; a wide Sheet dialog for one item with its fields and a live app card and email row.

STORY: The office opens Weekly digest, adds Baptisms and Christmas Day from the calendar, the food pantry post from
church news, and a carols night by hand with a picture; rewords Baptisms in the dialog while watching its card;
chooses the picture beside or across for the email; copies it into Elvanto and sends it to the app.

FIRST VIEWPORT: Title band. Left (~600px): Saved line, From + How long, the range; the running order; From the
calendar, A church news post, Something else. Right: In the app | Email, the preview large on sand, Copy for email
and Send at its foot with the reach line.

FORM: Workbench (list + detail) with the detail as the preview; Sheet Wide for the item; Sheet Wide pickers.
seed:b8aa31578d046343

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md,
and every shipping raster carrying its provenance.
