---
version: 1
slug: "src-hosts-gsbc-churchapp-hosts-admin"
primary_target: "src/Hosts/GSBC.ChurchApp.Hosts.Admin"
related_targets: ["src/Features/Accounts/GSBC.ChurchApp.Features.Accounts.Admin","src/Features/Access/GSBC.ChurchApp.Features.Access.Admin","src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Admin","src/Features/Media/GSBC.ChurchApp.Features.Media.Admin"]
---

# GSBC admin panel: the desktop shell and its pages

Scope: the admin host's shell (sidebar navigation, signed-in and signed-out states, not-found and error
states, the home page) and every admin page: Drafts and its preview, People, Account, Welcome, Roles,
role detail, the role editor, the developer roles tool, Who can do what, and Media. Mode: operate.
Desktop first (1280 to 1920); a narrow window must not break, but phones are the app's job.

Audience: office staff who change the app's content every few weeks (drafts, home screen, colours,
fonts, pictures) at a desk; then the same staff inviting people and giving roles; pastors and leaders
reviewing. Not technical: every return must make sense without remembering how it works. Roles stay
staff-friendly; the raw pattern editor is a developer tool only (owner, 2026-11-03).

Owner's answers (2026-11-03): home is "what needs me"; the look is the bulletin, calmer, "but it's
definitely a desktop tool"; light and dark both, following the computer; long lists (People, Roles,
Media) are sortable, filterable tables with the chosen row's detail in a pane beside them, keyboard
up/down moving through.

## Direction contract

THESIS: The admin panel opens on the church's office week: what needs you, and what goes out when, on
the Week tab's seven-day rail, inside a persistent navy sidebar. It refuses the empty "Welcome, admin"
landing and the grid of tool tiles.

OWN-WORLD: The app's bulletin system translated to a desk: a navy sidebar with the mark, sand group
labels in beige, the orange marker bar on the current page and orange count capsules; a white work area
with a compact title band (no phone masthead), sand panels, hairline rows and tables, navy primary
capsules, aqua-deep links, Montserrat 400/600/700, tabular figures.

STORY: I open the admin panel, see this week (what publishes Friday, the event layer on Sunday) and the
two things waiting on me, open one, do it, and come back; or I pick an area from the sidebar and work
in a table with its detail beside it.

FIRST VIEWPORT: Sidebar 248px: mark and "Good Shepherd", Home, Content (Drafts, Media), People (People,
Roles, Who can do what), Account at the foot. Work area: "This week" title band with the date, a sand
seven-day rail of white day tiles (today outlined in navy) carrying dated items as small capsules, then
two columns: "Needs you" rows with their action, and "Your areas" with one-line purposes.

FORM: dealt lead (structure 4 of 7, "The office week"), locked by the owner on the decision page;
seed:688b593ad19aa56c.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
