---
version: 1
slug: "ple-gsbc-churchapp-features-people-client-1ea5b6c1"
primary_target: "src/Features/People/GSBC.ChurchApp.Features.People.Client"
related_targets: ["src/Features/People/GSBC.ChurchApp.Features.People.Admin","src/Features/People/GSBC.ChurchApp.Features.People.App"]
---

# People: families, minors and parents, blue cards and training

Scope: Stage 1 of Ministries. The phone's Account page gains *Your family* and *Blue card and training* (the same
component on the admin panel's Account page); a person's page in the admin panel gains Family and Safeguarding
tabs; a Safeguarding page under People lists who's flagged, with lines in Home's Needs you. Mode: operate. Phone
for the Account sections; desktop first for the admin parts. Sketches and the decision page:
`.impeccable/mocks/people/decision.html` (built: phone-card, admin-groups, flagged-tiles; chosen by Claude
2026-11-10 while the owner slept, alternatives kept for the owner).

Audience and job: parents adding a child and making them a sign-in code; young people naming a parent; volunteers
checking their own blue card; office staff linking families and recording cards; later, ministry leaders.
Constraints: who sees what through Access; flags never block; entered by hand until an import exists.

## Direction contract

THESIS: A family and a blue card are things people already know in life, so the screens show them as such: the
people you look after as rows you act on, the card as a card. Refuses a settings form of birth dates and checkboxes.

OWN-WORLD: The bulletin: Account's hairline rows with 34px tiles, a waiting request on the warm tile first; the blue
card as a navy masthead-coloured card (14px radius, the mark's ring at 7%, number in tabular title-large, a Current
pill); admin tabs as the Roles tab's rows beside a sand panel; flags in orange-ink pills, run-out in danger.

STORY: I open Account, see Jordan's request and approve it, tap Leo, read out his sign-in code. At the desk I open
Safeguarding, see 2 run out, open Chris, type the new expiry, save.

FIRST VIEWPORT: Phone: masthead, You (name, birth date), Your family (waiting first, children, Add a child), then the
blue card card and the training row. Admin tab: birth date row, Waiting, Children, Parents, sand panel at 380px.
Safeguarding page: title band, four counts that filter, then one list soonest first.

FORM: phone 6 of 7 ("the card in your pocket"); admin 4 of 7 ("groups and a record"),
seed:951afd06c6d8; flagged 6 of 7 ("counts that filter one list"; seed: none (unavailable).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
