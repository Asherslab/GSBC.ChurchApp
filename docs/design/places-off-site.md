# Places off site

Detail behind [DESIGN.md](../../DESIGN.md) for choosing a place off site and showing it, on the phone and in the admin
panel. Read DESIGN.md first; its rules win. The decisions and the data: [locations.md](../features/locations.md). The
owner picked card A on the decision page (`.impeccable/mocks/places/`, 2026-11-17): a place is a row, the same
everywhere, and choosing one is a task of its own in a picker. B and C stay on the page.

The components are in Platform.Ui `Locations/`; their words are in `LocationWords`, their styles in `ui.css` (the
`location-` classes and `map-pin--tip`). Both are live in the gallery.

## Choosing: `LocationField`

- **Nothing chosen:** a quiet *Choose a place* button with the pin icon, under the field's label and description.
- **Chosen:** the place in a field box (Field fill, field edge, the field radius): the pin icon (or the church icon for
  At church) on a row tile, the name at 600 with the address and the note under it in ink 2 at caption size, and
  *Change* as a link at the right. `Optional` adds a quiet × that removes the place.
- **A location written before places had a map** shows its text as it was, with the line "Written before places had a
  map. Change it to put it on the map."
- `AllowHome` offers *This is someone's home* in the picker; `Recent` passes the places used before.

## The picker: `LocationPicker`

A wide `Sheet` (`Wide`, `ActionsInView`) titled with the field's label, the same on the phone and the desk.

- **At the top:** *At church* or *Somewhere else* (`Segmented`). At church asks only *Where at church*, with the hint
  "Leave this empty for the usual place. A room or a door helps people find it."
- **Left column** (15rem to 17rem, from a 720px window): the address search, focused when there is no pin yet. Under
  it, one of: the results, as `ListRow`s with the chosen one `Current`; the places used before, while the search is
  empty; "Nearest address to the pin" and that address, once the pin has been moved by hand; a sentence when nothing
  matches; or, offline, "Searching needs a connection. Type the address below, and choose it on the map later." The
  column ends with "Or tap the map to put the pin on the exact spot."
- **How far from church:** each result's second line ends with its distance and direction from church ("Brendale QLD
  4500 · 1 km north of church", or "near the church" under half a kilometre). That line is how a leader knows the
  right result without reading the map; the nearest address to a moved pin answers the same way.
- **Right column:** the map (220px high, the field radius, a hairline edge, sand while it loads) with the pin; a tap
  moves the pin, and a pin near the map's edge gets the next block's map ("Getting the map…"). Before anything is
  chosen the map's place holds a deep-aqua pin icon and "Choose an address and the map shows it here, with a pin you
  can move." Under the map: *Address*, *Name* ("Shown above the address. Leave it empty to show only the address."),
  *Note* ("Say where to park or which door to use.") and, where the record allows it, *This is someone's home* as a
  `ToggleRow` that says what everyone else will see.
- **Foot:** a `SaveBar`, *Cancel* bottom left and *Use this place* bottom right, held at the sheet's foot over a
  hairline while the columns scroll. It is blocked, with the reason above it, until there is an address, a name or a
  pin.
- Under 720px the columns stack: the search and its results, then the map and the fields. The brief planned a page of
  its own on the phone; the build uses the same sheet, and that is what is recorded here.

## Showing: `LocationRow`

A `ListRow` (class `location-row`): the pin icon (the church icon for At church), the name, the address or the suburb
under it, the note under that in ink 2 at caption size, and a 64px square of map at the end (tile radius, hairline
edge, no attribution, the pin at 16 × 22px). At church, a place with no pin and a hidden home have no square;
`ShowsMap="false"` leaves it off too. Tapping the row opens `LocationSheet`.

## The place's map: `LocationSheet`

A wide `Sheet` titled with the place's name over its address: the map (up to 420px high, 56% of the screen's height
at most, the card radius, a hairline edge, attribution under it), the note at reading width, then *Open in Apple Maps*
(primary, the directions icon) and *Open in Google Maps* (quiet, the external-link icon), full width. On Android it is
one *Open in Maps* through `geo:`, which offers the phone's own choice. Each hands over to that app, where Directions
is one tap away. The brief planned a full-screen page; the build uses the sheet.

## The pin

The shared `MapPin` with `map-pin--tip`, so the point of the pin sits on the place: a 28 × 38px teardrop in primary
(navy by day, bright aqua at night) edged in the page colour, with a page-coloured dot and a soft drop shadow. The
maps are our own base map (`mapStyle`, [maps.md](maps.md)) at zoom 13 in the row, 15 in the sheet and 16 in the
picker, from the kept map on the phone so they work offline.

## A home

A place marked as a home shows its street address, note, pin and map only to the people of its ministry or event.
Everyone else sees one line, "A home in" and the suburb, with "Only its own people see the street address. Ask a
leader if you need it." under it, and no map or sheet.
