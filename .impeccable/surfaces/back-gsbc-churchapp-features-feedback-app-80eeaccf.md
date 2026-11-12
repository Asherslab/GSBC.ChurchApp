---
version: 1
slug: "back-gsbc-churchapp-features-feedback-app-80eeaccf"
primary_target: "src/Features/Feedback/GSBC.ChurchApp.Features.Feedback.App"
related_targets: ["src/Features/Feedback/GSBC.ChurchApp.Features.Feedback.Client","src/Hosts/App/GSBC.ChurchApp.Hosts.App/Pages/More.razor"]
---

# Send feedback (the phone)

Mode: Operate. Inherits the app's world (DESIGN.md, The Sunday Bulletin); composition only was chosen.

- **Who and when:** staff and testers with the Feedback role (More → Send feedback), and anyone on a verified phone
  through a triple tap on the church mark at the foot of More: often the owner holding a member's phone. Speed,
  nothing in the way (owner, 2026-11-11).
- **Job:** pick screenshots or videos from the photo library, write in your own words, send. The app records its
  version, the phone, the page you came from, who you are and the phone's state by itself. Ideas are welcome too; the
  words are generic and staff tell what it is from what's written (owner).
- **States:** empty; pictures chosen (up to 6; videos up to 150 MB, pictures 25 MB); keeping a file on the phone;
  sent (online) / waiting to send (offline) / couldn't send with the reason; over the day's limit; this phone blocked;
  not available here (no proven install and no role).
- **Untouched:** the More page apart from the mark's triple tap; the outbox and FilePick behaviour other pages rely on.

## Direction contract

THESIS: The screenshot leads. The first picture sits large in its own phone shape at the top so you see what you're
reporting while you write; refuses the generic form of stacked fields with an attachment row at the end.
OWN-WORLD: The app's bulletin: navy masthead with the mark's ring at 7% bleeding off the right, white page, sand fields
at the 12px radius, Montserrat, deep-aqua add tile, the frosted foot bar with a full-width navy Send.
STORY: They see what they chose, say what's on their mind in their own words, see one quiet line of what goes with it,
and send; it goes now or waits on the phone and says which.
FIRST VIEWPORT: Masthead "Send feedback" with a one-line note. Below, a stage: the first picture large at 9:16 (about
150 by 266pt) with a vertical filmstrip of the rest and the + tile beside it, a count caption under it; empty, the stage
is one tall dashed "Choose screenshots or videos" frame. Then the text field, the quiet "Sent with it" line with an
info icon, and Send docked in the foot bar.
FORM: "The screenshot leads", 3rd of 7 on the ordered list; seed:89df9deccaf2dfa5. Signature move: the chosen picture
as the page's hero in its true phone proportions, the filmstrip choosing which one is large.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Written like a message (2026-11-12)

Owner (#55): "The send sheet and such just take up so much space, especially with keyboard up. Doesn't look nice." Picked
**Like writing a message** on the decision page (`.impeccable/mocks/admin-nav-calendar/fb-composer.html`, code-led),
over a slim bar and Send at the end of the page. THESIS: feedback is a message, so it is written in a message box docked
on the keyboard, not a form. FIRST VIEWPORT: solid top bar; a short lead and *Sent with it*; the shared `Composer` at the
foot (paperclip, three-line field that grows, round Send), chosen files as a thumbnail row above it. Replaces the
masthead, the dashed picker, the stage and the sticky foot bar of 2026-11-11.

## Pictures back at the top (2026-11-12)

Owner (#59): "Looks way too barren now. Put the images back up top where they were before. But keep the textbox and send
like it is, looks decent". FIRST VIEWPORT: solid top bar; the 2026-11-11 picture area at the top (the dashed *Choose
screenshots or videos* tile, or the chosen one large beside its filmstrip and + tile, "1 of 6 chosen"); the lead and
*Sent with it*; the shared `Composer` at the foot, unchanged except that it has no paperclip, so pictures have one way in.
