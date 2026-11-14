# The app's shell

Detail behind [DESIGN.md](../../DESIGN.md) for the phone's top bar, tab bar, bell, masthead, section tabs, sheets, empty states, sign-in, previews and the sermon player. Read DESIGN.md first; its rules win.

## Navigation
- **Top-bar actions:** DESIGN.md, *Pattern map*; pressed or with their menu open they take the masthead chip fill.
- **Top bar:** back, centred title, actions. Transparent over the masthead, it turns solid navy once the big title reaches it (so the title slides under it, never under Back); the small title fades in once the big one is fully under, or past a per-page threshold (120px, 44px on Home), whichever is first. Hero pages use it solid. A page whose heading is its title (a post) uses `TopBarStyle.SolidTitleOnScroll`: solid, the small title shown only once the `data-page-title` heading is fully under the bar, so the title never shows twice. Back carries the parent's name, spoken "Back to" it; a page may give it its own action (the event editor's "Cancel"). A word action at the end ("Edit") is on-masthead text.
- **Tab bar:** up to five items: Home first, up to three chosen by staff (Sermons until they publish a change), More last; the active one is deep aqua and tapping it scrolls to the top. While audio plays, orange bars animate beside the Listen icon.
- **Notifications bell:** on every tab root, with an orange count badge; it opens the inbox, whose top bar has a sliders button to Notification settings.

## Masthead (signature)
The cover of every tab root (DESIGN.md, *Masthead First*): the display title (up to 13 characters a line), a beige note line (verse and date, a date range, a purpose), then an optional slot for the screen's live element.
- **Status strip:** on Sunday's, a clock or a pulsing live dot, the next service or "Live now", and a ghost or live button.
- **Now playing:** on Listen's, 16:9 artwork beside the title: the latest sermon with Listen and Watch, or, while audio plays, that sermon with an orange progress line and transport (elapsed, 15s back, play/pause, 30s forward, remaining).
- **Beside:** a slot on the title's row (the calendar's Week and Month switch).
- **Week rail:** seven day buttons (at least 44px) with a beige dot when something is on. The day in view takes the masthead chip; today is a white tile with an orange dot; past days are dimmed. Tapping a day scrolls the agenda to it.

## Section tabs (signature)
A sticky, sideways-scrolling row under the top bar on sectioned pages: the tab for the section in view turns ink under the sliding orange underline and stays visible. Tapping one smooth-scrolls to its section with a selection haptic. Sunday's five tabs (Welcome, Sermons, This week, Next, Give) fit 390pt.

## Sermon outline and player
- **Outline:** point headings, orange dot bullets, passage chips beneath; a "Main text" line leads.
- **Player:** a sand panel: play, position, 15s back, 30s forward, an orange scrubber.

## Sheet
A bottom sheet over the scrim with a grabber, heading and lead. At 720px and up it is centred (560px), and without a cover hides its grabber. With the keyboard up, DESIGN.md, *Layout*.
- **Actions:** DESIGN.md, *Pattern map* (Deleting): the phone stacks them full width, main on top; the admin panel puts the main one right, the back-out (Keep it, Cancel, Stay here) far left. A confirm says what goes with the thing and asks once.
- **Tools:** a sheet editing one item puts its ⋯ menu (Delete last, red) on the title's line (`Tools`), never its own row; the roster's sheets use it.
- **Wide:** a sheet with a whole list to choose from (the place picker) is 760px on wide screens.
- **Cover:** an optional navy band across the top in masthead colours. It says what the sheet is about, so a covered sheet is usually named by its label, not a title.
- **Paged sheet:** a few items, each with cover and body; orange page dots (44px targets) and a swipe turn the page. Actions: an optional per-page primary over Next, which reads Done last; with no action, Next is primary. Only a page change slides; one item shows no dots.
- **Sheet heading:** a covered sheet's head: an icon tile beside the title, with an optional beige note line. What's new and the notifications welcome use it.
- **What's new:** a paged sheet shown once after an update: the highlight's icon and title with the note ("New in 1.1"), then the summary, an optional 16:9 picture and the fact list. More → What's new lists every version, highlights on the warm tile. Model: [docs/architecture/changelog.md](../architecture/changelog.md).
- **Notifications welcome:** asks before iOS does, once, on the first open while the permission is undecided; it comes before What's new, and only one opening sheet shows per open. Step one: "Hear from the church" (note "Notifications"), a line on what the church sends, Turn on notifications (primary) over Not now (quiet). Turn on raises the system dialog; on Allow it slides to step two, "Notifications are on" (note "Change these any time in More"), the notification toggles and Done, without dots. Not now, or closing step one, is remembered and it doesn't return.

## Empty state
Centred: a deep-aqua icon, a title, a line, an optional quiet button.
- **A link that can't open (app):** a pushed page back to Home, its masthead naming the state ("Not available", "Not found") over an empty state saying why and what to do. Signed out and the place is for signed-in people: a primary Sign in. Not for this person, taken down or unknown: a quiet Go home. A file opened in the phone's browser: "Opened", with a quiet "Open it again".

## Sign-in and gates
- **Sign-in card:** a lifted card up to 460px wide on a sand stage. A quiet "App reviewer? Sign in with a code" switches to the code form.
- **Open in the app:** a shared link opened without the app is the sign-in card naming the place, with two short paragraphs on what the link does and how to get the app. A place for signed-in people is named only "A place in the church app"; one that's gone says "This isn't in the app any more". Signed-in staff also get a quiet "See it in Places in the app".
- **Gate:** a page needing sign-in or a permission the person lacks keeps its title band over an empty state in a sand box. A page that doesn't exist or fails shows the empty state alone, with a quiet button home or to try again.

## Previews
- **Preview banner:** an orange marker strip above the tab bar on every screen while a preview is on: an eye icon, the name of what is shown (optional caption), an underlined Hide and an Exit capsule. The page reserves room for it.
- **Phone frame:** a phone at its true point size (each supported iPhone's size, safe areas and cutout), with status bar and tab bar (the tabs everyone sees, from App styling, unless the page passes its own; `ITabBarNow`), scaled to its holder's width and the window height, or the holder's height when set (the Places column). It can carry its own palette; nothing inside responds to taps. Given `Back`, its top bar is a pushed page's solid bar with ‹ and that label, title held back (a post's *Preview on a phone*: "News" or the ministry's name); without, a tab root's bar.
- **Lock-screen proof:** the notification on a locked phone, "Time sensitive" in marker ink when urgent, the message clamped to four lines, placeholder words for empty fields.
- **Home screen and surface views:** the Home masthead and its Hub tiles are one component, and each surface registers one view; the app's Home and the admin preview both draw through them, so a preview is the real screen, not a copy.
