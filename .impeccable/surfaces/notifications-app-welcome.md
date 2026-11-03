---
version: 1
slug: "notifications-app-welcome"
primary_target: "src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.App/NotificationsWelcomeMoment.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/SheetHeading.razor","src/Features/Notifications/GSBC.ChurchApp.Features.Notifications.Client/NotificationSettings.razor","src/Features/ReleaseNotes/GSBC.ChurchApp.Features.ReleaseNotes.App/WhatsNewMoment.razor"]
---

# Notifications welcome (app)

Scope: the sheet that asks for notifications on the first open, while iOS's permission is undecided. Mode:
operate; a short interruption that must stay welcome. Phone (390pt), light and dark, both palettes, Dynamic Type.
Extends the What's new sheet (same cover band and heading) and reuses `NotificationSettings` for the choices.

Audience and job (owner, 2026-11-03): everyone who installs the app, signed in or not, often on a Sunday with
someone showing them. Notifications are core to the app. One clear yes, an easy "Not now", and then the few
choices they can make.

Decided (owner, 2026-11-03): shows once while iOS's permission is undecided; "Not now" is remembered on the phone
and doesn't use iOS's one-time prompt; it comes before What's new, which waits for the next open; browser builds
never show it; declining in iOS's prompt closes the sheet quietly; wording "Hear from the church".

## Direction contract

THESIS: One ask, in the church's voice, before iOS's own prompt: the app explains what it will send and only then
raises the system dialog, and the same sheet turns into the choices once the answer is yes. It refuses a cold
system prompt on launch, a multi-page onboarding carousel, and a second sheet stacked on the first.

OWN-WORLD: The existing shell's covered sheet: navy cover band with the mark at 7%, a 44px bell tile on the
masthead chip beside "Hear from the church" in headline type, a beige note line; the lead line at lead size;
a full-width primary capsule and a quiet "Not now" under it. The second step keeps the band ("Notifications
are on", note "Change these any time in More") over the settings' hairline toggle rows and Done.

STORY: I open the app for the first time, read one line about what the church sends, tap Turn on notifications,
say Allow, and see the one or two kinds I can turn off; I tap Done and I'm on Home.

FIRST VIEWPORT: Home behind a 38% scrim; the sheet from the bottom: cover band with bell tile, title and note;
the line; Turn on notifications (primary); Not now (quiet).

FORM: extension of the existing covered sheet (What's new). Concept round waived: new-work says not to roll for
a local extension or a precisely specified request, and the owner specified the structure (2026-11-03).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
