---
version: 1
slug: "tures-configuration-client-components-hub-32ff780a"
primary_target: "src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Hub"
related_targets: ["src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Drafts/DraftsWorkspace.razor"]
---

# GSBC admin panel: arranging Home on a phone canvas

Scope: the Home screen area inside the Drafts workbench (`/drafts/{id}?area=home.hub`), desktop first (1280 to 1920):
the phone canvas, the tile settings panel, adding a tile, the icon library (uploaded SVGs), and the grid's columns. The
app's own Home is a separate brief (`src/Platform/GSBC.ChurchApp.Platform.Ui/Surfaces`). Mode: operate.

Audience and job: office staff, every few weeks, rebuilding or tweaking Home for a season or an event; must make sense
without remembering how it works. Owner, 2026-11-04: every tile configurable (size in columns and rows, words, picture,
icon, colour from the theme, where it opens, who sees it and when), plain tiles and feature tiles, uploaded SVG icons
that follow the theme, all inside a draft, previewed on the phone and published as usual. Keyboard reordering and
resizing too. Desktop first; nothing of this goes into the app (AGENTS.md).

Decided by Claude while the owner slept (2026-11-04), from a dealt round on the decision page
(`.impeccable/mocks/decision/hub/`): the phone canvas, because the owner's brief names it; raises from the other two
cards: a "Not shown now" list under the phone (tiles hidden, dated or for another audience, which the canvas can't
draw) and a "Show it as" switch above it.

## Direction contract

THESIS: Home is arranged where it will be seen: the app's own Home at true size is the canvas, and a tile is moved,
resized and recoloured on it, never as numbers in a far-off form. It refuses the list-of-rows editor with a preview
off to the side.

OWN-WORLD: The drafts workbench as it is (sand drafts column, white work area, lifted white settings panel, Montserrat
400/600/700, navy primaries, deep-aqua actions). On the canvas: the selected tile outlined by the 2px orange marker
with an orange corner grip and a small navy size capsule ("2 × 1"); the grid's columns as faint hairline guides only
while dragging; theme swatches as real role chips named in staff words (Soft, Card, Top of Home, Buttons, Pale accent, Accent, Pale highlight, Highlight), with a plain warning only when words on one are hard to read (owner, 2026-11-04, note 9: no ratios).

STORY: I open my draft on Home, see the phone as everyone sees it, drag the bulletin under the sermon, pull its corner
to make it tall, click it, pick navy, change its words, check it on my own phone, and publish.

FIRST VIEWPORT: Drafts column on the left. Middle: the draft head and area tabs, then a toolbar strip (Columns 2 3 4 as
a segmented control, Show it as, Add a tile as the primary button), the phone centred at true size scaled to fit the
window height, and under it "Not shown now" rows. Right: the chosen tile's settings (kind and what fills it, size,
colour, icon, words, picture, opens, who sees it, dates, Take it out), or with nothing chosen, the grid's settings and a
hint.

FORM: dealt structure 2 of 7 ("The phone is the canvas"), chosen over the dealt lead because the brief pins the canvas;
raises from the lead (an ordered list for what the canvas can't draw) and from structure 7 (Show it as);
seed:5ac23732b3d42ef3.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Morning round (owner's notes, 2026-11-04)

Decided on `.impeccable/mocks/decision/hub-morning/` for the owner to react to: pictures are chosen in a
sheet (search, upload, thumbnail grid, Choose and Cancel), shared by every caller of `MediaChooser`; Who sees
it is Everyone, Signed in or Roles, roles found by typing and shown as chips (`ChipPicker`, `AudiencePicker`
in Platform.Ui); dragging reflows the grid live with a dashed orange landing slot, a lift of 1.04 and a deep
shadow, 200 ms glides, Escape or dropping off the phone to cancel, and no glide under reduced motion. The
settings column scrolls on its own beside the pinned phone; the toolbar has a Preview Light/Dark switch.

Finish review, morning round (2026-11-04): so the whole phone fits a 900px-tall window without dropping
below the legible 0.72 scale, the canvas's controls moved from a strip above the phone into a rail beside
it (Add a tile, Columns, Show Home for, Preview Light/Dark, Preview as on), the phone starting right under
the draft head. The size capsule on the chosen tile is orange, the Marker, like the outline and grip it
belongs to; it reads "2 wide, 1 tall", never "2 × 1". Column hairlines show only while dragging.

Round 2 (owner, 2026-11-04, notes 11-14): the page never scrolls on the canvas (the workbench is the
window's height; head and tabs stay, the area under them scrolls), so the phone is fitted to the height
under the head with a floor of 0.6 instead of 0.72, and the how-to caption moved to the rail. The drag's
target is the cell under the dragged tile's centre (20% leeway, 180 ms rest), and `HubGrid.Plan` picks the
order, swaps included, that lands it there moving no tile outside that cell where it can.

Round 3 (after the owner's recordings, 2026-11-04): the drag is built to iOS's home-screen edit mode. No slot
box and no column hairlines: the gap the tile left is the landing place. The lifted tile is a copy in a drag
layer over the phone's screen (above the masthead and the tab bar, under the status bar), 1.05 large with the
deep shadow, following the pointer 1:1, and the real cell goes invisible so its gap stays. The target is the
cell under the tile's top-left cell (20% leeway, 130 ms rest), every plan is made from the arrangement the
drag started with, and `HubGrid.Plan` lands the tile exactly there, letting a big tile give way downwards and
the row above fill in, with the fewest moves. Neighbours glide 220 ms; the drop settles on a gentle `linear()`
spring as the shadow fades; Escape glides everything back and scrolls the phone back smoothly. Holding near
the top or bottom scrolls every frame, faster nearer the edge, never past the end, and the grid stays still
until the scroll stops. Arrow keys move the chosen tile a cell in that direction through the same plan.
Reduced motion keeps the drag but drops the glides and the lift.
