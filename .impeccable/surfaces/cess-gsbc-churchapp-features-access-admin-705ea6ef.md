---
version: 1
slug: "cess-gsbc-churchapp-features-access-admin-705ea6ef"
primary_target: "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin"
related_targets: ["src/Features/Accounts/GSBC.ChurchApp.Features.Accounts.Admin","src/Platform/GSBC.ChurchApp.Platform.Ui"]
---

# Roles, redone: a person's roles and the people on a thing

Scope: People's per-person page (`/people/{id}`: Roles, What they can do, Account, Activity) and the
shared *People on …* sheet used on a calendar, a regular event and a draft (ministries later). Replaces
the Roles page, the role editor and Who can do what. Mode: operate. Desktop first; a narrow window
must not break. Admin panel only.

Audience and job (owner, 2026-11-09): office staff giving someone what their job needs, and a calendar's
editors adding a helper. "It feels painful"; "role names just don't feel easy to understand". Roles are
built in only, named *Feature Level*, ticked independently, each with an (i) saying what it lets them do;
roles for one thing are given on the thing. Decisions: independent ticks; built-in only (made-here roles
go); the Roles page goes; a page per person (decision round 1, `page`); a People button opening a sheet
on each thing (round 2, `sheet`). Role set as in `.impeccable/mocks/decision/roles/proposal.html`.

## Direction contract

THESIS: Access is given from the person or from the thing, never from a roles catalogue. It refuses the
flat list of 22 roles and the far-away "Give a role" picker.

OWN-WORLD: The admin's bulletin desk: title band with a back link, the 3px orange-underlined tab strip,
uppercase micro feature heads in ink-3, hairline tick rows with deep-aqua ticks, a sand explanation panel
at the 14px radius, sand quiet capsules for actions; the shared Sheet for the thing's people.

STORY: I open People, choose Ruth, tick Announcements Writer, glance at the sand panel to see what it
gives her, and I'm done. On the Kids calendar I press People, find Ben, tick Editor, Done.

FIRST VIEWPORT: Back to People; the name with email, joined and passkey; account actions at the end.
Tabs Roles · What they can do · Account · Activity. Roles: the tick list in two columns grouped by
feature, "For one thing" under it with links; the sand panel (about 400px) beside it explains the chosen
role. The thing: a People row ("Ruth and Ben · 2 people") opening the sheet: search to add, then each
person with that thing's roles as ticks and (i), Take off, Done.

FORM: dealt structure 3 of 7 ("A page for each person"), locked by the owner; then "A People button
that opens a sheet" for one thing; seed:603e3bc575a8571c.

FINISH: unreviewed and undocumented is unfinished; ends with the detector, the finish review and the verdict.
