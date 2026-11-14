# Components

Which shared component draws each part of the design, and its main parameters. Every one is live in the gallery (`/gallery` on both sides). Read [DESIGN.md](../../DESIGN.md) first.

The system ships in `src/Platform/GSBC.ChurchApp.Platform.Ui`, and both hosts link it: the app in full,
the admin panel for tokens, type and the shared components. The admin shell, the office week's columns and admin-only page styles live in the admin host's `wwwroot/css/admin.css`; the day rail is shared and lives in `ui.css`. A feature's own screens are styled in its Client's `wwwroot/css` (the announcements desk and the inbox in `notifications.css`, the calendar in Events Client's `events.css`, families, the blue card and a person's Family and Safeguarding tabs in People Client's `people.css`). The agenda row, the week rail, the toggle chips and the masthead's Beside row are shared and live at the end of `ui.css`; the rich text editor and `RichTextField` (`rich-text-field`, `cm-md-*`) are styled in `rich-text.css`, the writing page (`writing-desk`) at the end of `ui.css` with its full-bleed rule in `admin.css`, and the post editor in `news.css`. The editor foot bar is shared in `ui.css`. Send feedback and the Feedback board are styled in Feedback Client's `feedback.css`.

- **Tokens:** `wwwroot/css/tokens.css` holds every literal colour: the semantic roles, redefined per
  `data-theme` and `data-scheme`, plus the palette-picker swatches. `ui.css` styles the components from
  the roles only. `Architecture.Tests` (`DesignTokenRules`) fails on a literal colour in any other
  stylesheet and on a px font size (the root fallback aside), which are the Roles Not Hexes and
  Dynamic Type rules.
- **Theme:** `wwwroot/js/theme.js` runs in `<head>` and sets `data-scheme` and `data-theme` from what
  the phone last knew, so nothing flashes. `AppTheme` (in the app's layout) reads Configuration's
  `app.appearance` area through the store, applies staff's palette at start-up and whenever the feed
  changes, and remembers it. `Appearance` holds the person's Light, Dark or Automatic and a local
  preview palette.
- **Shell:** `wwwroot/js/shell.js` and the `Shell` service: haptics, back with a fallback, the top bar
  turning solid past the masthead, edge-swipe back, scroll to top, pull to refresh, the status bar.

| Design | Component | Notes |
|---|---|---|
| Screen frame, top bar, back | `Page` | `BackHref` makes it a pushed page (back button, edge swipe); `OnBack` gives the back button its own action; `Actions` fills the top bar's end |
| Masthead | `Masthead` | Title, note, a slot for the live element, `Beside` for a control on the title's row, and a `Class` |
| Section and section head | `Section` | Title, lead, a "see all" link, a trailing slot; `Grouped` makes it a settings group (a card on the admin panel, an inset group on the phone) and `Foot` fills its foot |
| Callout, picture card, navy card | `Card` | `CardTone.Callout`, `Raised` (only where a picture leads, via `Media`), `Masthead` |
| Rows and boxed lists | `ListGroup`, `ListRow` | Leading tile tones, trailing text or content, chevron or external-link icon, disabled; `Reading` for entries to read, `TitleExtra` for a pill after the title, `Below` for more; `Unread` true or false sets the dot and the title weight; `Current` marks the open one (`aria-current`, `row--current`) |
| Pull to refresh | `PullToRefresh` | Wraps the whole page body, masthead included; the indicator sits under the top bar. Touch only, so offer a button too |
| Sheet | `Sheet` | Grabber, title, lead, body, actions; Escape and the scrim close it; centred on wide screens; `Cover` adds the navy band (then name it with `Label`); `Wide` for a list to choose from; opening moves focus into it without scrolling the page; it opens in a native `<dialog>` with `showModal()` (the top layer), so no scrolling, sticky or transformed ancestor can clip or hide it (Safari did), falling back to the fixed overlay where `showModal` is missing; opening focuses a child marked `autofocus` if there is one, and closing returns focus to where it was; `LayerClass` adds a class to the layer (Jump to's placement) |
| Sheet heading | `SheetHeading` | `Icon`, `Title` and an optional `Note`; goes in a sheet's `Cover` |
| Paged sheet | `PagedSheet` | `Count`, a `Cover` and `Page` per index, an optional `Action`; page dots, swipe, Next and Done |
| Facts | `FactList`, `Fact` | Label and text (or `ChildContent` for a rich value), with an optional icon tile; a fact with neither is skipped |
| Link in a row | `TextLink` | Deep-aqua label with a chevron and a haptic |
| Empty state | `EmptyState` | Icon, title, line, an action slot |
| Dates and times | `DateLine` | Church time, `Long`/`Short`/`Relative`/`TimeOnly`, ranges, "10:30am" and "6pm" as the bulletin writes them |
| Live/sync | `SyncIndicator` | Up to date, catching up, offline; plain or on the masthead |
| Buttons | `Button` | Primary, Quiet, Tinted, On masthead, Ghost on masthead, Live, Danger; block, small, icon-only, busy |
| Fields | `TextField`, `SelectField`, `ToggleRow`, `Segmented` | Labels tied to inputs; hint and problem lines are described-by; a `SegmentOption` can carry a `Count` |
| Copy a value | `CopyField` | Label, read-only value, Copy that turns to Copied, a polite status line; `ButtonOnly` is the copy button alone, `ButtonIcon` its icon (`link` by default, `copy` for a value that isn't a link) |
| Loaded data | `LiveData` | Loading, failure and stale states; reloads itself when what it depends on changes; `Remember` draws the last value at once when the screen is opened again (forgotten on sign-in, sign-out and access changes) |
| Loading | `Skeleton` | Rows, lines or a block, with a status for screen readers |
| Tab bar and More | `AppTabBar`, `MoreList`, `PhoneTabs` | The phone's tab bar draws the first four published tabs the person sees (`TabBar.For`), then More; More lists every module entry that isn't a tab now. Tabs are placements in the Tab bar area, chosen by staff in a draft |
| Admin sidebar | `ModuleNav`, `SidebarPlan`, `SidebarPins` | Grouped by `NavEntry.Area` (`NavAreas`: Sundays & sermons, This week, Ministries & groups, Safety, People & access, The app, Technical, and You at the foot); `SidebarPlan` lays out Home, Pinned, the areas (open or folded, with what's waiting) and the foot; `SidebarPins` keeps the account's pins; folds are remembered on the computer through `Shell`; `Counts` for the orange capsules |
| Jump to | `JumpTo` | A `Sheet` (`LayerClass` `jump-layer`) with a combobox over a listbox of up to 12 pages the person's role opens, matched on title, area and summary (`PickerMatches`); arrow keys and Enter; a polite count. `ModuleNav` hosts it and listens for ⌘K and Ctrl K |
| Icon picker | `IconPicker` (Configuration.Client) | The chosen icon and *Change icon*, opening the grid; uploaded icons and their upload for tiles, the app's own only for tabs |
| Admin title band | `TitleBand` | Title, lead or lead slot, back link, actions; sets the browser title |
| Admin tables | `DataTable`, `TableColumn` | Columns with a cell, an optional sort key and a class (`num`, `optional`, `quiet`, `truncate`); a toolbar slot, an empty slot, selection with the arrow keys |
| Table and detail | `SplitView` | The workbench without a rail (`workbench--stack`); only goes down |
| Search | `SearchField` | Labelled, settles before it reports the value |
| Filters | `FilterChips`, `FilterOption` | One choice from a set, with optional counts; `Rail` draws a group of rows for the workbench's rail, `ShowLabel` its label |
| Office week | `IOfficeWeekSource`, `OfficeWeek` | Features contribute tasks and dated spans (`Everyone`, `Layer`, `Draft`); `NavEntry.Summary` is an area's one-line purpose on the home page |
| Day rail | `DayRail` | `First`, `Today`, `Days` (7, or 14 for a fortnight), the spans, a hidden heading, the quiet line, a `Legend` of kinds and words, and the `Selected` href; a span whose end equals its start is a moment |
| Admin workbench | `Workbench` | `Aside` (the rail), main and `Detail` (the record) panes; `FoldAside` hides the rail while a record is open at 1360px and under; `Narrow` makes it the one-column form; `Showing` picks the pane in one column |
| Record head | `PaneHead` | `Title`, `Lead`, `Actions` (the ⋯ menu's `MenuItem`s), `Tools`, `OnOpen`, `OnClose` |
| More actions | `Menu`, `MenuItem`, `MenuItems`, `MenuButton` | `Label`, `Items` made with `this.Item(label, action, icon:, detail:, danger:, startsGroup:, @checked:, disabled:)`; `Text` and `Chevron` for a menu with words, `Block`, `Small`, `AlignStart`; `MenuButton` is one row in a hand-built panel |
| A small panel | `Popover` | `Label`, `Open`/`OpenChanged`; focus moves into it, Escape and a click outside close it |
| Roles | `RoleMenu`, `RoleChoice`, `RoleTick`, `RoleGives` | One role of several, with what each allows and an optional Remove; several at once as ticks |
| Boards | `Board`, `BoardColumn`, `BoardCard`, `BoardMove` | Columns (`Narrow`, `Folds`, `Shown` and `More`, `Addable`), `ColumnOf`, a `Card` template, `OnMove`, `OnAdd`, `Compact`; a card's `Shot`, `Meta`, `Labels`, `Open`, `Quiet` |
| Dates and times | `DateField`, `TimeField`, `DateTimeField` (`TypedDates`) | Typed with a calendar or time popover on the desk (`UiHost.Desktop`), the native field on the phone; `Min`, `Max`, `Hint`, `Problem` |
| A picture, bigger | `PictureOpen`, `PictureViewer` | `Pictures`, `Index`, `Noun`, `About`, `Open`; Download of the original |
| Light or dark | `SchemeChoice` | Automatic, Light, Dark through `Appearance`; `ShowLabel`, `Hint` |
| App icons | `AppIcons` (Platform.Contracts) | The one name for each kind of thing's icon; `IconRules` checks one icon per meaning |
| Gallery | `DesignGallery` | Every public component live with sample names and when to use it, at `/gallery` on both hosts |
| Pill | `Pill` | `PillTone.Marker`, `Accent`, `Masthead`, `Quiet`, `Danger` (run out only), with an optional icon |
| Charts | `StatTile`, `TrendLine` | `Quiet` for a value too small to show; a main and an optional comparison series, a table of every number, an `Empty` line under two points |
| Composer | `Composer` | `Label`, `Text`/`TextChanged`, `Placeholder`, `MaxLength`, `Rows`, `CanSend`, `Busy`, `SendLabel`, `SendIcon`, `OnSend`, `Class`, and `Attach`, `Above`, `Below` slots; `EnterSends` (Enter sends, Shift+Enter a new line) with `OnEscape` |
| Message bubble | `MessageBubble`, `BubbleReactions` | `Mine`, `Name`, `Time`, `Quote`/`OnQuote`, `Reactions`, `Status`, `OnLongPress`; `MoreLabel` adds the ⋯ beside it and makes the bubble focusable (Enter or the context-menu key opens it); `Tools` for what opens from it |
| Reorder list | `ReorderList` | `Label`, `Items`, `Key`, `Name`, an `ItemTemplate`, `OnReordered`, optional `Trailing` and `Preview`, `Disabled`, `Quiet` (hover handle, Alt+arrows, no ⋯ menu), `RowClass`, `Class` |
| Ranked list | `RankedList`, `RankedItem` | Title, explanation, label and value items, an `Empty` line; `Numbered` (default true) shows the rank |
| Save state | `SaveStatus` | Saved, waiting, saving, failed with a retry, not saved yet; an optional detail |
| Save action | `SaveBar` | Cancel (`OnCancel`, `CancelLabel`) at the left, the one save button at the right, the reason it can't save (`Blocked`) or its `SaveStatus` above; `New`, `OnMasthead`, `Block`, `Submit`; other actions as child content, after Cancel, moving above when narrow |
| Edits in children | `EditScope` | Re-renders the editor around it when anything inside is edited, so a header or a save button that reads a shared model stays current |
| Unsaved changes | `UnsavedGuard`, `UnsavedChanges` | Leaving a form with changes (another tab, page or sidebar link, Back, or an in-page switch that asks `UnsavedChanges`) opens a sheet: *Save and go on* (when the form can save), *Discard the changes*, *Stay here*; closing or reloading the browser tab gets the browser's own warning |
| Stages | `StagePipeline` | `Label`, `Stages` (`SegmentOption`s in order), `Value`/`ValueChanged`, `Disabled`, `ReadOnly`, `Class` |
| Sidebar counts | `INavCounts` (Platform.Client) | `Feeds` it depends on and `CountAsync` (a count per page href); `MainLayout` adds them to the office week's |
| Sidebar actions | `SidebarAction` (Platform.Client) | Title, icon, the permission it needs and the sheet it opens (given `Open` and `OpenChanged`); `ModuleNav` draws it in the foot above You |
| Tabs with counts | `TabStrip` | `TabOption` value, label and count; orange underline on the selected tab; a trailing slot; `Href` makes each tab a link to its area (the current one marked as the page); `Sticky` pins it on the phone |
| Preview banner | `PreviewBanner` | Docked above the tab bar; title, detail, Hide, Exit |
| Phone preview | `PhoneFrame`, `PhoneDevice` | A true-size device with status bar, cutout and tab bar, scaled to fit its space; an optional palette |
| Lock-screen proof | `LockScreenProof` | Title, body, `TimeSensitive`, the date line, and a caption slot |
| Home and surfaces | `HomeScreen`, `SurfaceViews` | The one way Home and the admin preview draw surfaces |
| Hub grid and tiles | `HubTiles`, `HubTile`, `HubIcon`, `HubTileViews` | The grid packs tiles by `HubGrid` (2 to 4 columns, up to 4 rows); `HubTile` draws a `TileShape` in a `TileTones` tone, with an optional `Banner` strip across its top; each kind's view picks its `TileVariant` |
| Hub canvas | `StylingPhone`, `HomePanel`, `TileSettings`, `AddTileSheet`, `HubLibrary` (Configuration Client) | Arranging, sizing and recolouring tiles in a draft; the Home screen panel; the settings panel with swatches, the layout picker and the contrast warning. `hub-canvas.js` carries the live drag and a port of `HubGrid`'s packing |
| App styling | `StylingWorkspace`, `StylingPhone`, `PreviewChips`, `StylingOverview`, `ColoursPanel`, `HomePanel`, `TabsPanel`, `DraftsMenu`, `DraftListing` (Configuration Client), `ThemeSwatches` (Platform.Ui) | The page, the phone with the Hub and Tab bar canvases, the preview chips, the panel's parts and the Drafts menu; styled in `drafts.css` |
| Choose several | `ChipPicker` | `Items`, `Chosen`, `Text`, an optional `Detail`, `Placeholder`, `Empty`; matches starting with the search come first |
| Who sees it | `AudiencePicker` (Content) | Everyone, Signed in, or several roles through `ChipPicker`; an audience it can't express shows as "Set up specially" |
| Choose a picture or file | `MediaChooser` (Content) | A wide `Sheet` around the registered `MediaPicker` (Media's `MediaChoice`); `ImagesOnly`; opens on the last folder used |
| Pick files | `FilePick` (Content) | `Label`, `Kinds` (`PickableFiles`), `Multiple`, `Limit`, `TooLarge`, `OnPicked` once per kept file, or `OnChosen` once with them all; `TakesPaste`, `DropOn` (a selector around it); `Input` for a caller that hands it files; child content (given whether it's busy) replaces the add row's icon and label |
| Show a picture, video or file | `MediaPreview`, `MediaVideo` | `Media`, `Controls`, `Size`, `Class`; a video plays inline and is muted without controls |
| Feedback | `SendFeedbackPage` (Feedback App); `AttachmentThumb` (Feedback Client); `SendFeedbackSheet`, `FeedbackBoard`, `TicketCard`, `TicketView`, `TicketPage`, `BlockedPhonesList` (Feedback Admin) | The phone's page with its stage and filmstrip; the admin panel's sheet; a chosen file's thumbnail in both; the board, the card, a ticket docked (`Docked`) or as a page, and blocked phones; styled in `feedback.css` |
| Prayer desk settings | `PrayedForTimes`, `PrayedTimes` (Prayer Admin) | The Settings pane of `PrayerDesk`: the timeline, the times (`PrayedTimes.Choices`, the day times 7am to 8:30pm) and their save, read through the shared digest schedule (`DigestSchedulesClient`); styled at the end of Prayer Client's `prayer.css` |
| Breadcrumbs | `Breadcrumbs`, `Crumb` | `Label`, a `Trail` of crumbs (label, key, icon), `OnOpen`; `CanDrop` and `OnDrop` make crumbs drop targets |
| Find someone | `PersonFinder` | `Find`, `ActionText`, `OnChosen`, `Leave` (ids to leave out), `Label`, `Placeholder`, `Busy` (the id being acted on), `AutoFocus`; the People sheet and the Family tab use it |
| Families and safeguarding | `AccountPeople`, `BlueCard`, `YourSafeguarding`, `ChildSheet`, `AddChildSheet`, `AskParentSheet`, `YourBirthDate`, `BirthDateAsk` (People Client); `PersonBirthDate`, `PersonFamilyTab`, `PersonSafeguardingTab`, `Safeguarding` (People Admin) | Your family and the blue card on Account (an `AccountSection`), your birth date in its You group (an `AccountYouPart`); a person's birth date in their title band (a `PersonFact`) and their Family and Safeguarding tabs (`PersonTab`); the Safeguarding page, whose Needs-you items come from `SafeguardingOfficeWeek` |
| Act on several | `SelectionTray` | `Label`, `Count` (hidden at 0), `One` and `Many` nouns, `OnClear`, the actions as child content |
| Media library | `MediaLibrary`, `FolderTiles`, `MoveToSheet` (Media Client) | The shelves, path row, tiles, grid or list, tray and folder sheets; `FolderTiles` has a `Compact` form for the chooser; `MoveToSheet` takes `Current`, `Suggested`, `Blocked` and an `Action` label |
| Places in the app | `PlaceDirectory`, `PlaceExplorer`, `PlacePhone`, `PlaceField` | Search, area chips and grouped rows; the directory beside the phone column with `PlaceAction`s (one `Leads`); a `PlacePreview` a feature registers, else the masthead summary; the picker field in a wide sheet. A holder with `data-fit-height` fits the phone frame to its height |
| Agenda row | `AgendaRow` | `Time`, `Title`, `Label`, `Subtitle`, `NotOn` (the circle-off line), `Quiet`, `Past`, `Warn`, `Selected`; a link, a button or plain |
| Week rail | `WeekRail` | `Days` (`RailDay`: date and whether anything is on), `Today`, `Here`, `Picked` |
| Choose several on and off | `ToggleChips` | `Label`, `Chips` (`ToggleChip`), `Toggled`, a `Trailing` slot; `toggle-chips--wrap` wraps instead of scrolling |
| Calendar | `DaySection`, `MonthGrid`, `EventHead`, `DateBlock`, `EventDetails`, `EventEditor`, `ComingUpTile` (Events Client) | The week, the month, an event and editing it on the phone and in the admin panel (`InPanel`); styled in `events.css`. The admin page is `CalendarWorkbench` with `CalendarSettingsPanel` and `BreakPanel` |
| Chats | `ChatsList`, `ChatView`, `ChatFrame`, `ChatPeople`, `ChatComposer`, `MessageActions` (Chats Client); `ChatsPanel` (Chats Admin) | One chat screen on both sides: `ChatFrame` is the phone's `Page`, or with `InPanel` a `PaneHead` head and a `Beside` pane; `ChatView` takes `InPanel`, `SettingsOpen`, `OnSettings`; `ChatsList` takes `Search`, `Current`, `ShowHint`; `ChatPeople` is the settings and people, in the phone's sheet or the panel's pane; `MessageActions InPanel` is a `Popover`; `ChatComposer` takes `EnterSends` and `DropOn`; styled in `chats.css` |
| Icons, mark | `Icon`, `BrandMark` | Lucide paths (ISC); the mark is the logo pack's file as a mask, never redrawn |

The Design gallery (`/gallery`, in the admin panel's Technical area and in the app's More, for staff who may send feedback) shows every shared component in the current palette. The drift tests (`DesignDriftRules`, `IconRules`, with their counts in `design-ratchets.json`) hold the pattern map.
