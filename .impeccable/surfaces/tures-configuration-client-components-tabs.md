---
version: 1
slug: "tures-configuration-client-components-tabs"
primary_target: "src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Tabs"
related_targets: ["src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Drafts/DraftsWorkspace.razor", "src/Platform/GSBC.ChurchApp.Platform.Ui/Tabs"]
---

# GSBC admin panel: choosing the app's tabs on a phone canvas

Scope: the Tab bar area inside the Drafts workbench (`/drafts/{id}?area=app.tabs`), desktop first (1280 to 1920): the
phone with the draft's tab bar, the rail (Add a tab, Show the tabs for, the tabs in order, the tabs not on the bar),
and the chosen tab's settings. Mode: operate.

Audience and job: office staff, rarely (a season, an event, a new feature), deciding which places are tabs. Owner,
2026-11-08: any place can be a tab; five at most with More always last; who sees which can vary; taking a tab off
removes a link, not a page. Admin panel only.

## Direction contract

THESIS: the tab bar is chosen where it's seen: the app's own phone at true size, the tab clicked on its bar, its
settings beside it. It refuses a numbered form detached from the picture.

OWN-WORLD: the Hub canvas's world unchanged (sand rail, lifted white settings panel, orange marker for the chosen
thing, Montserrat 400/600/700, navy primary). Order is a numbered rail list with Move left and Move right, More
locked at its foot.

STORY: I open my draft on Tab bar, click Add a tab, choose This week, see it appear on the phone's bar, move it next to
Home, check it as a visitor and as someone signed in, and publish.

FORM: structure 1 of 3 on `.impeccable/mocks/decision/app-tabs/` (the phone is the canvas), with the ordered list
raised from structure 2; seed: none (unavailable): the three structures were dealt by hand, no concept-seed roll ran.

FINISH: finish review at the end of the build.
