---
version: 1
slug: "platform-ui-locations-locationfield-razor-412abbce"
primary_target: "src/Platform/GSBC.ChurchApp.Platform.Ui/Locations/LocationField.razor"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/Locations/LocationPicker.razor","src/Platform/GSBC.ChurchApp.Platform.Ui/Locations/LocationRow.razor"]
---

# A place off site: choosing it and showing it (Platform.Ui, both sides)

**Mode:** Operate. **Audience:** staff at a desk and leaders on the phone choosing where something happens off site
(an outreach day's meeting point, an event, a Life Group in a home); everyone in the app reading where to go, often on
the way there. **Job:** choose a real place (address search over G-NAF, or a pin), with an optional name and note;
show it as an address with its note and a small map; open it bigger in the app's map, then in Apple Maps, Google Maps
or the like. "At church" stays a quick choice with no map (the site map comes later). A home shows its street address
only to the group's own people; everyone else sees the suburb (owner, 2026-11-17). Decisions: docs/features/locations.md.

**States:** at church; nothing chosen; searching (typing, results, no results, offline: type the address and note,
no search); a result chosen; a pin moved by hand (nearest address offered); a home; an old text location kept as it
is; a place whose map is still being cut; no map (outside every map, or none yet); showing offline from the kept map.

**Chosen:** card A, a row and a picker of its own (decision page `.impeccable/mocks/places/`, B and C kept).

## Direction contract

THESIS: A place is a row, the same everywhere: pin icon, name, address, note, and a small square of map at its end;
choosing one is a task of its own in a picker. Refuses the category default of a free-text box plus a separate map
step, and a map that takes over every page.

OWN-WORLD: The bulletin's hairline rows on white, deep-aqua pin icon, ink name at 600, ink 2 address and note; the
map square at the tile radius drawn by our own base map (`mapStyle`) with a navy pin edged in white. Picker: sand
fields, the At church / Somewhere else segmented, results as rows with the chosen one in Selected Fill.

STORY: A leader picks "Somewhere else", types a few words of the address, picks the result (how far from church
tells them it's the right one), nudges the pin to the car park, adds "Park at the far end", and saves. Everyone sees
the row on the event, taps it, sees the map and opens Directions.

FIRST VIEWPORT: Admin: the field shows the chosen row in a sand field with Change at its right; Change opens a Sheet
(Wide): the segmented at the top, then two columns: the search (focused) and its results on the left (230 to 280px),
the map with the pin on the right with Name and Note under it; Cancel bottom left, Use this place bottom right. Phone:
the same picker as a full page (Page), the search then results, the map under them, Name, Note and A home below; the
keyboard's search key. Showing: a row under the time on the record's page, 64px map square at the right.

FORM: structure 3 of 7 on my ordered list (the row and its own picker), dealt card A. seed:894ab0270fd923aa
Signature move: the result rows say how far and which way each is from church ("Eatons Hill · 4 km west of church"),
so the right one is obvious without reading the map; the pin drop answers with the nearest address the same way.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Open
- The app's map page for a place (full screen, Open in Maps, Directions) follows `Page Focused` and `MapView`.
