---
version: 1
slug: "eatures-events-client-whatsnextview-razor-d8eabd9c"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.Client/WhatsNextView.razor"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.App/Pages/WhatsNextPage.razor"]
---

# What's next in the app

Scope: the app's What's next page (`/whats-next`), the digest staff sent, opened from Places in the app, a link or a
notification. Mode: Read, on the phone. Decisions in docs/features/events.md, decision 18.

- **Audience and job:** anyone with the app, glancing at what's coming up at church in the next two weeks, then adding
  one to their calendar or registering.
- **Owner's answers:** "in the app's own design language, made of its own components, never a copy of the email"
  (2026-11-07); a picture leading each item (2026-11-08).
- **States:** nothing sent yet, or everything passed (empty state, link to the calendar); one week left; an item with
  no picture; a date that isn't on (callout); an item with Register; an item the phone has in its calendar (title links
  to the event, Add to calendar); private items only for people their calendar is for.
- **Constraints:** Platform.Ui's Page, Masthead, Section, Card (Raised for events, Callout for not on), MediaCover,
  Button, EmptyState; DESIGN.md's tokens; data through LiveData with Remember.

## Direction contract

THESIS: A short read of the next fortnight, week by week, each thing as a card with its picture: the church's
noticeboard in the app's own voice. Refuses the email's layout.

OWN-WORLD: The approved mock `.impeccable/mocks/digest/app.html`: navy masthead "What's next" with the range and a lead
line; This week / Next week sections with the week's dates; raised cards (cover, aqua label, title, navy when line,
description, place); a sand callout for a date that isn't on.

STORY: On Sunday afternoon someone taps What's next, sees the AGM on Thursday, the Kids break on Friday and the outreach
morning on Saturday, and adds the high tea to their calendar.

FIRST VIEWPORT: Masthead (title, range, "Six things coming up…"), This week with its range, the first card.

FORM: The app's pushed page with masthead, as the event page and This week are. Signature move: the week headings
move with the days, so the digest stays right all fortnight.

FINISH: unreviewed is unfinished; this build ends with the detector and the finish review.
