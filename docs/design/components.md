# Components

Which component draws each part of the design, and the parameters that change its look or behaviour; the code and the gallery (`/gallery`) show the rest. Read [DESIGN.md](../../DESIGN.md) first.

The system ships in `src/Platform/GSBC.ChurchApp.Platform.Ui`; the app links all of it, the admin panel its tokens, type and shared components. Shared styles are in `ui.css` (with the day rail, agenda row, week rail, toggle chips, the masthead's Beside row, the editor foot bar and `writing-desk`), the rich text editor and `RichTextField` in `rich-text.css`. The admin shell, the office week's columns, `writing-desk`'s full-bleed rule and admin-only pages are in the admin host's `wwwroot/css/admin.css`. A feature's screens are styled in its Client's `wwwroot/css`: `notifications.css` (announcements desk, inbox), `events.css` (calendar), `people.css` (families, the blue card, a person's Family and Safeguarding tabs), `news.css` (post editor), `feedback.css` (Send feedback, the Feedback board).

- **Tokens:** `wwwroot/css/tokens.css` holds every literal colour: the semantic roles per `data-theme` and
  `data-scheme`, and the palette-picker swatches. `ui.css` uses roles only. `DesignTokenRules` fails on a literal
  colour in any other stylesheet and on a px font size (the root fallback aside).
- **Theme:** `wwwroot/js/theme.js` runs in `<head>` and sets `data-scheme` and `data-theme` from what the phone last
  knew, so nothing flashes. `AppTheme` applies staff's palette from Configuration's `app.appearance` feed at start-up
  and on change, and remembers it. `Appearance` holds the person's Light, Dark or Automatic and a local preview palette.
- **Shell:** `wwwroot/js/shell.js` and the `Shell` service: haptics, back with a fallback, the top bar turning solid
  past the masthead, edge-swipe back, scroll to top, pull to refresh, the status bar.

| Design | Component | Notes |
|---|---|---|
| Screen frame, top bar, back | `Page` | `BackHref` makes a pushed page (back, edge swipe); `OnBack`; `Actions` |
| Masthead | `Masthead` | Title, note, live-element slot; `Beside` for a control on the title's row |
| Section and section head | `Section` | Title, lead, "see all" link; `Grouped` is a settings group (a card on the admin panel, an inset group on the phone), `Foot` |
| Callout, picture card, navy card | `Card` | `CardTone.Callout`, `Raised` (only where a picture leads), `Masthead` |
| Rows and boxed lists | `ListGroup`, `ListRow` | Tile tones, chevron or external-link icon; `Reading`, `TitleExtra`, `Below`; `Unread` sets the dot and weight; `Current` marks the open one (`aria-current`) |
| Pull to refresh | `PullToRefresh` | Wraps the whole page body, masthead included. Touch only, so offer a button too |
| Sheet | `Sheet` | Escape and the scrim close it; centred on wide screens; `Cover` adds the navy band (name it with `Label`); `Wide`; `LayerClass`. A native `<dialog>` via `showModal()` so no ancestor clips it; focuses an `autofocus` child and returns focus on close |
| Sheet heading | `SheetHeading` | `Icon`, `Title`, `Note`; goes in a sheet's `Cover` |
| Paged sheet | `PagedSheet` | `Count`, `Cover`, `Page`, `Action`; dots, swipe, Next and Done |
| Facts | `FactList`, `Fact` | A fact with neither text nor `ChildContent` is skipped |
| Link in a row | `TextLink` | Deep-aqua label, chevron, haptic |
| Empty state | `EmptyState` | Icon, title, line, action slot |
| Dates and times | `DateLine` | Church time, `Long`/`Short`/`Relative`/`TimeOnly`; "10:30am" and "6pm" as the bulletin writes them |
| Live/sync | `SyncIndicator` | Up to date, catching up, offline |
| Buttons | `Button` | Primary, Quiet, Tinted, On masthead, Ghost on masthead, Live, Danger; block, small, icon-only, busy |
| Fields | `TextField`, `SelectField`, `ToggleRow`, `Segmented` | Labels tied to inputs; hint and problem lines are described-by |
| Copy a value | `CopyField` | Copy turns to Copied, with a polite status; `ButtonOnly`, `ButtonIcon` (`copy` for a value that isn't a link) |
| Loaded data | `LiveData` | Loading, failure, stale; reloads when what it depends on changes; `Remember` shows the last value at once (forgotten on sign-in, sign-out and access changes) |
| Loading | `Skeleton` | With a status for screen readers |
| Tab bar and More | `AppTabBar`, `MoreList`, `PhoneTabs` | The first four published tabs the person sees (`TabBar.For`), then More with every other entry; tabs are chosen by staff in a draft |
| Admin sidebar | `ModuleNav`, `SidebarPlan`, `SidebarPins` | Areas (`NavAreas`): Sundays & sermons, This week, Ministries & groups, Safety, People & access, The app, Technical, and You at the foot; pins per account, folds per computer; `Counts` for the orange capsules |
| Jump to | `JumpTo` | Up to 12 pages the person's role opens, matched on title, area and summary; arrow keys, Enter, a polite count; ⌘K and Ctrl K |
| Icon picker | `IconPicker` (Configuration.Client) | *Change icon*; uploaded icons for tiles only, the app's own for tabs |
| Admin title band | `TitleBand` | Title, lead, back link, actions; sets the browser title |
| Admin tables | `DataTable`, `TableColumn` | Column classes `num`, `optional`, `quiet`, `truncate`; optional sort; arrow-key selection |
| Table and detail | `SplitView` | The workbench without a rail |
| Search | `SearchField` | Settles before it reports |
| Filters | `FilterChips`, `FilterOption` | One choice from a set, optional counts; `Rail` for the workbench's rail |
| Office week | `IOfficeWeekSource`, `OfficeWeek` | Features contribute tasks and spans (`Everyone`, `Layer`, `Draft`); `NavEntry.Summary` is an area's one line |
| Day rail | `DayRail` | `Days` 7 or 14; `Legend`; `Selected`; a span ending where it starts is a moment |
| Admin workbench | `Workbench` | `Aside`, main, `Detail`; `FoldAside` hides the rail while a record is open at 1360px and under; `Narrow`; `Showing` |
| Record head | `PaneHead` | `Title`, `Lead`, `Actions` (the ⋯ menu), `Tools`, `OnOpen`, `OnClose` |
| More actions | `Menu`, `MenuItem`, `MenuItems`, `MenuButton` | `this.Item(label, action, icon:, detail:, danger:, startsGroup:, @checked:, disabled:)`; `Text`; `MenuButton` is a row in a hand-built panel |
| A small panel | `Popover` | Focus moves in; Escape and a click outside close it |
| Roles | `RoleMenu`, `RoleChoice`, `RoleTick`, `RoleGives` | One role, with what it allows and an optional Remove; or several as ticks |
| Boards | `Board`, `BoardColumn`, `BoardCard`, `BoardMove` | Columns `Narrow`, `Folds`, `Addable`; `OnMove`, `OnAdd`, `Compact` |
| Dates and times | `DateField`, `TimeField`, `DateTimeField` (`TypedDates`) | A popover on the desk (`UiHost.Desktop`), the native field on the phone |
| A picture, bigger | `PictureOpen`, `PictureViewer` | Download of the original |
| Light or dark | `SchemeChoice` | Automatic, Light, Dark |
| App icons | `AppIcons` (Platform.Contracts) | One icon per meaning (`IconRules`) |
| Gallery | `DesignGallery` | Every public component live, with when to use it |
| Pill | `Pill` | `PillTone.Marker`, `Accent`, `Masthead`, `Quiet`, `Danger` (run out only) |
| Charts | `StatTile`, `TrendLine` | `Quiet` for a value too small to show; a table of every number; an `Empty` line under two points |
| Composer | `Composer` | `EnterSends` (Enter sends, Shift+Enter a new line), `OnEscape`; `Attach`, `Above`, `Below` slots |
| Message bubble | `MessageBubble`, `BubbleReactions` | `Mine`, `Reactions`, `Status`, `OnLongPress`; `MoreLabel` adds the ⋯ and makes it focusable (Enter or the context-menu key) |
| Reorder list | `ReorderList` | `OnReordered`; `Quiet` (hover handle, Alt+arrows, no ⋯ menu) |
| Ranked list | `RankedList`, `RankedItem` | `Numbered` (default true); an `Empty` line |
| Save state | `SaveStatus` | Saved, waiting, saving, failed with a retry, not saved yet |
| Save action | `SaveBar` | Cancel left, the one save button right, `Blocked` or `SaveStatus` above; other actions after Cancel, moving above when narrow |
| Edits in children | `EditScope` | Re-renders around any edit inside, so a header or save button stays current |
| Unsaved changes | `UnsavedGuard`, `UnsavedChanges` | Leaving with changes opens a sheet: *Save and go on* (when it can save), *Discard the changes*, *Stay here*; closing the browser tab gets the browser's warning |
| Stages | `StagePipeline` | `Stages` (`SegmentOption`s in order), `ReadOnly` |
| Sidebar counts | `INavCounts` (Platform.Client) | A count per page href, added to the office week's |
| Sidebar actions | `SidebarAction` (Platform.Client) | Title, icon, permission, the sheet it opens; in the foot above You |
| Tabs with counts | `TabStrip` | Counts; orange underline on the selected tab; `Href` for link tabs; `Sticky` on the phone |
| Preview banner | `PreviewBanner` | Above the tab bar; Hide, Exit |
| Phone preview | `PhoneFrame`, `PhoneDevice` | A true-size device scaled to fit; optional palette |
| Lock-screen proof | `LockScreenProof` | `TimeSensitive`, a caption slot |
| Home and surfaces | `HomeScreen`, `SurfaceViews` | The one way Home and the admin preview draw surfaces |
| Hub grid and tiles | `HubTiles`, `HubTile`, `HubIcon`, `HubTileViews` | `HubGrid` packs 2 to 4 columns, up to 4 rows; `TileShape`, `TileTones`, `Banner`, `TileVariant` |
| Hub canvas | `StylingPhone`, `HomePanel`, `TileSettings`, `AddTileSheet`, `HubLibrary` (Configuration Client) | Arranging and recolouring tiles in a draft, with the contrast warning; `hub-canvas.js` ports `HubGrid`'s packing |
| App styling | `StylingWorkspace`, `StylingPhone`, `PreviewChips`, `StylingOverview`, `ColoursPanel`, `HomePanel`, `TabsPanel`, `DraftsMenu`, `DraftListing` (Configuration Client), `ThemeSwatches` (Platform.Ui) | The styling page; styled in `drafts.css` |
| Choose several | `ChipPicker` | Matches starting with the search come first |
| Who sees it | `AudiencePicker` (Content) | Everyone, Signed in, or roles; anything else shows as "Set up specially" |
| Choose a picture or file | `MediaChooser` (Content) | A wide `Sheet` around `MediaPicker`; `ImagesOnly`; opens on the last folder used |
| Pick files | `FilePick` (Content) | `Kinds`, `Multiple`, `Limit`, `TooLarge`, `OnPicked` or `OnChosen`; `TakesPaste`, `DropOn` |
| Show a picture, video or file | `MediaPreview`, `MediaVideo` | A video plays inline, muted without `Controls` |
| Feedback | `SendFeedbackPage` (Feedback App); `AttachmentThumb` (Feedback Client); `SendFeedbackSheet`, `FeedbackBoard`, `TicketCard`, `TicketView`, `TicketPage`, `BlockedPhonesList` (Feedback Admin) | Phone page, admin sheet, board, a ticket `Docked` or as a page |
| Prayer desk settings | `PrayedForTimes`, `PrayedTimes` (Prayer Admin) | `PrayerDesk`'s Settings: `PrayedTimes.Choices` (7am to 8:30pm) via `DigestSchedulesClient`; `prayer.css` |
| Breadcrumbs | `Breadcrumbs`, `Crumb` | `CanDrop`, `OnDrop` make crumbs drop targets |
| Find someone | `PersonFinder` | `Leave` (ids to leave out), `Busy`; the People sheet and Family tab use it |
| Families and safeguarding | `AccountPeople`, `BlueCard`, `YourSafeguarding`, `ChildSheet`, `AddChildSheet`, `AskParentSheet`, `YourBirthDate`, `BirthDateAsk` (People Client); `PersonBirthDate`, `PersonFamilyTab`, `PersonSafeguardingTab`, `Safeguarding` (People Admin) | On Account (`AccountSection`, `AccountYouPart`); a person's tabs (`PersonTab`, `PersonFact`); Needs-you from `SafeguardingOfficeWeek` |
| Act on several | `SelectionTray` | `Count` (hidden at 0), `One`/`Many`, `OnClear` |
| Media library | `MediaLibrary`, `FolderTiles`, `MoveToSheet` (Media Client) | `FolderTiles Compact` for the chooser; `MoveToSheet` `Suggested`, `Blocked` |
| Places in the app | `PlaceDirectory`, `PlaceExplorer`, `PlacePhone`, `PlaceField` | `PlaceAction`s (one `Leads`); a feature's `PlacePreview`, else the masthead summary; `data-fit-height` |
| Agenda row | `AgendaRow` | `NotOn` (the circle-off line), `Quiet`, `Past`, `Warn`, `Selected` |
| Week rail | `WeekRail` | `Days` (`RailDay`), `Today`, `Here`, `Picked` |
| Choose several on and off | `ToggleChips` | `ToggleChip`; `toggle-chips--wrap` wraps instead of scrolling |
| Calendar | `DaySection`, `MonthGrid`, `EventHead`, `DateBlock`, `EventDetails`, `EventEditor`, `ComingUpTile` (Events Client) | Both sides (`InPanel`); admin page `CalendarWorkbench`, `CalendarSettingsPanel`, `BreakPanel` |
| Chats | `ChatsList`, `ChatView`, `ChatFrame`, `ChatPeople`, `ChatComposer`, `MessageActions` (Chats Client); `ChatsPanel` (Chats Admin) | One screen both sides: `ChatFrame` is the phone's `Page`, or `InPanel` a `PaneHead` and `Beside` pane; `MessageActions InPanel` is a `Popover`; `chats.css` |
| Icons, mark | `Icon`, `BrandMark` | Lucide paths (ISC); the mark is the logo pack's file as a mask, never redrawn |

The gallery is in the admin panel's Technical area and the app's More (for staff who may send feedback), in the current palette. `DesignDriftRules` and `IconRules` (counts in `design-ratchets.json`) hold the pattern map.
