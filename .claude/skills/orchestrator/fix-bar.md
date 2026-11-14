## Bar (fix stations)

For fixes to screens that already exist: bugs, feedback tickets, layout and styling corrections, small behaviour
changes. The owner's rule is the same as for every station; the checking is sized to the change (owner, 2026-11-14:
agents were "taking more and more time for simple things and fixes").

- Read AGENTS.md and docs/architecture/testing.md first. Read DESIGN.md (a few minutes: the **Pattern map**
  and the rules), then only the `docs/design/` file for the parts you touch. The hard rules all hold: gRPC only, the app reads
  through Sync, Access for who-sees-what, create means delete, no comments, the repo is public.
- The owner's AppHost runs from the main checkout: don't stop it. **Only run your own backend if the fix needs data or
  the server** (then as AGENTS.md says: `Dev__PortOffset`, `tools/dev/copy-main-db-live.sh`, Development environment,
  stop it by its process id). Use the offset in your brief. Never print user secrets.
- **Use your own Simulator only if the fix reaches the phone,** and the real on-screen keyboard only if it touches a
  text field (`idb ui tap`, hardware keyboard off; see testing.md). Shut the Simulator down as soon as you're done
  looking. Don't deploy to the owner's phone. Don't notify real people.
- UI: invoke `impeccable:impeccable` and use the matching command (`polish`, `layout`, `clarify` …) with
  `craft-floor.md` before editing, then `impeccable detect`. A decision page only when a fix has more than one
  reasonable look (then every sketch is one plain HTML file, no iframes, checked with `tools/dev/check-sketches.sh`).
  Reuse the shared component for the job (Pattern map); a near-copy is a defect.
- **Screenshots: the changed screen at the size it ships at, in the theme the owner reported it in** (light and dark
  only if the fix changes colour; the phone too if it's shared). Keep them in your scratchpad, never on the canvas.
  Run `tools/design/look-check.sh`, then look over every screenshot yourself for the whole screen, not just your
  change: content touching an edge, things out of line, text cut off or overlapping, bars out of place, big empty
  areas, menus running off screen. No second-agent look-over.
- Run the tests for what you touch while working; the **full suite once, before you report**. Build with
  `/opt/homebrew/bin/dotnet`, 0 warnings. A flake is a bug.
- Release note in `changelog/unreleased/` if anyone would notice: `added: <today, YYYY-MM-DD>`, a line for everyone
  and `## For staff` with Changed:/Where:/Try:. InspectCode clean for files you touch.
- <Stations working at the same time.> Merge `skeleton/backbone` before your final commit.
- Commit on your branch; don't push or merge. Report briefly in plain English: each fix and its cause in a sentence,
  what you checked, what's open for the owner, screenshot paths.
