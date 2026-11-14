# Release notes and What's new

Decided 2026-11-03 (owner's brief; the design is the main session's). Changelogs are a core feature, in
place before more features land.

- **People using the app** get sharp, simple notes: one short line per change, in the church's plain voice.
- **UAT testers are staff.** Their notes say more: what changed, where to find it, what to try. Still not
  technical: no class names, no PR talk.
- **The app presents; TestFlight asks for testing.** In the app, even in UAT, notes read as
  presenting a feature to the person using it (what changed, where to find it). The testing steps (`Try:`)
  go only to TestFlight's What to Test, never into the app (owner, 2026-11-03: the app's "What to try"
  read as dev-tester focused, not user focused).
- **Big new things are presented once,** on the first open after an update: a short, designed What's new
  moment, not a wall of text.

## One file per change

Notes live in the repo, in [`changelog/`](../../changelog/README.md), one Markdown file per change, so
parallel branches never conflict:

```markdown
---
title: Sign in without a password
audience: everyone          # or staff: never in the production notes
added: 2026-10-26           # the day the note was written: What's new lists newest first
highlight: true             # shown in What's new after the update
icon: key                   # an icon from Platform.Ui's IconPaths
link: /account              # the screen "Show me" opens
image: passkeys.webp        # a 16:9 picture in changelog/images/
area: Account
---
Use your face, your fingerprint or your phone's passcode to sign in, and stay signed in.

## For staff
Changed: Signing in on the phone uses a passkey…
Where: More → Account
Try: Sign in, completely close the app, open it again…
```

- `title`, `audience` and `added` are required; the rest is optional. The file name is the note's id (lower-case
  words and hyphens), so a note keeps its id when it moves into a version folder.
- The line under the front matter is everyone's: one sentence, 160 characters at most.
- `## For staff` holds labelled parts (`Changed:`, `Where:`, `Try:`), each optional, each a line or a
  short paragraph. The owner chose labelled parts over free prose (2026-11-03) so the app and TestFlight
  show them the same way.
- `audience: staff` is for changes only staff see (admin screens, staff tools).
- **Every change people would notice gets a note, written in the same change** (AGENTS.md). Refactors,
  tests and docs get none.
- A note that breaks these rules fails the build with `GSBCRN01` naming the file and the problem. CI also
  runs when only `changelog/` changes, so a bad note is caught before TestFlight.

## Versions

- **The marketing version** (`1.0`, `1.1`, `1.1.1` for a fix) lives in one place,
  `src/Hosts/App/shell/ios/environments/Version.xcconfig`, which the Dev, UAT and Release configurations
  include. It is the version being built now. Android will read the same line when it is built.
- **The build number** is CI's run number (`testflight-uat.yml`); a dev build is build 1.
- **Releasing a version:** `tools/release-notes/release.sh 1.1` moves `changelog/unreleased/*` into
  `changelog/<version>/` (with `git mv`, so ids and history stay) and sets the marketing version to 1.1. Commit
  that, then cut the store build from it. New notes go to `unreleased/` again.
- UAT is a separate App Store Connect app that is never released, so its version trains never close; UAT
  builds can keep the same version for as long as it's useful.

## Built into the app

The notes describe the code in the build, so they travel with it: there is no server API and no feed.

- `Features.ReleaseNotes.Generator` is a Roslyn source generator. The App project hands it
  `changelog/*/*.md` and `changelog/images/*` as additional files, and it writes `BundledReleaseNotes` as
  plain C# (AOT-safe, SPEC §9: no reflection, no JSON), newest version first, and within a version the newest note first (`added`, then title; owner, 2026-11-08: it was alphabetical).
- **Which notes a build carries** is the MSBuild property `GsbcReleaseNotes`:
  - `staff` (every note, with the staff parts) by default for Debug, so dev builds behave like UAT;
  - `everyone` (only `audience: everyone`, only the line for everyone) by default for Release;
  - the TestFlight workflow publishes Release with `GsbcReleaseNotes=staff` in the environment, because
    the UAT and production apps share one WASM publish and differ only in the Xcode configuration.
  A production build therefore can't contain a staff note: it is never compiled in
  (`BuildNotesTests`, and checked in the published `.wasm` on 2026-11-03).
