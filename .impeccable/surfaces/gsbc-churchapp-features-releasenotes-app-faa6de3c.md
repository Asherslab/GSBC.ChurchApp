---
version: 1
slug: "gsbc-churchapp-features-releasenotes-app-faa6de3c"
primary_target: "src/Features/ReleaseNotes/GSBC.ChurchApp.Features.ReleaseNotes.App"
related_targets: ["src/Platform/GSBC.ChurchApp.Platform.Ui/PagedSheet.razor"]
---

# GSBC app: What's new

Scope: the highlight moment shown once on the first open after an update (WhatsNewMoment, built on Platform.Ui's
PagedSheet, Sheet's cover band, and FactList), and More → What's new, the list of every version's notes. Mode:
read for the page; the moment is a short, interruptive announcement that must stay welcome. Extends the
existing app shell. The model is in docs/architecture/changelog.md.

Audience: everyone who uses the app, opening it after a store update, often on a Sunday; and staff testing a
UAT build from TestFlight, who need to know what changed, where it is and what to try, in plain words.

## Direction contract

THESIS: News about the app arrives like a bulletin insert: one short, designed page per new thing, with a way to
try it at once, and gone in a tap. It refuses a wall of release-note text, a feed of cards, and a modal on a
fresh install.

OWN-WORLD: The existing shell (DESIGN.md). The moment is a bottom sheet whose top is a navy cover band (the
masthead's ground with the mark at 7% bleeding off the right): an icon tile on the masthead chip beside the
highlight's title in headline type, and a beige note line "New in 1.1" (or "New in this test build" in UAT).
Below: the one-line summary at lead size, staff facts (Where to find it, What to try) as a hairline fact list,
orange page dots (the marker: where you are), then Show me (primary) when the note links to a screen and
Next/Done (quiet). Swipe pages too. The page: masthead "What's new" with "Version 1.0 (build 42) on this phone"
for staff; one section per version, newest first; each note an entry (row icon, title, line, staff facts
indented, a Show me link); highlights carry the warm icon tile.

STORY: I update the app on Sunday, open it, and a sheet rises: "Sign in without a password. New in 1.1." I tap Show
me and land on Account. Later I find every change under More → What's new.

FIRST VIEWPORT: the sheet over Home: cover band with key icon and "Sign in without a password", "New in this test
build"; the line; Where to find it / What to try; Show me; Done.

FORM: extension of the existing shell; the owner answered the interview (Show me, icon now picture optional,
Where + Try for staff) and delegated the concept choice ("design what you think is best", 2026-11-03), so the
bulletin-insert structure was chosen from the three dealt; seed:35e226891792cea6.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, and
DESIGN.md.
