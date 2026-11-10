## Bar (every station)

- Read AGENTS.md, DESIGN.md and docs/architecture/testing.md first. HARD RULE: every API path is gRPC; no new HTTP paths. HARD RULE: the app reads data only through Sync (the local store) and writes through the outbox; never a direct server call from an app screen unless the owner approved it (offline-and-caching.md). Who may see or do something goes through Access only. If staff can create something, they can delete it.
- The owner's AppHost runs from the main checkout and holds the fixed ports: don't stop it. If you need a backend, run your own beside it (AGENTS.md: `Dev__PortOffset`, copy main's database with pg_dump), and stop it **by its process id only, never by a pkill/pgrep pattern**. Always start it in Development (`DOTNET_ENVIRONMENT=Development ASPNETCORE_ENVIRONMENT=Development`, your own dashboard ports): outside Development, Aspire writes a new Postgres password into the user-secrets file every checkout shares, which locks the owner out of the dev database. Never print or list user secrets. Use the offset given in your brief.
- Use your own Simulator (`GSBC_DEVICE=<your device id> tools/ios/deploy.sh --simulator`) and your own debug-proxy port. **Shut it down (`xcrun simctl shutdown <your device id>`) and stop your debug proxy as soon as you're done looking, and always before you report or go idle:** a booted Simulator holds about 8 GB of the Mac's memory. Don't deploy to the owner's phone. Don't notify real people.
- **Screenshots stay as files in your scratchpad, listed by path in your report. Never put images, web pages or frames on the canvas.** Keep throwaway files in your scratchpad, not the worktree.
- Reuse shared components (Platform.Ui) first; no near-copies. Data on screen uses LiveData.
- Build with `/opt/homebrew/bin/dotnet`, 0 warnings, full suite green. Tests guard real rules (unit unless it truly needs Postgres). A flake is a bug.
- Release note in `changelog/unreleased/` for anything noticeable: front matter needs `added: <today, YYYY-MM-DD>`; a line for everyone and `## For staff` with Changed:/Where:/Try:.
- Docs: record decisions in the feature's doc, dated, as the owner's where they are. The repo is public: no real people's names, emails, keys or the owner's employer.
- No comments (hook). InspectCode clean for files you touch. Keep the project map current if projects change.
- Commit on your branch in reviewable steps; don't push or merge. Report briefly, in plain English: what changed for the owner, what you checked, what's open for them, screenshot paths.
- <Stations working at the same time.> Before your final commit merge `skeleton/backbone` again in case one was merged first.
- If a genuine design choice remains that these decisions don't settle, ask the owner with AskUserQuestion in your node (they get a notification) and carry on with the rest meanwhile. Otherwise build.
- Check your work in the admin panel (headless) and/or your own Simulator, as fits.
