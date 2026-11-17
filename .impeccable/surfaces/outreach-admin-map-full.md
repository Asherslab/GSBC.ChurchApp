---
version: 1
slug: "outreach-admin-map-full"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/Maps/MapWorkspace.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/map/map-workspace.js", "src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Admin/Components/PlacesJob.razor", "src/Hosts/GSBC.ChurchApp.Hosts.Admin/wwwroot/css/admin.css"]
---

# Outreach → Map: the full map, and adding an address to skip (admin)

**Mode:** Operate. **Audience:** outreach leaders planning routes and keeping the places outreach skips, at a laptop
(1280–1728). Part of the Map workspace (`reach-admin-components-mapworkspace-razor-e436dcaf.md`).

**Asked (owner, 2026-11-17):** #123 "more realestate … a maximised screen (not a full browser maximise) removes the tabs
at top, nav bar on left, coverage/places/areas nav bar"; #118 "why would I put info for one form in another form".
**Interview:** jobs stay switchable while full; search and form stay separate; the Places drawer's search finds places
already skipped. **Decision page:** first hand re-rolled with the steer "floating panel, but allow the user to also move
it to the right side"; then picked *Drag it by its head to either side* (`.impeccable/mocks/outreach-map-max/drag.html`)
and *Matches in the card and numbered on the map* (`.impeccable/mocks/outreach-skip-address/pins.html`).

**States:** normal; full (panel open / folded, left / right, mid-drag with the drop place lit); a menu, sheet or drawing
open (Esc belongs to them); the skip card with nothing typed, under three letters, matches, none, an address chosen.

## Direction contract

THESIS: The map owns the window and the job's panel is a card you can carry to either side, so planning never fights
the admin chrome. Refuses both a browser-fullscreen toggle and a fixed side rail.

OWN-WORLD: The admin's own: page-coloured cards on the map's Pop shadow, the panel's head a row with the job as a menu
(icon, name, chevron), a grip and the fold chevron; mid-drag the panel lifts with an aqua edge and the far side shows a
dashed aqua drop place. Numbered match pins in accent with a page ring, the same numbers in the card's list.

STORY: The planner clicks Full map at the map's top right; the nav, heading, tabs and rail go; the panel floats on the
left. They drag it by its head to the right, plan, press Esc and everything comes back. In Places they add an address:
type 18 Tan in the card, see 1–3 on the map, click 1 and fill in why.

FIRST VIEWPORT: full: map to every edge; the panel 440px (320 for other jobs) floating 16px in from the left or right,
top to the attribution; Leave full map · Esc top right of the map (beside the panel when it's on the right); tools and
cards keep clear of the panel. Skip card: Find the address (hint: or click its letterbox on the map) above the form.

FORM: structure 2 of 7 on my re-rolled list (drag by the head, snap to a side), owner-steered. seed:e8ebed83ad6c3767
Signature move: lift the panel by its head and set it down on the other side; the map underneath never reloads.

ADAPTATIONS (recorded at the finish review): *Move the panel right/left* in the job menu, the keyboard way to do
what the drag does; the skip card's foot says "Choose the address first." rather than repeating the field's hint; up
to three matches within 15 km of the view are numbered and framed, the rest named by suburb as off the map, and when
none are near, the three nearest are shown and framed instead.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