- Images in `changelog/images/*` (the folder is made with the first image) ship as the App project's static web assets
  (`_content/GSBC.ChurchApp.Features.ReleaseNotes.App/release-notes/<file>`); the generator fails a note
  whose image isn't there.

## In the app

**What's new, once after an update.** `WhatsNewMoment` sits in the app's layout. A beat after the first
render the layout offers the open to each `IOpeningSheet` in turn (the notifications sheet first, while
iOS's permission is undecided; [notifications.md](notifications.md), *The first-open sheet*). When it's
What's new's turn, it asks `WhatsNew` for highlights the phone hasn't seen and, if there are any, raises a
`PagedSheet`: a navy cover with the icon, the title and "New in 1.1" ("New in this test build" in UAT),
then the line, the staff facts in UAT, page dots, **Show me** when the note links somewhere, and
Next/Done. It pages by swipe or Next.

- **What's been seen is remembered by note id, per device,** in the data layer's local preferences
  (`ILocalPreferences`: `UserDefaults` on iOS, which goes with the app on delete; the Keychain would
  survive a reinstall, which is wrong here). A note's id is the same in every build, so a highlight shows
  once whichever build first carries it, and a version bump with nothing new shows nothing.
- **Not on a fresh install:** with nothing remembered, every note in the build is marked seen and nothing
  is shown. A first-time person gets onboarding, not a changelog. (So the first build carrying this
  feature shows nothing on phones that already had the app; the next build's highlights do show.)
- Seen is recorded when the sheet is shown, not when it's dismissed, so it is shown once even if the app
  is closed with it open.
- **One sheet per open:** when the notifications sheet takes an open, What's new isn't asked, so nothing
  is marked seen and its highlights show on the next open.
- **It never blocks the app:** any failure (no native layer, a storage error) shows nothing.
- `WhatsNewTests` holds the rule: nothing on a fresh install, new highlights once after an update, then
  nothing.

**More → What's new** lists every version newest first ("In this test build" for unreleased notes in
UAT), each note an entry with its icon, title and line; UAT adds the staff parts and a "Staff only" pill.
The masthead names the version (and in UAT the build) on this phone. A development build also offers
"Show the latest highlights again", to check the moment without reinstalling.

The pieces that are patterns live in Platform.Ui: `Sheet`'s `Cover` slot (the navy band with the mark),
`PagedSheet` (pages, dots, swipe, an action slot, Next/Done) and `FactList`/`Fact` (labelled lines with an
optional icon tile).

## TestFlight's What to Test

`testflight-uat.yml`:

1. checks out with full history and writes the text with `tools/release-notes/what-to-test.sh`: every note
   added or changed under `changelog/` since the nearest `testflight/uat/*` tag (all notes before the
   first upload), within Apple's 4,000 characters. Highlights come first, each with its line, `Where:`
   and `Try:`; then as many other notes in that form as fit; then every remaining note by title under
   "Also in this build:" (ending "…and N more" only if even the titles don't fit). `Changed:` is left
   to What's new in the app. Keep each slice's notes few, merged by feature, so the highlights fit
   whole. The text is also printed in the job summary.
2. publishes the app with staff notes, archives and uploads as before;
3. tags the uploaded commit `testflight/uat/<build number>` and pushes the tag (the job has
   `contents: write`), so the next build's notes start there;
4. waits for Apple to finish processing (up to an hour), then sets the build's What to Test through the
   App Store Connect API (`betaBuildLocalizations`, `tools/release-notes/SetWhatToTest.cs`) with the key
   already in the `uat` environment. These are outbound calls from CI, so the gRPC rule doesn't apply.

**Decided: a git tag per upload**, not a recorded build number in the repo: a tag needs no commit back to
`main`, and `git describe` finds the last upload reachable from the commit being built, even after a
revert. A failed upload leaves no tag, so its notes roll into the next one.

Not proven yet: the API call and the tag push run for the first time on the first TestFlight build
(the key only exists in the `uat` environment). If setting What to Test fails, the build is still uploaded;
paste the job summary's text into App Store Connect by hand.
