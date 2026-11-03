---
version: 1
slug: "bc-churchapp-features-notifications-admin-de737a5a"
primary_target: "src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.Admin"
related_targets: ["src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.Client/AnnouncementsDesk.razor","src/Platform/GSBC.ChurchApp.Platform.Ui/DayRail.razor"]
---

# Announcements (admin panel)

Scope: the admin panel's Announcements page (`/announcements`, `/announcements/{id}`, `/announcements/new`):
seeing what has gone out and what is scheduled, writing one, trying it on your own phone, then sending,
scheduling or asking for it to be sent. Mode: operate. Desktop first (1280 to 1920), light and dark.

Audience and job (owner, 2026-11-03): mostly the church office, a few a month, plus the occasional urgent
one (a cancelled service, a change of venue). Writing it well is what should be easiest. Staff aren't
technical. Senders hold `notifications.announcements.send`; anyone else with `.write` asks to send.

Content and ranges: title up to 80 characters, message up to 600 (phones show about four lines), an
optional link (a page in the app or a web address), who it's for (Everyone with the app, Everyone signed
in; ministries and abilities later), urgent or not, now or a scheduled time in church time. States: being
written, waiting to send, scheduled, sent, cancelled. Typically 0 to 3 announcements in a fortnight;
older sent ones are reread rarely.

Decisions: the rail is this week and next (Monday to Sunday twice), the same rail as Home's office week,
shared as one Platform.Ui component. Unsent announcements (being written, waiting to send) have no
day, so they sit as chips on one line under the rail. Older sent ones are in a "Sent earlier" list below
the desk. Waiting-to-send also shows on Home's Needs you.

## Direction contract

THESIS: Announcements opens on the fortnight: this week and next as the office-week rail, every sent or
scheduled announcement placed on its day, with the writing desk directly beneath. It refuses the generic
list-beside-a-form workbench.

OWN-WORLD: The admin bulletin desk: navy sidebar, white work area, title band; the sand rail of fourteen
white day columns at the 10px radius, today ringed in primary; sent announcements as primary capsules,
scheduled ones as marker capsules; unsent ones as marker chips; borderless sand fields, a navy lock-screen
proof at the sheet radius; navy primary capsule that names its reach ("Send to 240 phones").

STORY: I open Announcements, see what went out this week and what's booked for next, pick up the one I'm
writing or start a new one, write it while it appears on a lock screen beside me, check who it's for and
when, send it to my own phone, then send or schedule it, and its capsule lands on its day.

FIRST VIEWPORT: Title band "Announcements" with a one-line lead and "New announcement" at the end. A
compact rail (about 130px) of fourteen days with capsules; under it "Not sent yet" chips. Then the desk:
a writing column (Title, Message, then Who / When / Opens / Urgent as a checklist of hairline rows with
their reach and consequence), its actions at the foot; on the right a 320px lock-screen proof and the
inbox row. At 1280x800 the Title field is in the first viewport.

FORM: dealt lead, structure 3 of 7 ("The fortnight ahead"), chosen by the owner 2026-11-03;
seed:fc19cd033c111162. Signature move: announcements live on the fortnight rail; choosing a capsule
opens it in the desk, and setting a send time puts a "this one" capsule on its day as you choose it.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
