---
version: 1
slug: "news-client-components-newspostform-razor-03194cc4"
primary_target: "src/Features/News/GSBC.ChurchApp.Features.News.Client/Components/NewsPostForm.razor"
related_targets: []
---

# Telling people again after changing a published post

Scope: saving a change to a published church news post, in the admin panel's writing page (`/news/{id}/change`) and the
phone's writing page (`/news/{id}/change` in the app). Mode: Operate. UAT feedback #12 and the owner's picks, 2026-11-16.

- **Who:** church news publishers (only they can change a published post).
- **Owner:** optional, off unless chosen; the same audience; words start from the post's notification and can be
  changed; steer on the decision page: "it should be a popup like changing a calendar event does".
- **Untouched:** the settings column and its Telling people group (it shows when it went and that it was sent again),
  the calendar's own Tell people sheet.

## Direction contract

Picks: the owner re-rolled the in-settings hand with the steer above, then chose "Tell people again, saying what
changed" (`.impeccable/mocks/news-all/t-a-what-changed.html`).

THESIS: The question comes when it matters: after Save, never as a setting you might forget is on. Refuses a toggle in
the settings that stays armed.

OWN-WORLD: The shared `Sheet` (a dialog at the desk, a bottom sheet on the phone) exactly as the calendar's Tell people
sheet: title, lead, `TextField`s, a `ToggleRow`, navy primary with the send icon, quiet Not now.

STORY: Staff fix the time in a post, Save; the sheet says it went to everyone (about 140 phones) on Monday and asks
whether to send it again; the words read "Updated: <title>" with a line to say what changed; they type it and press
Tell people again, or Not now.

FIRST VIEWPORT: The writing page behind the sheet: "Tell people again?", the lead, Notification, More, Urgent, Not now
at the left and Tell people again at the right (stacked on the phone, primary first).

FORM: seed:4f5d51dfbed69009 · what-changed, the dealt lead on the re-rolled hand. Signature: "Updated:" words that say
what changed, sent in place of the earlier notification.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
