---
version: 1
slug: "eatures-events-client-whatsnexttile-razor-daee563f"
primary_target: "src/Features/Events/GSBC.ChurchApp.Features.Events.Client/WhatsNextTile.razor"
related_targets: ["src/Features/Events/GSBC.ChurchApp.Features.Events.Client/WhatsNextView.razor","src/Features/Events/GSBC.ChurchApp.Features.Events.Client/DigestEmail.cs"]
---

# What's next on Home

Scope: the What's next Home tile (`hub.whats-next`, Events), which takes over the Sunday bulletin stub tile's
placements in place, plus *Earlier* at the foot of the What's next page and the email block for Elvanto. Mode: Read,
on the phone. Decisions in docs/features/events.md (UAT feedback #17, 2026-11-16).

- **Audience and job:** anyone opening the app on Home, seeing at a glance the headline thing coming up at church and
  what follows, then tapping through to What's next.
- **Owner's answers (2026-11-16):** swap the bulletin tile in place; earlier digests (the last three) from the page's
  foot; Copy for email gives only the content block for Elvanto's HTML source; option B on the decision page.
- **States:** nothing sent or everything passed (the tile says so and opens the calendar's What's next empty state);
  one thing left; a lead with no picture; a lead that isn't on; long titles; every size from 1 × 1 to 4 × 3; light,
  dark and the 2026 theme.
- **Constraints:** Platform.Ui's `HubTile` frame, tones and go line; data from `DigestReader` through `LiveData` with
  `Remember`; no new colours.

## Direction contract

THESIS: The bulletin's front page grown up: navy, the headline thing as the title, what follows under a rule. Refuses
a second agenda list (This week already is one) and a turning carousel.

OWN-WORLD: The tile's own tone (Masthead by default, so navy with beige notes), Montserrat 700 headline at the poster
size, caption rows divided by hairlines in the tile's ink, the go line at the foot. The lead's picture only on top at
two columns and two rows or more.

STORY: On Sunday afternoon someone opens the app, sees "Baptisms · This Sunday · 10:30am" with Christmas Day under it,
and taps through to read What's next.

FIRST VIEWPORT: Where the bulletin was (1 × 2): "What's next" kicker, the lead's title large, its when line, two
following titles under rules, "Read What's next" at the foot. One row: "What's next" with "Baptisms this Sunday and 3
more". One cell: "What's next" and "4 things coming up".

FORM: option B of three dealt (headline and what follows), code-led from `.impeccable/mocks/digest-tile/b-headline.html`.
seed: none (unavailable).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and
every shipping raster carrying its provenance
