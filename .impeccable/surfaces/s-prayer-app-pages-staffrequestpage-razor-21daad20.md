---
version: 1
slug: "s-prayer-app-pages-staffrequestpage-razor-21daad20"
primary_target: "src/Features/Prayer/GSBC.ChurchApp.Features.Prayer.App/Pages/StaffRequestPage.razor"
related_targets: ["src/Features/Prayer/GSBC.ChurchApp.Features.Prayer.App/Pages/YourRequestPage.razor","src/Features/Prayer/GSBC.ChurchApp.Features.Prayer.Admin"]
---

# Prayer: a request's page (phone, staff and the person's own)

Scope: Operate. `/prayer/requests/{id}` (prayer wall team and pastoral team on the phone) and `/prayer/yours/{id}` (the
person's own request); the admin desk's record pane follows the same order inside the workbench.

- **Audience and job:** a lay approver or pastoral team member, on their phone, deciding whether a request goes on the
  wall and moving the pastoral care along (owner feedback #43, #44, 2026-11-11). The person sees where their own request is.
- **Chosen (owner, decision page 2026-11-12):** "Status band, then cards" (`.impeccable/mocks/prayer-request/band.html`).
- **Answers (owner, 2026-11-12):** the pipeline is the pastoral care steps New, Followed up, Answered; the wall stays its
  own group. The person's page shows the same kind of steps, read only. Names default to "Sam S." for everyone.
- **Open:** none.

## Direction contract

THESIS: a request reads like a ticket: where it stands first, in one sand band, then their words, then each job in its
own group. Refuses the old single column where actions, the wall form and notes ran together undifferentiated.

OWN-WORLD: the app's bulletin world unchanged: a band in `--sand-strong` as the theme renders it (Themes.cs mixes 8% of the masthead navy into it, so it reads a cooler sand than the page ground, which sets it apart) under the solid navy top bar, the shared
StagePipeline in deep aqua, white inset groups on the group ground with caption headings in ink-2, field fills in sand,
Save bottom right with the quiet second action at the left.

STORY: the approver sees who and when and where it stands (the care steps, or for a wall-only approver one strong status line) at a glance, reads the words, then decides the wall in its
group, then contact and notes. The person sees Sent → On the prayer wall → Answered and acts from one wide button, with taking it down and Delete in the band's ⋯ menu (the pattern map).

FIRST VIEWPORT: top bar; sand band full width with the meta line (sent, name, Under 18 pill) and the three-stage
pipeline; their words at lead size; "The prayer wall" group heading with the form starting.

FORM: dealt structure 2 of 7 (status band, then cards); seed:109534d9fa1eee86.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
